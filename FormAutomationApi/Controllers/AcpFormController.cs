// Controllers/AcpFormController.cs
//
// ── What changed from the previous version ───────────────────────────────────
//
//  ResolvePatientAsync:
//    Before → received a single SignatureName and split it by space,
//             which crashed when only one word was entered ("bhuvi" → lastName = null
//             → MySQL NOT NULL violation on patients.LastName).
//    After  → reads SignatureFirstName and SignatureLastName directly from the
//             request. No splitting. No null risk. Frontend provides both fields.
//
//  MapToEntity:
//    Before → mapped SignatureName (single field).
//    After  → maps SignatureFirstName + SignatureLastName (two separate fields).
//
//  BuildResponse:
//    Before → returned SignatureName.
//    After  → returns SignatureFirstName + SignatureLastName.
//
// ─────────────────────────────────────────────────────────────────────────────

using FormAutomationApi.Context;
using FormAutomationApi.DTOs;
using FormAutomationApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormAutomationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AcpFormController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AcpFormController> _log;

        public AcpFormController(ApplicationDbContext db, ILogger<AcpFormController> log)
        {
            _db = db;
            _log = log;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/AcpForm/patient/{patientId}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetByPatient(long patientId)
        {
            _log.LogInformation("[AcpForm] GET patient/{PatientId}", patientId);

            var form = await _db.PatientAcpForms
                .FirstOrDefaultAsync(f => f.PatientId == patientId);

            if (form == null)
                return NotFound(new { message = "No ACP form found for this patient." });

            var agents = await _db.AcpAgents.Where(a => a.FormId == form.Id).ToListAsync();
            var witnesses = await _db.AcpWitnesses.Where(w => w.FormId == form.Id).OrderBy(w => w.Id).ToListAsync();

            return Ok(BuildResponse(form, agents, witnesses));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/AcpForm/submit
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] AcpFormRequest? request)
        {
            if (request == null)
                return BadRequest(new { step = "deserialise", message = "Request body is null. Ensure Content-Type: application/json." });

            if (string.IsNullOrWhiteSpace(request.PatientName))
                return BadRequest(new { step = "validate", message = "patient_name is required." });

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Resolve or create patient row
                long resolvedPatientId = await ResolvePatientAsync(request);
                _log.LogInformation("[AcpForm] resolved patient_id={Id}", resolvedPatientId);

                // Step 2: Upsert ACP form row
                (long formId, bool isNew) = await UpsertFormAsync(request, resolvedPatientId);
                _log.LogInformation("[AcpForm] formId={FormId}  isNew={IsNew}", formId, isNew);

                // Step 3: Replace agents
                await ReplaceAgentsAsync(formId, request.Agents);

                // Step 4: Replace witnesses
                await ReplaceWitnessesAsync(formId, request.Witnesses);

                if (request.SessionId != Guid.Empty)
                {
                    var submission = await _db.FormSubmissions
                        .FirstOrDefaultAsync(x => x.SessionId == request.SessionId);

                    if (submission != null)
                    {
                        var now = DateTime.UtcNow;

                        submission.Status = SubmissionStatus.Completed;
                        submission.CompletedAt = now;
                        submission.ComplianceExpiresAt = now.AddYears(1);
                        submission.PatientId = (int)resolvedPatientId;

                        //await _db.SaveChangesAsync();
                    }
                    else
                    {
                        _log.LogWarning("[AcpForm] No FormSubmission found for SessionId={Id}", request.SessionId);
                    }
                }
                else
                {
                    _log.LogWarning("[AcpForm] SessionId is empty — FormSubmission not updated");
                }

                // Step 5: Commit
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
               
                _log.LogInformation("[AcpForm] Commit OK – formId={FormId}", formId);

                var saved = await _db.PatientAcpForms.FindAsync(formId);
                var agents = await _db.AcpAgents.Where(a => a.FormId == formId).ToListAsync();
                var witnesses = await _db.AcpWitnesses.Where(w => w.FormId == formId).OrderBy(w => w.Id).ToListAsync();

                return Ok(new AcpSubmitResult
                {
                    Success = true,
                    FormId = formId,
                    IsNew = isNew,
                    PatientId = resolvedPatientId,
                    Form = BuildResponse(saved!, agents, witnesses)
                });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                var deepest = dbEx.InnerException?.InnerException ?? dbEx.InnerException ?? (Exception)dbEx;
                _log.LogError(dbEx, "[AcpForm] DbUpdateException: {Msg}", deepest.Message);
                return StatusCode(500, new
                {
                    step = "db_update",
                    error = dbEx.Message,
                    inner = dbEx.InnerException?.Message,
                    detail = deepest.Message,
                    hint = GetDbHint(deepest.Message)
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _log.LogError(ex, "[AcpForm] Unhandled: {Msg}", ex.Message);
                return StatusCode(500, new
                {
                    step = "unknown",
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    detail = ex.InnerException?.InnerException?.Message,
                    trace = ex.StackTrace?.Split('\n').Take(10).Select(l => l.Trim())
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/AcpForm/debug  ⚠️ DEV ONLY — no DB write
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("debug")]
        public IActionResult Debug([FromBody] AcpFormRequest? request)
        {
            if (request == null)
                return BadRequest(new { step = "deserialise", error = "Request body is null." });

            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(request.PatientName)) issues.Add("patient_name is empty");
            if (string.IsNullOrWhiteSpace(request.LwName)) issues.Add("lw_name is empty");
            if (string.IsNullOrWhiteSpace(request.LifeChoice)) issues.Add("life_choice is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureFirstName)) issues.Add("signature_first_name is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureLastName)) issues.Add("signature_last_name is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureDate)) issues.Add("signature_date is empty");
            if (string.IsNullOrWhiteSpace(request.SignaturePhone)) issues.Add("signature_phone is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureAddress)) issues.Add("signature_address is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureCity)) issues.Add("signature_city is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureState)) issues.Add("signature_state is empty");
            if (string.IsNullOrWhiteSpace(request.SignatureZip)) issues.Add("signature_zip is empty");
            if (request.Agents == null || request.Agents.Count == 0) issues.Add("agents list is null/empty");
            if (request.Witnesses == null || request.Witnesses.Count == 0) issues.Add("witnesses list is null/empty");

            bool isNewPatient = !(request.PatientId.HasValue && request.PatientId > 0);
            if (isNewPatient && request.PatientInfo == null)
                issues.Add("patient_id is null AND patient_info is missing — new-patient creation will fail");

            return Ok(new
            {
                step = "debug_echo",
                is_new_patient = isNewPatient,
                validation_issues = issues,
                received = new
                {
                    patient_id = request.PatientId,
                    patient_info = request.PatientInfo,
                    patient_name = request.PatientName,
                    life_choice = request.LifeChoice,
                    signature_first_name = request.SignatureFirstName,
                    signature_last_name = request.SignatureLastName,
                    signature_date = request.SignatureDate,
                    signature_phone = request.SignaturePhone,
                    signature_address = request.SignatureAddress,
                    signature_city = request.SignatureCity,
                    signature_state = request.SignatureState,
                    signature_zip = request.SignatureZip,
                    signature_image_len = request.SignatureImage?.Length ?? 0,
                    agents = request.Agents?.Select(a => new { a.Type, a.Name, a.Address, a.City, a.Phone }),
                    witnesses = request.Witnesses?.Select((w, i) => new
                    {
                        index = i,
                        w.Name,
                        w.Date,
                        w.Address,
                        sig_image_len = w.SignatureImage?.Length ?? 0
                    })
                }
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE — Step 1: Resolve or create patient
        //
        //  EXISTING patient → return request.PatientId directly, no DB touch.
        //
        //  NEW patient      → read SignatureFirstName + SignatureLastName directly
        //                     from the request (no splitting, no null risk),
        //                     INSERT into patients, SaveChanges() for AUTO_INCREMENT id,
        //                     return that id.
        // ─────────────────────────────────────────────────────────────────────
        private async Task<long> ResolvePatientAsync(AcpFormRequest request)
        {
            // ── Existing patient ─────────────────────────────────────────────
            if (request.PatientId.HasValue && request.PatientId > 0)
            {
                _log.LogInformation("[AcpForm] Existing patient – patient_id={Id}", request.PatientId.Value);
                return request.PatientId.Value;
            }

            // ── New patient ──────────────────────────────────────────────────
            if (request.PatientInfo == null)
                throw new Exception(
                    "patient_id is null and patient_info is missing. " +
                    "The frontend must send patient_info when patient_id is not provided.");

            // Read first/last directly — no splitting needed, no null risk.
            // PatientInfo.FirstName/LastName are populated from the two separate
            // form fields (signature_first_name, signature_last_name).
            string? firstName = request.PatientInfo.FirstName?.Trim();
            string? lastName = request.PatientInfo.LastName?.Trim();

            // Validation guard — these are NOT NULL in MySQL
            if (string.IsNullOrWhiteSpace(firstName))
                throw new Exception("First name is required to create a new patient record. " +
                                    "Ensure patient_info.first_name is populated from signature_first_name.");

            if (string.IsNullOrWhiteSpace(lastName))
                throw new Exception("Last name is required to create a new patient record. " +
                                    "Ensure patient_info.last_name is populated from signature_last_name.");

            _log.LogInformation("[AcpForm] New patient – creating patients row for '{First} {Last}'",
                firstName, lastName);

            var patient = new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                PhonePrimary = request.PatientInfo.PhonePrimary,
                AddressLine1 = request.PatientInfo.AddressLine1,
                City = request.PatientInfo.City,
                State = request.PatientInfo.State,
                ZipCode = request.PatientInfo.ZipCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Patients.Add(patient);

            // SaveChanges here to get the AUTO_INCREMENT PatientId BEFORE
            // writing the FK row in patient_acp_forms.
            await _db.SaveChangesAsync();

            _log.LogInformation("[AcpForm] New patient created – PatientId={Id}  Name='{First} {Last}'",
                patient.PatientId, firstName, lastName);

            return (long)patient.PatientId;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE — Step 2: Upsert patient_acp_forms row
        // ─────────────────────────────────────────────────────────────────────
        private async Task<(long formId, bool isNew)> UpsertFormAsync(
            AcpFormRequest request,
            long resolvedPatientId)
        {
            var existing = await _db.PatientAcpForms
                .FirstOrDefaultAsync(f => f.PatientId == resolvedPatientId);

            if (existing != null)
            {
                _log.LogInformation("[AcpForm] UPDATE form id={Id} for patient_id={PatientId}",
                    existing.Id, resolvedPatientId);
                MapToEntity(request, existing, resolvedPatientId);
                existing.UpdatedAt = DateTime.UtcNow;
                return (existing.Id, false);
            }

            _log.LogInformation("[AcpForm] INSERT new form for patient_id={PatientId}", resolvedPatientId);

            var entity = new PatientAcpForm();
            MapToEntity(request, entity, resolvedPatientId);
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _db.PatientAcpForms.Add(entity);

            // SaveChanges here to get the AUTO_INCREMENT FormId BEFORE
            // writing agents and witnesses (FK → FormId)
            await _db.SaveChangesAsync();

           

            _log.LogInformation("[AcpForm] INSERT saved – formId={Id}", entity.Id);
            return (entity.Id, true);
        }

        private static void MapToEntity(AcpFormRequest dto, PatientAcpForm e, long resolvedPatientId)
        {
            e.PatientId = resolvedPatientId;   // never null
            e.PatientName = dto.PatientName;
            e.AgentLimits = dto.AgentLimits;
            e.ProxyExpiry = dto.ProxyExpiry;
            e.AgentInstructions = dto.AgentInstructions;
            e.LwName = dto.LwName;
            e.LifeChoice = dto.LifeChoice;
            e.NoCpr = dto.NoCpr;
            e.NoVent = dto.NoVent;
            e.NoNutrition = dto.NoNutrition;
            e.NoAntibiotics = dto.NoAntibiotics;
            e.PainLimit = dto.PainLimit;
            e.OtherDirections = dto.OtherDirections;
            e.OrganChoice = dto.OrganChoice;
            e.OrganSpec = dto.OrganSpec;
            e.PurposeTransplant = dto.PurposeTransplant;
            e.PurposeTherapy = dto.PurposeTherapy;
            e.PurposeResearch = dto.PurposeResearch;
            e.PurposeEducation = dto.PurposeEducation;
            e.SignatureFirstName = dto.SignatureFirstName;  // direct — no splitting
            e.SignatureLastName = dto.SignatureLastName;   // direct — no splitting
            e.SignaturePhone = dto.SignaturePhone;
            e.SignatureAddress = dto.SignatureAddress;
            e.SignatureCity = dto.SignatureCity;
            e.SignatureState = dto.SignatureState;
            e.SignatureZip = dto.SignatureZip;
            e.SignatureImage = dto.SignatureImage;

            if (!string.IsNullOrWhiteSpace(dto.SignatureDate) &&
                DateTime.TryParse(dto.SignatureDate, out var sigDate))
                e.SignatureDate = sigDate;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE — Steps 3 & 4: Replace agents and witnesses (delete → insert)
        // ─────────────────────────────────────────────────────────────────────
        private async Task ReplaceAgentsAsync(long formId, List<AcpAgentDto>? agents)
        {
            var old = await _db.AcpAgents.Where(a => a.FormId == formId).ToListAsync();
            _db.AcpAgents.RemoveRange(old);

            if (agents == null || agents.Count == 0) return;

            foreach (var dto in agents)
            {
                if (string.IsNullOrWhiteSpace(dto.Name)) continue;
                _db.AcpAgents.Add(new AcpAgent
                {
                    FormId = formId,
                    Type = dto.Type,
                    Name = dto.Name,
                    Address = dto.Address,
                    City = dto.City,
                    Phone = dto.Phone
                });
            }
        }

        private async Task ReplaceWitnessesAsync(long formId, List<AcpWitnessDto>? witnesses)
        {
            var old = await _db.AcpWitnesses.Where(w => w.FormId == formId).ToListAsync();
            _db.AcpWitnesses.RemoveRange(old);

            if (witnesses == null || witnesses.Count == 0) return;

            foreach (var dto in witnesses)
            {
                if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                DateTime? parsedDate = null;
                if (!string.IsNullOrWhiteSpace(dto.Date) && DateTime.TryParse(dto.Date, out var d))
                    parsedDate = d;

                _db.AcpWitnesses.Add(new AcpWitness
                {
                    FormId = formId,
                    Name = dto.Name,
                    Address = dto.Address,
                    Date = parsedDate,
                    SignatureImage = dto.SignatureImage
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // RESPONSE BUILDER
        // ─────────────────────────────────────────────────────────────────────
        private static AcpFormResponse BuildResponse(
            PatientAcpForm form,
            List<AcpAgent> agents,
            List<AcpWitness> witnesses)
        {
            return new AcpFormResponse
            {
                Id = form.Id,
                PatientId = form.PatientId,
                PatientName = form.PatientName,
                AgentLimits = form.AgentLimits,
                ProxyExpiry = form.ProxyExpiry,
                AgentInstructions = form.AgentInstructions,
                LwName = form.LwName,
                LifeChoice = form.LifeChoice,
                NoCpr = form.NoCpr,
                NoVent = form.NoVent,
                NoNutrition = form.NoNutrition,
                NoAntibiotics = form.NoAntibiotics,
                PainLimit = form.PainLimit,
                OtherDirections = form.OtherDirections,
                OrganChoice = form.OrganChoice,
                OrganSpec = form.OrganSpec,
                PurposeTransplant = form.PurposeTransplant,
                PurposeTherapy = form.PurposeTherapy,
                PurposeResearch = form.PurposeResearch,
                PurposeEducation = form.PurposeEducation,
                SignatureFirstName = form.SignatureFirstName,
                SignatureLastName = form.SignatureLastName,
                SignatureDate = form.SignatureDate?.ToString("yyyy-MM-dd"),
                SignaturePhone = form.SignaturePhone,
                SignatureAddress = form.SignatureAddress,
                SignatureCity = form.SignatureCity,
                SignatureState = form.SignatureState,
                SignatureZip = form.SignatureZip,
                SignatureImage = form.SignatureImage,
                Agents = agents.Select(a => new AcpAgentDto
                {
                    Type = a.Type,
                    Name = a.Name,
                    Address = a.Address,
                    City = a.City,
                    Phone = a.Phone
                }).ToList(),
                Witnesses = witnesses.Select(w => new AcpWitnessResponseDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Date = w.Date?.ToString("yyyy-MM-dd"),
                    Address = w.Address,
                    SignatureImage = w.SignatureImage
                }).ToList()
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // DB ERROR HINTS
        // ─────────────────────────────────────────────────────────────────────
        private static string GetDbHint(string msg)
        {
            if (msg.Contains("foreign key constraint") || msg.Contains("Cannot add or update a child row"))
                return "FK violation — patient_id doesn't exist in patients table yet, or form_id hasn't been committed before writing agents/witnesses.";
            if (msg.Contains("Data too long"))
                return "Column too small — run: ALTER TABLE patient_acp_forms MODIFY COLUMN signature_image LONGTEXT;";
            if (msg.Contains("Incorrect datetime") || msg.Contains("truncated"))
                return "Date parse failure — expected format YYYY-MM-DD.";
            if (msg.Contains("Duplicate entry"))
                return "Unique constraint — row already exists with this key.";
            if (msg.Contains("cannot be null") && msg.Contains("LastName"))
                return "LastName is NOT NULL in patients table. Ensure signature_last_name is filled in on the form and patient_info.last_name is populated in the payload.";
            if (msg.Contains("cannot be null") && msg.Contains("FirstName"))
                return "FirstName is NOT NULL in patients table. Ensure signature_first_name is filled in on the form and patient_info.first_name is populated in the payload.";
            if (msg.Contains("doesn't have a default value") || msg.Contains("cannot be null"))
                return "NOT NULL column missing a value — check DDL vs payload.";
            if (msg.Contains("Unknown column"))
                return "Column name mismatch — did you run the ALTER TABLE to add signature_first_name and signature_last_name? Check EF [Column] annotations vs DDL.";
            if (msg.Contains("Table") && msg.Contains("doesn't exist"))
                return "Table not found — run EF migrations or check DbContext DbSet registration.";
            return "Compare 'detail' above against your MySQL table DDL.";
        }
    }
}
// Controllers/PatientController.cs

using FormAutomationApi.Context;
using FormAutomationApi.DTOs;
using FormAutomationApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormAutomationApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public PatientController(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/Patient/{id}
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(int id)
        {
            // Patient: PK = PatientId (AUTO_INCREMENT)
            var patient = await _db.Patients
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();

            var patientId = patient.PatientId;

            // PatientDemographic: PK = PatientId (shared key, 1-to-1)
            var demographics = await _db.PatientDemographics
                .FirstOrDefaultAsync(d => d.PatientId == patientId);

            // PatientEmployment: PK = PatientEmploymentId, FK = PatientId
            var employer = await _db.PatientEmployments
                .FirstOrDefaultAsync(e => e.PatientId == patientId);

            // EmergencyContact: PK = EmergencyContactId, FK = PatientId
            var emergency = await _db.EmergencyContacts
                .FirstOrDefaultAsync(e => e.PatientId == patientId);

            // PatientPharmacy: PK = PatientPharmacyId, FK = PatientId
            var pharmacy = await _db.PatientPharmacies
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            // PatientInsurance: PK = PatientInsuranceId, FK = PatientId + InsurancePlanId
            var patientInsurance = await _db.PatientInsurances
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            // InsurancePlan: PK = InsurancePlanId
            InsurancePlan? insurance = null;
            if (patientInsurance?.InsurancePlanId != null)
            {
                insurance = await _db.InsurancePlans
                    .FirstOrDefaultAsync(p => p.InsurancePlanId == patientInsurance.InsurancePlanId);
            }

            // PatientOffice: PK = PatientOfficeId, FK = PatientId + OfficeId
            var patientOffice = await _db.PatientOffices
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            // Office: PK = OfficeId
            Office? office = null;
            if (patientOffice?.OfficeId != null)
            {
                office = await _db.Offices
                    .FirstOrDefaultAsync(o => o.OfficeId == patientOffice.OfficeId);
            }

            // PatientProvider: PK = PatientProviderId, FK = PatientId
            var patientProvider = await _db.patientProviders
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            // IntakePacket: PK = IntakePacketId, FK = PatientId + OfficeId
            var intakePacket = await _db.IntakePackets
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            // Everything below depends on having a SignedDocument
            SignedDocument? signedDocuments = null;
            List<SignedDocumentResponse> signedDocumentResponse = new();
            List<HipaaFamilyMember> hipaa = new();
            UnableToObtainSignature? unableToObtainSignature = null;

            if (intakePacket != null)
            {
                // SignedDocument: PK = SignedDocumentId, FK = IntakePacketId + DocumentTypeId
                signedDocuments = await _db.SignedDocuments
                    .FirstOrDefaultAsync(s => s.IntakePacketId == intakePacket.IntakePacketId);

                if (signedDocuments != null)
                {
                    var signedDocumentId = signedDocuments.SignedDocumentId;

                    // SignedDocumentResponse: PK = ResponseId, FK = SignedDocumentId
                    signedDocumentResponse = await _db.SignedDocumentResponse
                        .Where(r => r.SignedDocumentId == signedDocumentId)
                        .ToListAsync();

                    // HipaaFamilyMember: PK = HipaaFamilyMemberId, FK = SignedDocumentId
                    // IMPORTANT: No PatientId column on this table
                    hipaa = await _db.HipaaFamilyMembers
                        .Where(h => h.SignedDocumentId == signedDocumentId)
                        .ToListAsync();

                    // UnableToObtainSignature: PK = UnableId, FK = SignedDocumentId
                    unableToObtainSignature = await _db.UnableToObtainSignatures
                        .FirstOrDefaultAsync(u => u.SignedDocumentId == signedDocumentId);
                }
            }

            // PatientSignature: PK = PatientSignatureId, FK = PatientId
            var signature = await _db.PatientSignatures
                .FirstOrDefaultAsync(s => s.PatientId == patientId);

            return Ok(new
            {
                patient,
                demographics,
                employer,
                emergency,
                pharmacy,
                patientInsurance,
                insurance,
                patientOffice,
                office,
                patientProvider,
                intakePacket,
                signedDocuments,
                signedDocumentResponse,
                hipaa,
                unableToObtainSignature,
                signature
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/Patient/upload-signature
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("upload-signature")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadSignature([FromForm] UploadSignatureRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No file provided.");

            using var ms = new MemoryStream();
            await request.File.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // PatientSignature: PK = PatientSignatureId, FK = PatientId
            var existing = await _db.PatientSignatures
                .FirstOrDefaultAsync(x => x.PatientId == request.PatientId);

            if (existing != null)
            {
                existing.SignatureData = bytes;
                existing.FileType = request.File.ContentType;
                existing.SignedAt = DateTime.UtcNow;
            }
            else
            {
                _db.PatientSignatures.Add(new PatientSignature
                {
                    PatientId = request.PatientId,
                    SignatureData = bytes,
                    FileType = request.File.ContentType,
                    SignedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/Patient/submit
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] RequestFormSubmission request)
        {
            if (request == null)
                return BadRequest("Request body is required.");
            

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Step 1: Patient — must be first (all other tables FK to patientId)
                var patientId = await UpsertPatientAsync(request.Patient , request.SessionId);

                // Step 2: Independent child tables — all keyed by PatientId
                await UpsertDemographicAsync(patientId, request.PatientDemographic);
                await UpsertEmploymentAsync(patientId, request.PatientEmployment);
                await UpsertPharmacyAsync(patientId, request.PatientPharmacy);
                await UpsertInsuranceAsync(patientId, request.PatientInsurance);
                await UpsertOfficeAsync(patientId, request.PatientOffice);
                await UpsertEmergencyContactAsync(patientId, request.EmergencyContact);
                await UpsertProviderAsync(patientId, request.PatientProvider);

                // Step 3: IntakePacket — needs patientId, produces intakePacketId
                var intakeId = await UpsertIntakePacketAsync(patientId, request.IntakePacket);
                if (intakeId == null)
                    throw new Exception("IntakePacket creation failed.");

                // Step 4: SignedDocument — needs intakePacketId, produces signedDocumentId
                var signedDocumentId = await UpsertSignedDocumentAsync(intakeId.Value, request.SignedDocument);

                // Step 5: HipaaFamilyMember — linked via SignedDocumentId ONLY (no PatientId column)
                await UpsertHipaaAsync(signedDocumentId, request.HipaaFamilyMembers);

                // Step 6: SignedDocumentResponses — keyed by signedDocumentId + questionCode
                if (request.SignedDocumentResponses != null)
                    await UpsertSignedDocumentResponsesAsync(signedDocumentId, request.SignedDocumentResponses);

                // Step 7: UnableToObtainSignature — keyed by signedDocumentId
                if (request.UnableToObtainSignature != null)
                    await UpsertUnableToObtainSignatureAsync(signedDocumentId, request.UnableToObtainSignature);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, patientId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    detail = ex.InnerException?.InnerException?.Message
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE UPSERT HELPERS
        // ─────────────────────────────────────────────────────────────────────

        // Patient
        // Schema: PatientId(PK AUTO_INCREMENT), FirstName, MiddleInitial, LastName,
        //         DateOfBirth, Sex, MaritalStatus, SSN_Last4, SSN_Encrypted,
        //         Email, PhonePrimary, PhoneAlternate, AddressLine1, AddressLine2,
        //         City, State, ZipCode, CreatedAt, UpdatedAt, initials, apt
        private async Task<int> UpsertPatientAsync(Patient dto , Guid sessionId)
        {
            if (dto == null) throw new Exception("Patient data is required.");

            Patient? existing = null;
            if (dto.PatientId > 0)
                existing = await _db.Patients.FindAsync(dto.PatientId);

            if (existing != null)
            {
                existing.FirstName = dto.FirstName;
                existing.MiddleInitial = dto.MiddleInitial;
                existing.LastName = dto.LastName;
                existing.DateOfBirth = dto.DateOfBirth;
                existing.Sex = dto.Sex;
                existing.MaritalStatus = dto.MaritalStatus;
                existing.SSN_Last4 = dto.SSN_Last4;
                existing.Email = dto.Email;
                existing.PhonePrimary = dto.PhonePrimary;
                existing.PhoneAlternate = dto.PhoneAlternate;
                existing.AddressLine1 = dto.AddressLine1;
                existing.AddressLine2 = dto.AddressLine2;
                existing.City = dto.City;
                existing.State = dto.State;
                existing.ZipCode = dto.ZipCode;
                existing.Initials = dto.Initials;
                existing.Apt = dto.Apt;
                existing.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return existing.PatientId;
            }
            else
            {
                var entity = new Patient
                {
                    FirstName = dto.FirstName,
                    MiddleInitial = dto.MiddleInitial,
                    LastName = dto.LastName,
                    DateOfBirth = dto.DateOfBirth,
                    Sex = dto.Sex,
                    MaritalStatus = dto.MaritalStatus,
                    SSN_Last4 = dto.SSN_Last4,
                    Email = dto.Email,
                    PhonePrimary = dto.PhonePrimary,
                    PhoneAlternate = dto.PhoneAlternate,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    State = dto.State,
                    ZipCode = dto.ZipCode,
                    Initials = dto.Initials,
                    Apt = dto.Apt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Patients.Add(entity);
                await _db.SaveChangesAsync(); // ✅ NOW PatientId is generated

                var submission = await _db.FormSubmissions
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId);

                if (submission != null)
                {
                    submission.PatientId = entity.PatientId; // ✅ correct value now
                    await _db.SaveChangesAsync();
                }

                return entity.PatientId;
            }
        }

        // PatientDemographic
        // Schema: PatientId(PK/FK — shared key), Language, Race, Ethnicity, UpdatedAt
        private async Task UpsertDemographicAsync(int patientId, PatientDemographic? dto)
        {
            if (dto == null) return;

            // PatientId IS the PK here so FindAsync is correct
            var existing = await _db.PatientDemographics.FindAsync(patientId);

            if (existing != null)
            {
                existing.Language = dto.Language;
                existing.Race = dto.Race;
                existing.Ethnicity = dto.Ethnicity;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.PatientDemographics.Add(new PatientDemographic
                {
                    PatientId = patientId,
                    Language = dto.Language,
                    Race = dto.Race,
                    Ethnicity = dto.Ethnicity,
                    UpdatedAt = DateTime.UtcNow
                });

                
            }
        }

        // PatientEmployment
        // Schema: PatientEmploymentId(PK AUTO_INCREMENT), PatientId(FK),
        //         EmployerName, Occupation, EmployerAddress, CreatedAt
        private async Task UpsertEmploymentAsync(int patientId, PatientEmployment? dto)
        {
            if (dto == null) return;

            // PK is PatientEmploymentId not PatientId — must use FirstOrDefaultAsync
            var existing = await _db.PatientEmployments
                .FirstOrDefaultAsync(e => e.PatientId == patientId);

            if (existing != null)
            {
                existing.EmployerName = dto.EmployerName;
                existing.Occupation = dto.Occupation;
                existing.EmployerAddress = dto.EmployerAddress;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.EmployerName)) return;

                _db.PatientEmployments.Add(new PatientEmployment
                {
                    PatientId = patientId,
                    EmployerName = dto.EmployerName,
                    Occupation = dto.Occupation,
                    EmployerAddress = dto.EmployerAddress,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // PatientPharmacy
        // Schema: PatientPharmacyId(PK AUTO_INCREMENT), PatientId(FK),
        //         PharmacyName, Location, Phone, IsPreferred, CreatedAt
        private async Task UpsertPharmacyAsync(int patientId, PatientPharmacy? dto)
        {
            if (dto == null) return;

            // PK is PatientPharmacyId not PatientId — must use FirstOrDefaultAsync
            var existing = await _db.PatientPharmacies
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (existing != null)
            {
                existing.PharmacyName = dto.PharmacyName;
                existing.Location = dto.Location;
                existing.Phone = dto.Phone;
                existing.IsPreferred = dto.IsPreferred;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.PharmacyName)) return;

                _db.PatientPharmacies.Add(new PatientPharmacy
                {
                    PatientId = patientId,
                    PharmacyName = dto.PharmacyName,
                    Location = dto.Location,
                    Phone = dto.Phone,
                    IsPreferred = dto.IsPreferred,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // PatientInsurance
        // Schema: PatientInsuranceId(PK AUTO_INCREMENT), PatientId(FK), InsurancePlanId(FK nullable),
        //         CoverageType, MemberId, GroupNumber, SubscriberName, SubscriberDOB,
        //         RelationshipToPatient, IsActive, CreatedAt
        private async Task UpsertInsuranceAsync(int patientId, PatientInsurance? dto)
        {
            if (dto == null) return;

            // PK is PatientInsuranceId not PatientId — must use FirstOrDefaultAsync
            var existing = await _db.PatientInsurances
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (existing != null)
            {
                existing.InsurancePlanId = dto.InsurancePlanId;
                existing.CoverageType = dto.CoverageType;
                existing.MemberId = dto.MemberId;
                existing.GroupNumber = dto.GroupNumber;
                existing.SubscriberName = dto.SubscriberName;
                existing.SubscriberDOB = dto.SubscriberDOB;
                existing.RelationshipToPatient = dto.RelationshipToPatient;
                existing.IsActive = dto.IsActive;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.CoverageType)) return;

                _db.PatientInsurances.Add(new PatientInsurance
                {
                    PatientId = patientId,
                    InsurancePlanId = dto.InsurancePlanId,
                    CoverageType = dto.CoverageType,
                    MemberId = dto.MemberId,
                    GroupNumber = dto.GroupNumber,
                    SubscriberName = dto.SubscriberName,
                    SubscriberDOB = dto.SubscriberDOB,
                    RelationshipToPatient = dto.RelationshipToPatient,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // PatientOffice
        // Schema: PatientOfficeId(PK AUTO_INCREMENT), PatientId(FK), OfficeId(FK),
        //         IsPrimary(TINYINT nullable), FirstVisitDate(DATE nullable), Active(TINYINT nullable)
        private async Task UpsertOfficeAsync(int patientId, PatientOffice? dto)
        {
            if (dto == null || dto.OfficeId == 0) return;

            var existing = await _db.PatientOffices
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (existing != null)
            {
                existing.OfficeId = dto.OfficeId;
                existing.IsPrimary = dto.IsPrimary;
                existing.FirstVisitDate = dto.FirstVisitDate;
                existing.Active = dto.Active;
            }
            else
            {
                _db.PatientOffices.Add(new PatientOffice
                {
                    PatientId = patientId,
                    OfficeId = dto.OfficeId,
                    IsPrimary = dto.IsPrimary,
                    FirstVisitDate = dto.FirstVisitDate,
                    Active = dto.Active
                });
            }
        }

        // EmergencyContact
        // Schema: EmergencyContactId(PK AUTO_INCREMENT), PatientId(FK),
        //         ContactName, Relationship, Phone, IsPrimary(TINYINT), CreatedAt
        private async Task UpsertEmergencyContactAsync(int patientId, EmergencyContact? dto)
        {
            if (dto == null) return;

            // PK is EmergencyContactId not PatientId — must use FirstOrDefaultAsync
            var existing = await _db.EmergencyContacts
                .FirstOrDefaultAsync(e => e.PatientId == patientId);

            if (existing != null)
            {
                existing.ContactName = dto.ContactName;
                existing.Relationship = dto.Relationship;
                existing.Phone = dto.Phone;
                existing.IsPrimary = dto.IsPrimary;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.ContactName)) return;

                _db.EmergencyContacts.Add(new EmergencyContact
                {
                    PatientId = patientId,
                    ContactName = dto.ContactName,
                    Relationship = dto.Relationship,
                    Phone = dto.Phone,
                    IsPrimary = dto.IsPrimary,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // PatientProvider
        // Schema: PatientProviderId(PK AUTO_INCREMENT), PatientId(FK),
        //         ProviderName, ProviderType, Notes, CreatedAt
        private async Task UpsertProviderAsync(int patientId, PatientProvider? dto)
        {
            if (dto == null) return;

            // PK is PatientProviderId not PatientId — must use FirstOrDefaultAsync
            var existing = await _db.patientProviders
                .FirstOrDefaultAsync(p => p.PatientId == patientId);

            if (existing != null)
            {
                existing.ProviderName = dto.ProviderName;
                existing.ProviderType = dto.ProviderType;
                existing.Notes = dto.Notes;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.ProviderName)) return;

                _db.patientProviders.Add(new PatientProvider
                {
                    PatientId = patientId,
                    ProviderName = dto.ProviderName,
                    ProviderType = dto.ProviderType,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // IntakePacket
        // Schema: IntakePacketId(PK AUTO_INCREMENT), PatientId(FK), PacketDate(DATE),
        //         LocationName, CreatedAt, OfficeId(FK)
        // CRITICAL: Must SaveChangesAsync before returning to get the real AUTO_INCREMENT id
        private async Task<int?> UpsertIntakePacketAsync(int patientId, IntakePacket? dto)
        {
            if (dto == null) return null;

            var existing = await _db.IntakePackets
                .FirstOrDefaultAsync(x => x.PatientId == patientId);

            if (existing != null)
            {
                existing.PacketDate = dto.PacketDate;
                existing.LocationName = dto.LocationName;
                existing.OfficeId = dto.OfficeId > 0 ? dto.OfficeId : null;

                await _db.SaveChangesAsync();
                return existing.IntakePacketId;
            }
            else
            {
                // ✅ FIXED — treat 0 as null so FK is not violated
                var entity = new IntakePacket
                {
                    PatientId = patientId,
                    PacketDate = dto.PacketDate,
                    LocationName = dto.LocationName,
                    OfficeId = dto.OfficeId > 0 ? dto.OfficeId : null,  // ← null when no office
                    CreatedAt = DateTime.UtcNow
                };

                _db.IntakePackets.Add(entity);
                await _db.SaveChangesAsync(); // Required to populate IntakePacketId before returning
                return entity.IntakePacketId;
            }
        }

        // SignedDocument
        // Schema: SignedDocumentId(PK AUTO_INCREMENT), IntakePacketId(FK), DocumentTypeId(FK),
        //         SignedByName, SignedByRole, RepresentativeAuthority, SignedAt,
        //         SignatureCaptured(TINYINT), Notes, DocumentVersionId(FK nullable)
        // CRITICAL: Must SaveChangesAsync before returning to get the real AUTO_INCREMENT id
        private async Task<int> UpsertSignedDocumentAsync(int intakePacketId, SignedDocument? dto)
        {
            if (dto == null) throw new Exception("SignedDocument is required.");

            var existing = await _db.SignedDocuments
                .FirstOrDefaultAsync(x => x.IntakePacketId == intakePacketId);

            if (existing != null)
            {
                existing.SignedByName = dto.SignedByName;
                existing.SignedByRole = dto.SignedByRole;
                existing.RepresentativeAuthority = dto.RepresentativeAuthority;
                existing.SignedAt = DateTime.UtcNow;
                existing.SignatureCaptured = dto.SignatureCaptured;
                existing.Notes = dto.Notes;
                existing.DocumentVersionId = dto.DocumentVersionId;

                await _db.SaveChangesAsync();
                return existing.SignedDocumentId;
            }
            else
            {
                var entity = new SignedDocument
                {
                    IntakePacketId = intakePacketId,
                    DocumentTypeId = dto.DocumentTypeId > 0 ? dto.DocumentTypeId : 1,
                    SignedByName = dto.SignedByName,
                    SignedByRole = dto.SignedByRole,
                    RepresentativeAuthority = dto.RepresentativeAuthority,
                    SignedAt = DateTime.UtcNow,
                    SignatureCaptured = dto.SignatureCaptured,
                    Notes = dto.Notes,
                    DocumentVersionId = dto.DocumentVersionId
                };

                _db.SignedDocuments.Add(entity);
                await _db.SaveChangesAsync(); // Required to populate SignedDocumentId before returning
                return entity.SignedDocumentId;
            }
        }

        // HipaaFamilyMember
        // Schema: HipaaFamilyMemberId(PK AUTO_INCREMENT), SignedDocumentId(FK),
        //         FamilyMemberName, Relationship, isRepresentative(BIT)
        // IMPORTANT: No PatientId column on this table — linked ONLY via SignedDocumentId
        private async Task UpsertHipaaAsync(int signedDocumentId, List<HipaaFamilyMember>? members)
        {
            if (members == null || members.Count == 0) return;

            foreach (var dto in members)
            {
                HipaaFamilyMember? existing = null;

                // Update path: only if we already have a real PK from a previous save
                if (dto.HipaaFamilyMemberId > 0)
                    existing = await _db.HipaaFamilyMembers.FindAsync(dto.HipaaFamilyMemberId);

                if (existing != null)
                {
                    existing.FamilyMemberName = dto.FamilyMemberName;
                    existing.Relationship = dto.Relationship;
                    existing.IsRepresentative = dto.IsRepresentative;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.FamilyMemberName)) continue;

                    _db.HipaaFamilyMembers.Add(new HipaaFamilyMember
                    {
                        SignedDocumentId = signedDocumentId, // Only FK — no PatientId
                        FamilyMemberName = dto.FamilyMemberName,
                        Relationship = dto.Relationship,
                        IsRepresentative = dto.IsRepresentative
                    });
                }
            }
        }

        // SignedDocumentResponse
        // Schema: ResponseId(PK AUTO_INCREMENT), SignedDocumentId(FK), QuestionCode,
        //         ResponseType, BoolValue(TINYINT nullable), TextValue, DateValue, ChoiceValue
        private async Task UpsertSignedDocumentResponsesAsync(
            int signedDocumentId,
            List<SignedDocumentResponse> responses)
        {
            foreach (var dto in responses)
            {
                var existing = await _db.SignedDocumentResponse
                    .FirstOrDefaultAsync(x =>
                        x.SignedDocumentId == signedDocumentId &&
                        x.QuestionCode == dto.QuestionCode);

                if (existing != null)
                {
                    existing.BoolValue = dto.BoolValue;
                    existing.TextValue = dto.TextValue;
                    existing.ChoiceValue = dto.ChoiceValue;
                    existing.DateValue = dto.DateValue;
                }
                else
                {
                    _db.SignedDocumentResponse.Add(new SignedDocumentResponse
                    {
                        SignedDocumentId = signedDocumentId,
                        QuestionCode = dto.QuestionCode,
                        ResponseType = dto.ResponseType,
                        BoolValue = dto.BoolValue,
                        TextValue = dto.TextValue,
                        DateValue = dto.DateValue,
                        ChoiceValue = dto.ChoiceValue
                    });
                }
            }
        }

        // UnableToObtainSignature
        // Schema: UnableId(PK AUTO_INCREMENT), SignedDocumentId(FK),
        //         AttemptDate(DATE nullable), Reason, StaffInitials
        private async Task UpsertUnableToObtainSignatureAsync(
            int signedDocumentId,
            UnableToObtainSignature dto)
        {
            if (dto == null) return;

            // Always look up by FK, not by UnableId PK
            var existing = await _db.UnableToObtainSignatures
                .FirstOrDefaultAsync(u => u.SignedDocumentId == signedDocumentId);

            if (existing != null)
            {
                existing.AttemptDate = dto.AttemptDate;
                existing.Reason = dto.Reason;
                existing.StaffInitials = dto.StaffInitials;
            }
            else
            {
                _db.UnableToObtainSignatures.Add(new UnableToObtainSignature
                {
                    SignedDocumentId = signedDocumentId,
                    AttemptDate = dto.AttemptDate,
                    Reason = dto.Reason,
                    StaffInitials = dto.StaffInitials
                });
            }
        }
    }
}

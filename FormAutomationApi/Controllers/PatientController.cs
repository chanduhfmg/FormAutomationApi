// Controllers/PatientFormController.cs

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPatient(int id)

        {

            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null) return NotFound();
            var patientid = patient?.PatientId;
            
            var emergency = await _db.EmergencyContacts.FirstOrDefaultAsync(p => p.PatientId == patientid);
            
            var hippa = await _db.HipaaFamilyMembers
     .Where(p => p.HipaaFamilyMemberId == patientid)
     .ToListAsync();

            //pharmacy
            var pharmacy = await _db.PatientPharmacies.FirstOrDefaultAsync(p => p.PatientId == patientid);

            //demographics
            var demographics = await _db.PatientDemographics.FirstOrDefaultAsync(p => p.PatientId == patientid);

            //employer
            var employer = await _db.PatientEmployments.FirstOrDefaultAsync(p => p.PatientEmploymentId == patientid);


            //patientinsurance
            var patientInsurance = await _db.PatientInsurances.FirstOrDefaultAsync(p => p.PatientId == patientid);

            var insurancePlanId = patientInsurance?.InsurancePlanId;

            var insurance = await _db.InsurancePlans.FirstOrDefaultAsync(p => p.InsurancePlanId == insurancePlanId);

            var intakePacket = await _db.IntakePackets.FirstOrDefaultAsync(p => p.PatientId == patientid);

            var intakePacketId = intakePacket?.IntakePacketId;

            var signedDocuments = await _db.SignedDocuments.FirstOrDefaultAsync(p => p.IntakePacketId == intakePacketId);

            var signedDocumentId = signedDocuments.SignedDocumentId;

            var signedDocumentResponse = await _db.SignedDocumentResponse.Where(p => p.SignedDocumentId == signedDocumentId).ToListAsync();

            var patientoffice = await _db.PatientOffices.FirstOrDefaultAsync(p => p.PatientId == patientid);

            var officeid = patientoffice.OfficeId;

            var office = await _db.Offices.FirstOrDefaultAsync(p => p.OfficeId == officeid);

            var patientProvider = await _db.patientProviders.FirstOrDefaultAsync(p => p.PatientId == patientid);

            var unableToObtain = await _db.UnableToObtainSignatures.FirstOrDefaultAsync(p => p.SignedDocumentId == signedDocumentId);

         
            if (patient == null)
                return NotFound();
            var obj = new
            {
                patient,
                emergency,
                insurance,
                hippa,
                pharmacy,
                demographics,
                employer,
                patientInsurance,
                intakePacket,
                signedDocuments,
                signedDocumentResponse,
                patientoffice,
                office,
                patientProvider,
                unableToObtain


            };
            return Ok(obj);

        }


        [HttpPost("submit")]
        public async Task<IActionResult> Submit([FromBody] RequestFormSubmission request)
        {
            if (request == null)
                return BadRequest("Request body is required.");

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var patientId = await UpsertPatientAsync(request.Patient);

                await UpsertDemographicAsync(patientId, request.PatientDemographic);
                await UpsertEmploymentAsync(patientId, request.PatientEmployment);
                await UpsertPharmacyAsync(patientId, request.PatientPharmacy);
                await UpsertInsuranceAsync(patientId, request.PatientInsurance);
                await UpsertOfficeAsync(patientId, request.PatientOffice);
                await UpsertEmergencyContactAsync(patientId, request.EmergencyContact);
                await UpsertProviderAsync(patientId, request.PatientProvider);
                await UpsertHipaaAsync(patientId, request.HipaaFamilyMembers);

                // ✅ Intake
                var intakeId = await UpsertIntakePacketAsync(patientId, request.IntakePacket);

                if (intakeId == null)
                    throw new Exception("IntakePacket creation failed");

                // ✅ Signed Document    
                var signedDocumentId = await UpsertSignedDocumentAsync(intakeId.Value, request.SignedDocument);

                // ✅ Responses
                if (request.SignedDocumentResponses != null)
                {
                    await UpsertSignedDocumentResponsesAsync(signedDocumentId, request.SignedDocumentResponses);
                }

                // ✅ Unable to sign
                if (request.UnableToObtainSignature != null)
                {
                    await UpsertUnableToObtainSignatureAsync(signedDocumentId, request.UnableToObtainSignature);
                }

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


        // ── PATIENT ───────────────────────────────────────────────────────────
        private async Task<int> UpsertPatientAsync(Patient dto)
        {
            if (dto == null) throw new Exception("Patient data is required.");
            var existing = await _db.Patients.FindAsync(dto.PatientId);

            if (existing!=null)                          // ← ID exists = UPDATE
            {
                //if (existing == null) throw new Exception($"Patient {dto.PatientId} not found.");

                existing.FirstName = dto.FirstName;
                existing.LastName = dto.LastName;
                existing.MiddleInitial = dto.MiddleInitial;
                existing.AddressLine1 = dto.AddressLine1;
                existing.AddressLine2 = dto.AddressLine2;
                existing.City = dto.City;
                existing.State = dto.State;
                existing.ZipCode = dto.ZipCode;
                existing.Email = dto.Email;
                existing.PhonePrimary = dto.PhonePrimary;
                existing.PhoneAlternate = dto.PhoneAlternate;
                existing.Sex = dto.Sex;
                existing.MaritalStatus = dto.MaritalStatus;
                existing.DateOfBirth = dto.DateOfBirth;
                existing.SSN_Last4 = dto.SSN_Last4;
                existing.UpdatedAt = DateTime.UtcNow;

               
                return existing.PatientId;
            }
            else                                            // ← No ID = INSERT
            {
                var newPatient = new Patient
                {
                    
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    MiddleInitial = dto.MiddleInitial,
                    AddressLine1 = dto.AddressLine1,
                    AddressLine2 = dto.AddressLine2,
                    City = dto.City,
                    State = dto.State,
                    ZipCode = dto.ZipCode,
                    Email = dto.Email,
                    PhonePrimary = dto.PhonePrimary,
                    PhoneAlternate = dto.PhoneAlternate,
                    Sex = dto.Sex,
                    MaritalStatus = dto.MaritalStatus,
                    DateOfBirth = dto.DateOfBirth,
                    SSN_Last4 = dto.SSN_Last4,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Patients.Add(newPatient);
                return newPatient.PatientId;
            }
        }


        // ── DEMOGRAPHIC ───────────────────────────────────────────────────────
        private async Task UpsertDemographicAsync(int patientId, PatientDemographic? dto)
        {
            if (dto == null) return;
            Console.WriteLine("Patient upserted with ID: " + patientId);

            var existing = await _db.PatientDemographics.FindAsync(patientId); // PK = PatientId (1-to-1)

            if (existing != null)                           // ← UPDATE
            {
                existing.Language = dto.Language;
                existing.Race = dto.Race;
                existing.Ethnicity = dto.Ethnicity;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else                                            // ← INSERT
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


        // ── EMPLOYMENT ────────────────────────────────────────────────────────
        private async Task UpsertEmploymentAsync(int patientId, PatientEmployment? dto)
        {
            if (dto == null) return;
             var existing = await _db.PatientEmployments.FindAsync(patientId);

            if (existing!=null)                // ← UPDATE
            {
                

                existing.EmployerName = dto.EmployerName;
                existing.Occupation = dto.Occupation;
                existing.EmployerAddress = dto.EmployerAddress;
            }
            else                                            // ← INSERT
            {
                if (string.IsNullOrEmpty(dto.EmployerName)) return; // skip blank

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


        // ── PHARMACY ──────────────────────────────────────────────────────────
        private async Task UpsertPharmacyAsync(int patientId, PatientPharmacy? dto)
        {
            if (dto == null) return;
            var existing = await _db.PatientPharmacies.FindAsync(patientId);

            if (existing!=null)                  // ← UPDATE
            {

                existing.PharmacyName = dto.PharmacyName;
                existing.Location = dto.Location;
                existing.Phone = dto.Phone;
                existing.IsPreferred = dto.IsPreferred;
            }
            else                                            // ← INSERT
            {
                if (string.IsNullOrEmpty(dto.PharmacyName)) return;

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


        // ── INSURANCE ─────────────────────────────────────────────────────────
        private async Task UpsertInsuranceAsync(int patientId, PatientInsurance? dto)
        {
            if (dto == null) return;
            var existing = await _db.PatientInsurances.FindAsync(patientId);

            if (existing!=null)                 // ← UPDATE
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
            else                                            // ← INSERT
            {
                if (string.IsNullOrEmpty(dto.CoverageType)) return;

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


        // ── OFFICE ────────────────────────────────────────────────────────────
        private async Task UpsertOfficeAsync(int patientId, PatientOffice? dto)
        {
            if (dto == null) return;

                var existing = await _db.PatientOffices.FirstOrDefaultAsync(p=>p.PatientId==patientId);
            if (existing!=null)                    // ← UPDATE
            {
                existing.OfficeId = dto.OfficeId;
                existing.IsPrimary = dto.IsPrimary;
                existing.FirstVisitDate = dto.FirstVisitDate;
                existing.Active = dto.Active;
            }
            else                                            // ← INSERT
            {
                if (dto.OfficeId == 0) return;

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


        // ── EMERGENCY CONTACT ─────────────────────────────────────────────────
        private async Task UpsertEmergencyContactAsync(int patientId, EmergencyContact? dto)
        {
            if (dto == null) return;
                var existing = await _db.EmergencyContacts.FindAsync(patientId);

            if (existing!=null)                 // ← UPDATE
            {
                existing.ContactName = dto.ContactName;
                existing.Relationship = dto.Relationship;
                existing.Phone = dto.Phone;
                existing.IsPrimary = dto.IsPrimary;
            }
            else                                            // ← INSERT
            {
                if (string.IsNullOrEmpty(dto.ContactName)) return;

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


        // ── PROVIDER ──────────────────────────────────────────────────────────
        private async Task UpsertProviderAsync(int patientId, PatientProvider? dto)
        {
            if (dto == null) return;

            if (dto.PatientProviderId > 0)                  // ← UPDATE
            {
                var existing = await _db.patientProviders.FindAsync(patientId);
                if (existing == null) return;

                existing.ProviderName = dto.ProviderName;
                existing.ProviderType = dto.ProviderType;
                existing.Notes = dto.Notes;
            }
            else                                            // ← INSERT
            {
                if (string.IsNullOrEmpty(dto.ProviderName)) return;

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


        // ── HIPAA FAMILY MEMBERS ──────────────────────────────────────────────
        private async Task UpsertHipaaAsync(int patientId, List<HipaaFamilyMember>? members)
        {
            if (members == null || members.Count == 0) return;

            foreach (var dto in members)
            {
                    var existing = await _db.HipaaFamilyMembers.FirstOrDefaultAsync(dto=>dto.HipaaFamilyMemberId==patientId);
                if (existing!=null)            // ← UPDATE
                {

                    existing.FamilyMemberName = dto.FamilyMemberName;
                    existing.Relationship = dto.Relationship;
                }
                else                                        // ← INSERT
                {
                    if (string.IsNullOrEmpty(dto.FamilyMemberName)) continue;

                    _db.HipaaFamilyMembers.Add(new HipaaFamilyMember
                    {
                        SignedDocumentId = dto.SignedDocumentId,
                        FamilyMemberName = dto.FamilyMemberName,
                        Relationship = dto.Relationship
                    });
                }
            }

        }


        // ── INTAKE PACKET ─────────────────────────────────────────────────────
        private async Task<int?> UpsertIntakePacketAsync(int patientId, IntakePacket? dto)
        {
            if (dto == null) return null;

            var existing = await _db.IntakePackets
                .FirstOrDefaultAsync(x => x.PatientId == patientId);

            if (existing != null)
            {
                existing.PacketDate = dto.PacketDate;
                existing.LocationName = dto.LocationName;
                existing.OfficeId = dto.OfficeId;

                return existing.IntakePacketId;
            }
            else
            {
                var entity = new IntakePacket
                {
                    PatientId = patientId,
                    PacketDate = dto.PacketDate,
                    LocationName = dto.LocationName,
                    OfficeId = dto.OfficeId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.IntakePackets.Add(entity);

                return entity.IntakePacketId;
            }
        }


        // ── SIGNED DOCUMENT ───────────────────────────────────────────────────
        private async Task<int> UpsertSignedDocumentAsync(int intakePacketId, SignedDocument dto)
        {
            if (dto == null) throw new Exception("SignedDocument is required");

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
                

                return existing.SignedDocumentId;
            }
            else
            {
                var entity = new SignedDocument
                {
                    IntakePacketId = intakePacketId,
                    DocumentTypeId = dto.DocumentTypeId,
                    SignedByName = dto.SignedByName,
                    SignedByRole = dto.SignedByRole,
                    RepresentativeAuthority = dto.RepresentativeAuthority,
                    SignedAt = DateTime.UtcNow,
                    SignatureCaptured = dto.SignatureCaptured,
                    Notes = dto.Notes,
                    DocumentVersionId = dto.DocumentVersionId,
                    
                };

                _db.SignedDocuments.Add(entity);

                return entity.SignedDocumentId;
            }
        }


        private async Task UpsertSignedDocumentResponsesAsync(int signedDocumentId, List<SignedDocumentResponse> responses)
        {
            foreach (var dto in responses)
            {
                var existing = await _db.SignedDocumentResponse
                    .FirstOrDefaultAsync(x =>
                        x.SignedDocumentId == signedDocumentId &&
                        x.QuestionCode == dto.QuestionCode);

                if (existing != null)
                {
                    // ✅ UPDATE
                    existing.BoolValue = dto.BoolValue;
                    existing.TextValue = dto.TextValue;
                    existing.ChoiceValue = dto.ChoiceValue;
                    existing.DateValue = dto.DateValue;
                }
                else
                {
                    // ✅ INSERT
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


        // ── UNABLE TO OBTAIN SIGNATURE ────────────────────────────────────────
        private async Task UpsertUnableToObtainSignatureAsync(int signedDocumentId, UnableToObtainSignature dto)
        {
            if (dto == null) return;

            if (dto.UnableId > 0)                           // ← UPDATE
            {
                var existing = await _db.UnableToObtainSignatures.FindAsync(dto.UnableId);
                if (existing == null) return;

                existing.AttemptDate = dto.AttemptDate;
                existing.Reason = dto.Reason;
                existing.StaffInitials = dto.StaffInitials;
            }
            else                                            // ← INSERT
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


        // ── FORM SUBMISSION ───────────────────────────────────────────────────
        //private async Task UpsertFormSubmissionAsync(int patientId, FormSubmission? dto)
        //{
        //    if (dto == null) return;

        //    var existing = await _db.Form
        //        .FirstOrDefaultAsync(f => f.PatientId == patientId);

        //    if (existing != null)                           // ← UPDATE
        //    {
        //        existing.Status = dto.Status;
        //        existing.ExpiresAt = dto.ExpiresAt;
        //    }
        //    else                                            // ← INSERT
        //    {
        //        _db.FormSubmissions.Add(new FormSubmission
        //        {
        //            SessionId = Guid.NewGuid(),
        //            PatientId = patientId,
        //            SenderId = dto.SenderId,
        //            FormIds = dto.FormIds,
        //            Status = dto.Status,
        //            ExpiresAt = dto.ExpiresAt,
        //            CreatedAt = DateTime.UtcNow
        //        });
        //    }

        //    await _db.SaveChangesAsync();
        //}
    }
}
using FormAutomationApi.Model;
using Microsoft.EntityFrameworkCore;

namespace FormAutomationApi.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<DocumentType> DocumentTypes { get; set; }

        public DbSet<DocumentVersion> DocumentVersions { get; set; }

        public DbSet<EmergencyContact> EmergencyContacts { get; set; }

        public DbSet<HipaaFamilyMember> HipaaFamilyMembers { get; set; }    

        public DbSet<InsurancePlan> InsurancePlans { get; set; }   
        public DbSet<IntakePacket> IntakePackets { get; set; }

        public DbSet<Office> Offices {  get; set; }

        public DbSet<OfficeDocumentRequirement> OfficeDocumentRequirements { get; set; }    

        public DbSet<PatientDemographic> PatientDemographics {  get; set; }

        public DbSet<PatientEmployment> PatientEmployments { get; set; }    

        public DbSet<PatientOffice> PatientOffices { get; set; }    

        public DbSet<PatientPharmacy> PatientPharmacies { get; set; }   

        public DbSet<PatientProvider> patientProviders { get; set; }    

        public DbSet<SignedDocument> SignedDocuments { get; set; }  

        public DbSet<SignedDocumentResponse> SignedDocumentResponse { get; set; }  

        public DbSet<UnableToObtainSignature> UnableToObtainSignatures { get; set; }    

        public DbSet<PatientInsurance> PatientInsurances { get; set; }

        public DbSet<FormSubmission> FormSubmissions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>().ToTable("patient");

            modelBuilder.Entity<DocumentType>().ToTable("documenttype");

            modelBuilder.Entity<DocumentVersion>().ToTable("documentversion");

            modelBuilder.Entity<EmergencyContact>().ToTable("emergencycontact");

            modelBuilder.Entity<HipaaFamilyMember>().ToTable("hipaafamilymember");

            modelBuilder.Entity<InsurancePlan>().ToTable("insuranceplan");

            modelBuilder.Entity<IntakePacket>().ToTable("intakepacket");

            modelBuilder.Entity<Office>().ToTable("office");

            modelBuilder.Entity<OfficeDocumentRequirement>().ToTable("officedocumentrequirement");

            modelBuilder.Entity<PatientDemographic>().ToTable("patientdemographics");

            modelBuilder.Entity<PatientEmployment>().ToTable("patientemployment");

            modelBuilder.Entity<PatientInsurance>().ToTable("patientinsurance");

            modelBuilder.Entity<PatientOffice>().ToTable("patientoffice");

            modelBuilder.Entity<PatientPharmacy>().ToTable("patientpharmacy");

            modelBuilder.Entity<PatientProvider>().ToTable("patientprovider");

            modelBuilder.Entity<SignedDocument>().ToTable("signeddocument");

            modelBuilder.Entity<SignedDocumentResponse>().ToTable("signeddocumentresponse");

            modelBuilder.Entity<UnableToObtainSignature>().ToTable("unabletoobtainsignature");

            modelBuilder.Entity<FormSubmission>().ToTable("formsubmissions");
        }
    }
}

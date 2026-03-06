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

        public DbSet<HippaFamilyMember> HippaFamilyMembers { get; set; }    

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

        public DbSet<SignedDocumentResponse> SignedDocumentsResponse { get; set; }  

        public DbSet<UnableToObtainSignature> unableToObtainSignatures { get; set; }    
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>().ToTable("patient");

            modelBuilder.Entity<DocumentType>().ToTable("documenttype");

            modelBuilder.Entity<DocumentVersion>().ToTable("documentversion");

            modelBuilder.Entity<EmergencyContact>().ToTable("emergencycontact");

            modelBuilder.Entity<HippaFamilyMember>().ToTable("hippafamilymember");

            modelBuilder.Entity<InsurancePlan>().ToTable("insuranceplan");

            modelBuilder.Entity<IntakePacket>().ToTable("intakeplan");

            modelBuilder.Entity<Office>().ToTable("office");

            modelBuilder.Entity<OfficeDocumentRequirement>().ToTable("officedocumentrequirement");

            modelBuilder.Entity<PatientDemographic>().ToTable("patientdemographics");

            modelBuilder.Entity<PatientEmployment>().ToTable("patientemployment");

            modelBuilder.Entity<PatientInsurance>().ToTable("insuranceplan");

            modelBuilder.Entity<PatientOffice>().ToTable("patientoffice");

            modelBuilder.Entity<PatientPharmacy>().ToTable("patientpharamcy");

            modelBuilder.Entity<PatientProvider>().ToTable("patientprovider");

            modelBuilder.Entity<SignedDocument>().ToTable("signeddocument");

            modelBuilder.Entity<SignedDocumentResponse>().ToTable("signeddocumentresponse");

            modelBuilder.Entity<UnableToObtainSignature>().ToTable("unabletoobtainsignature");
        }
    }
}

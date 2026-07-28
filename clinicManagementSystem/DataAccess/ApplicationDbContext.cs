using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace clinicManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Doctor> Doctors { get; set; }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<MedicalRecord> MedicalRecords { get; set; }

        public DbSet<MedicalFile> MedicalFiles { get; set; }

        public DbSet<Review> Reviews { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dep => dep.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Doctor>()
                .HasOne(d => d.ApplicationUser)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(d => d.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Patient>()
                .HasOne(p => p.ApplicationUser)
                .WithOne(u => u.Patient)
                .HasForeignKey<Patient>(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.MedicalRecord)
                .WithOne(m => m.Appointment)
                .HasForeignKey<MedicalRecord>(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MedicalFile>()
                .HasOne(m => m.MedicalRecord)
                .WithMany(r => r.MedicalFiles)
                .HasForeignKey(m => m.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            

            builder.Entity<Appointment>()
                .HasOne(a => a.Review)
                .WithOne(r => r.Appointment)
                .HasForeignKey<Review>(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Appointment>()
                .HasOne(a => a.Schedule)
                .WithMany(ds => ds.Appointments)
                .HasForeignKey(a => a.DoctorScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.DoctorSchedules)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Prescription>()
                .HasOne(p=>p.MedicalRecord)
                .WithMany(m=>m.Prescriptions)
                .HasForeignKey(p=>p.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
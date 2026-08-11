namespace clinicManagementSystem.ViewModels
{
    public class CreateMedicalRecordVM
    {
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;

        public string Diagnosis { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public List<PrescriptionItemVM> Prescriptions { get; set; } = new List<PrescriptionItemVM>();
    }

    public class PrescriptionItemVM
    {
        public string? MedicineName { get; set; }
        public string? Dosage { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }
}
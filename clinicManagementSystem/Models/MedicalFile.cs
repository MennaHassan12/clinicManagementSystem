using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class MedicalFile
    {
        public int MedicalFileId { get; set; }

        public int MedicalRecordId { get; set; }

        [MaxLength(500)]

        public string FileName { get; set; } = null!;

        [MaxLength(500)]
        public string FilePath { get; set; } = null!;

        [MaxLength(50)]
        public string FileType { get; set; } = null!;

        public DateTime UploadDate { get; set; }

        public MedicalRecord MedicalRecord { get; set; } = null!;
    }
}
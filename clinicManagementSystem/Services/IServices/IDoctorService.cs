namespace clinicManagementSystem.Services.IServices
{
    public interface IDoctorService
    {
        Task SendDoctorAccountCredentialsAsync(string doctorEmail, string doctorName, string setPasswordLink, bool isNewAccount = true);
    }
}
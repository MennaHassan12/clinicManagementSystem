using clinicManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace clinicManagementSystem.DataAccess
{
    public class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager =
                serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();


            string[] roles =
            {
                "SuperAdmin",
                "Admin",
                "Patient",
                "Doctor"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await CreateUserAsync(
                userManager,
                "superadmin123@gmail.com",
                "SuperAdmin@123",
                "Super Admin",
                "SuperAdmin"
            );

            await CreateUserAsync(
                userManager,
                "admin123@gmail.com",
                "Admin@123",
                "Ahmed Mohamed",
                "Admin"
            );
             await CreateUserAsync(
                userManager,
                "patient123@gmail.com",
                "Patient@123",
                "yasmine khaled",
                "Patient"
            );
 
            var doctorUser = await CreateUserAsync(
                userManager,
                "doctor123@gmail.com",
                "Doctor@123",
                "DR Mohamed Ali",
                "Doctor"
            );

            var patientUser = await CreateUserAsync(
                userManager,
                "patient123@gmail.com",
                "Patient@123",
                "yasmine khaled",
                "Patient"
            );

            var dbContext = serviceProvider.GetRequiredService<clinicManagementSystem.Data.ApplicationDbContext>();

            // Ensure at least one Department exists
            var defaultDept = dbContext.Departments.FirstOrDefault();
            if (defaultDept == null)
            {
                defaultDept = new Department
                {
                    Name = "General Practice",
                    Description = "General medical consultations and comprehensive care"
                };
                dbContext.Departments.Add(defaultDept);
                await dbContext.SaveChangesAsync();
            }

            // Ensure Doctor profile is created for doctor123@gmail.com
            if (doctorUser != null && !dbContext.Doctors.Any(d => d.ApplicationUserId == doctorUser.Id))
            {
                var docProfile = new Doctor
                {
                    ApplicationUserId = doctorUser.Id,
                    DepartmentId = defaultDept.DepartmentId,
                    LicenseNumber = 99881,
                    ConsultationFee = 150,
                    YearsOfExperience = 8,
                    Bio = "Senior consultant dedicated to comprehensive medical care."
                };
                dbContext.Doctors.Add(docProfile);
                await dbContext.SaveChangesAsync();
            }

            // Ensure Patient profile is created for patient123@gmail.com
            if (patientUser != null && !dbContext.Patients.Any(p => p.ApplicationUserId == patientUser.Id))
            {
                var patProfile = new Patient
                {
                    ApplicationUserId = patientUser.Id,
                    Address = "Cairo, Egypt",
                    BirthDate = DateTime.Now.AddYears(-24),
                    Gender = Gender.Female,
                    BloodType = "A+"
                };
                dbContext.Patients.Add(patProfile);
                await dbContext.SaveChangesAsync();
            }
        }


        private static async Task<ApplicationUser> CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description)
                    );

                    throw new Exception(
                        $"Could not create user {email}: {errors}"
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return user;
        }
    }
}

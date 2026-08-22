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
 
            await CreateUserAsync(
                userManager,
                "doctor123@gmail.com",
                "Doctor@123",
                "DR Mohamed Ali",
                "Doctor"
            );
        }


        private static async Task CreateUserAsync(
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
        }
    }
}

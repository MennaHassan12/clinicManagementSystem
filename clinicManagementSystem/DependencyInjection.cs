using clinicManagementSystem.Models;
using clinicManagementSystem.Repositories;
using clinicManagementSystem.Repositories.IRepositories;
using clinicManagementSystem.Services;
using clinicManagementSystem.Services.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace clinicManagementSystem
{
    public  static class DependencyInjection
    {
        public static IServiceCollection Configure(this IServiceCollection services)
        {
            services.AddScoped<IRepository<Doctor>, Repository<Doctor>>();
            services.AddScoped<IRepository<Patient>, Repository<Patient>>();
            services.AddScoped<IRepository<Department>, Repository<Department>>();
            services.AddScoped<IRepository<Appointment>, Repository<Appointment>>();
            services.AddScoped<IRepository<MedicalRecord>, Repository<MedicalRecord>>();
            services.AddScoped<IRepository<MedicalFile>, Repository<MedicalFile>>();
            services.AddScoped<IRepository<Review>, Repository<Review>>();
            services.AddScoped<IRepository<DoctorSchedule>, Repository<DoctorSchedule>>();
            services.AddScoped<IRepository<ApplicationUserOTP>, Repository<ApplicationUserOTP>>();


            services.AddScoped<IAccountService, AccountService>();

            return services;


        }
    }
}

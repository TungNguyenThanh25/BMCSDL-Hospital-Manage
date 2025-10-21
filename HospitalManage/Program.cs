using Microsoft.AspNetCore.Authentication.Cookies;
using HospitalManage.Services.Encryption;
using Oracle.ManagedDataAccess.Client;

namespace HospitalManage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            // Register encryption services
            // - Symmetric (DES) via interface
            builder.Services.AddScoped<ISymmetricEncryptionService, SymmetricEncryptionService>();

            // - Asymmetric (RSA)
            builder.Services.AddScoped<AsymmetricEncryptionService>();

            // - Hybrid (uses RSA + DES)
            builder.Services.AddScoped<HybridEncryptionService>();

            // Authentication (cookie)
            builder.Services.AddScoped<OracleConnection>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connStr = config.GetConnectionString("OracleDBConnection");
                return new OracleConnection(connStr);
            });
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                    options.SlidingExpiration = true;
                });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

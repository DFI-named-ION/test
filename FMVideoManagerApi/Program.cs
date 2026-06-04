
using FFMpegCore;
using FMVideoManagerApi.Data;
using FMVideoManagerApi.Data.Repositories.UserRepository;
using FMVideoManagerApi.Models;
using FMVideoManagerApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FMVideoManagerApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection("Jwt"));

            JwtOptions jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

            byte[] keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);

            builder.Services.AddDbContext<ServerDbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("ServerDb"));
                options.EnableSensitiveDataLogging(); // remove
            });

            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<JwtTokenService>();
            builder.Services.AddScoped<DropboxStorageIndexingService>();

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.Configure<DropboxOptions>(builder.Configuration.GetSection("Dropbox"));
            builder.Services.AddHttpClient();
            builder.Services.AddDataProtection();

            builder.Services.AddSingleton<TokenProtector>();

            var app = builder.Build();

            try
            {
                using (IServiceScope scope = app.Services.CreateScope())
                {
                    ServerDbContext db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                    db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during database setup!");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();

            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = Path.Combine(AppContext.BaseDirectory, "Resources", "Exec")
            });
        }
    }
}
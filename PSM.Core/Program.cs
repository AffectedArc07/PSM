using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using Tomlyn.Extensions.Configuration;

namespace PSM.Core {
    public static class Program {
        public static void Main(string[] args) {
            // Logger outside of the host builder that we can use here
            ILogger startup_logger = LoggerFactory.Create(configure => {
                configure.AddSimpleConsole(options => {
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
                });
            }).CreateLogger("startup");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            // Config right off the bat
            builder.Configuration.AddTomlFile("appsettings.toml");

            string connstr = builder.Configuration.GetSection("Database").GetValue<string>("Connstring");

            startup_logger.LogInformation("Attempting to connect to database...");

            // Try establish a DBCon ASAP
            builder.Services.AddDbContext<PSMContext>(options => {
                try {
                    options.UseMySql(connstr, ServerVersion.AutoDetect(connstr));
                    startup_logger.LogInformation("Database connection successful");
                } catch (MySqlConnector.MySqlException ex) {
                    startup_logger.LogCritical(string.Format("Failed to connect to the MySQL/MariaDB Server: {0}", ex.ToString()));
                    startup_logger.LogCritical("This is a fatal error. PSM will now close");
                    Environment.Exit(1);
                }
            });

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Setup auth
            builder.Services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o => {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Constants.GetJWTBytes())
                };
            });

            builder.Services.AddScoped<IJWTRepository, JWTRepository>();

            // Setup logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options => {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
            });
            // Suppress EF logs
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

            WebApplication app = builder.Build();

            using (IServiceScope scope = app.Services.CreateScope()) {
                PSMContext dbcon = scope.ServiceProvider.GetRequiredService<PSMContext>();
                app.Logger.LogInformation("Migrating database.....");
                dbcon.Database.Migrate();
                app.Logger.LogInformation("Database schema migrated. Seeding default users...");
                bool users_seeded = dbcon.SeedPSMUsers(app.Logger);
                if(users_seeded) {
                    app.Logger.LogInformation("Users seeded successfully.");
                } else {
                    app.Logger.LogInformation("No user seeding required.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Logger.LogInformation("Finished initial startup");
            app.Run();
        }
    }
}
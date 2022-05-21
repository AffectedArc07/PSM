using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using PSM.Core.Core;
using PSM.Core.Core.Auth;
using PSM.Core.Core.Database;
using Tomlyn.Extensions.Configuration;

namespace PSM.Core {
    public static class Program {
        public static void Main(string[] args) {
            // Logger outside of the host builder that we can use here
            var startup_logger = LoggerFactory.Create(configure => {
                                                          configure.AddSimpleConsole(options => {
                                                                                         options.IncludeScopes = true;
                                                                                         options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
                                                                                     });
                                                      }).CreateLogger("startup");

            var builder = WebApplication.CreateBuilder(args);
            // Config right off the bat
            builder.Configuration.AddTomlFile("appsettings.toml");

            var connstr = builder.Configuration.GetSection("Database").GetValue<string>("Connstring");

            startup_logger.LogInformation("Attempting to connect to database...");

            // Try establish a DBCon ASAP
            builder.Services.AddDbContext<PSMContext>(options => {
                                                          try {
                                                              options.UseMySql(connstr, ServerVersion.AutoDetect(connstr));
                                                              startup_logger.LogInformation("Database connection successful");
                                                          } catch(MySqlConnector.MySqlException ex) {
                                                              startup_logger.LogCritical("Failed to connect to the MySQL/MariaDB Server: {Exception}", ex.ToString());
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
                    IssuerSigningKey = new SymmetricSecurityKey(Constants.JWT.GetByteMap())
                };
            });

            // Add the JWT service
            builder.Services.AddScoped<IJWTRepository, JWTRepository>();

            // Setup logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options => {
                options.IncludeScopes = true;
                options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
            });
            // Suppress EF logs
            builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

            var app = builder.Build();

            using (var scope = app.Services.CreateScope()) {
                var dbcon = scope.ServiceProvider.GetRequiredService<PSMContext>();
                app.Logger.LogInformation("Migrating database.....");
                dbcon.Database.Migrate();
                app.Logger.LogInformation("Database schema migrated. Seeding default users...");
                var users_seeded = dbcon.SeedPSMUsers(app.Logger);
                if(users_seeded) {
                    app.Logger.LogInformation("Users seeded successfully");
                } else {
                    app.Logger.LogInformation("No user seeding required");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // This MUST go before MapControllers
            var webapp_path = "wwwroot";
            if(app.Environment.IsDevelopment()) {
                webapp_path = "ClientApp/dist";
                if(args.Contains("NO_WDS")) {
                    app.Logger.LogInformation("Skipping WDS proxy setup");
                } else {
                    app.Map("/app", ctx => ctx.UseSpa(spa => {
                        spa.Options.SourcePath = "ClientApp";
                        spa.UseProxyToSpaDevelopmentServer("http://localhost:8400/");
                    }));
                    app.Logger.LogInformation("Initialised WDS proxy");
                }
                
            }

            app.UseFileServer(new FileServerOptions {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, webapp_path)),
                RequestPath = "/app",
                EnableDefaultFiles = true,
                EnableDirectoryBrowsing = false,
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Logger.LogInformation("Finished initial startup");
            app.Run();
        }
    }
}
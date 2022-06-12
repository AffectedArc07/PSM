using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using PSM.Core.Auth;
using PSM.Core.Database;
using Tomlyn.Extensions.Configuration;

namespace PSM.Core {
  public static class Program {
    public static async Task Main(string[] args) {
      Console.WriteLine("Starting PSM.");
      // Logger outside of the host builder that we can use here
      var startup_logger = LoggerFactory.Create(configure => {
                                                  configure.AddSimpleConsole(options => {
                                                                               options.IncludeScopes   = true;
                                                                               options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
                                                                             });
                                                }).CreateLogger("startup");

      var builder = WebApplication.CreateBuilder(args);
      // Config right off the bat
      builder.Configuration.AddTomlFile("appsettings.toml");

      var sqlSection = builder.Configuration.GetSection("Database");

      var sqlAddress  = sqlSection.GetValue<string>("Address");
      var sqlPassword = sqlSection.GetValue<string>("Password");
      var sqlUsername = sqlSection.GetValue<string>("Username");
      var sqlPort     = sqlSection.GetValue<int>("Port");
      var sqlDatabase = sqlSection.GetValue<string>("Database");

      var conStr = $"User={sqlUsername};Password={sqlPassword};Database={sqlDatabase};Server={sqlAddress};Port={sqlPort};";

      startup_logger.LogInformation("Attempting to connect to database...");

      // Establish DBCons immediately
      builder.Services.AddDbContext<UserContext>(options => {
                                                   try {
                                                     options.UseMySql(conStr, ServerVersion.AutoDetect(conStr));
                                                     startup_logger.LogInformation("User Context established");
                                                   } catch(MySqlException permEx) {
                                                     startup_logger.LogCritical("Failed to establish User Context");
                                                     startup_logger.LogCritical(" Inner Exception: {MySqlEx}", permEx.ToString());
                                                   }
                                                 });
      builder.Services.AddDbContext<InstanceContext>(options => {
                                                       try {
                                                         options.UseMySql(conStr, ServerVersion.AutoDetect(conStr));
                                                         startup_logger.LogInformation("User Context established");
                                                       } catch(MySqlException permEx) {
                                                         startup_logger.LogCritical("Failed to establish User Context");
                                                         startup_logger.LogCritical(" Inner Exception: {MySqlEx}", permEx.ToString());
                                                       }
                                                     });
      builder.Services.AddDbContext<PermissionContext>(options => {
                                                         try {
                                                           options.UseMySql(conStr, ServerVersion.AutoDetect(conStr));
                                                           startup_logger.LogInformation("Permission Context established");
                                                         } catch(MySqlException permEx) {
                                                           startup_logger.LogCritical("Failed to establish Permission Context");
                                                           startup_logger.LogCritical(" Inner Exception: {MySqlEx}", permEx.ToString());
                                                         }
                                                       });

      // Add services
      builder.Services.AddMvc().AddNewtonsoftJson();
      builder.Services.AddControllers();
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen();

      // Setup auth
      builder.Services.AddAuthentication(x => {
                                           x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                                           x.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
                                         }).AddJwtBearer(o => {
                                                           o.SaveToken = true;
                                                           o.TokenValidationParameters = new TokenValidationParameters {
                                                                                                                         ValidateIssuer           = false,
                                                                                                                         ValidateAudience         = false,
                                                                                                                         ValidateLifetime         = true,
                                                                                                                         ValidateIssuerSigningKey = true,
                                                                                                                         IssuerSigningKey         = new SymmetricSecurityKey(Constants.JWT.GetByteMap())
                                                                                                                       };
                                                         });

      // Add the JWT service
      builder.Services.AddScoped<IJWTRepository, JWTRepository>();

      // Setup logging
      builder.Logging.ClearProviders();
      builder.Logging.AddSimpleConsole(options => {
                                         options.IncludeScopes   = true;
                                         options.TimestampFormat = "[yyyy-MM-dd hh:mm:ss] ";
                                       });
      // Suppress EF logs
      builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

      var app = builder.Build();
      Constants.AppLog = app.Logger;

      using(var scope = app.Services.CreateScope()) {
        var userDb = scope.ServiceProvider.GetRequiredService<UserContext>();
        var permDb = scope.ServiceProvider.GetRequiredService<PermissionContext>();
        var instDb = scope.ServiceProvider.GetRequiredService<InstanceContext>();

        await userDb.Database.MigrateAsync();
        await permDb.Database.MigrateAsync();
        await instDb.Database.MigrateAsync();


        await userDb.WithPermissionContext(permDb).EnsureDefaultUsers();
      }

      // Configure the HTTP request pipeline.
      if(app.Environment.IsDevelopment()) {
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
                                                FileProvider            = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, webapp_path)),
                                                RequestPath             = "/app",
                                                EnableDefaultFiles      = true,
                                                EnableDirectoryBrowsing = false,
                                              });

      app.UseAuthentication();
      app.UseAuthorization();
      app.MapControllers();

      app.Logger.LogInformation("Finished initial startup");
      await app.RunAsync();
    }
  }
}

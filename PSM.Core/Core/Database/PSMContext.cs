using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PSM.Core.Models;

namespace PSM.Core.Core.Database {
    public class PSMContext : DbContext {
        public PSMContext(DbContextOptions<PSMContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Adds the default system user and an admin user
        /// </summary>
        public bool SeedPSMUsers(ILogger logger) {
            // Add system user first. Used by automated jobs & for the admin user
            bool workdone = false;

            User system_user;
            if (!Users.Where(x => x.Username.Equals(Constants.SYSTEM_USER_NAME)).Any()) {
                system_user = new User();
                system_user.Enabled = false;
                system_user.Username = Constants.SYSTEM_USER_NAME;
                system_user.PasswordHash = "_"; // Impossible to get via hashes
                Users.Add(system_user);
                logger.LogInformation("Seeded system user.");
                workdone = true;
                SaveChanges();
            } else {
                system_user = Users.Where(x => x.Username.Equals(Constants.SYSTEM_USER_NAME)).First();
            }

            // Add the default admin user
            if (!Users.Where(x => x.Username.Equals(Constants.ADMIN_USER_NAME)).Any()) {
                User admin_user = new User();
                admin_user.Enabled = true;
                PasswordHasher<User> hasher = new PasswordHasher<User>();
                admin_user.Username = Constants.ADMIN_USER_NAME;
                admin_user.PasswordHash = hasher.HashPassword(admin_user, Constants.DEFAULT_ADMIN_PASS);
                admin_user.CreatedBy = system_user;
                logger.LogInformation("Seeded default admin user.");
                Users.Add(admin_user);
                workdone = true;
                SaveChanges();
            }

            return workdone;
        }
    }
}

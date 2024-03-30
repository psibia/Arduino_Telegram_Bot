using ArduinoTelegramBot.Models.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoTelegramBot.Repositories.Authorization
{
    public class AccessControlDbContext : DbContext
    {
        public DbSet<AccessKey> AccessKeys { get; set; }
        public DbSet<UserKey> UserKeys { get; set; }

        public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AccessKey>()
                .HasKey(ak => ak.Key);

            modelBuilder.Entity<UserKey>()
                .HasKey(uk => uk.UserId);

            // Каскадное удаление связанных пользователей при удалении ключа доступа
            modelBuilder.Entity<UserKey>()
                .HasOne<AccessKey>()
                .WithMany()
                .HasForeignKey(uk => uk.Key)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

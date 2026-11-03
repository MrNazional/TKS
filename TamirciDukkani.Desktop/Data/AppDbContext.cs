using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using TamirciDukkani.Desktop.Models;

namespace TamirciDukkani.Desktop.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDir = Path.Combine(appData, "TamirciDukkani");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
            var dbPath = Path.Combine(dbDir, "tamirci.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Vehicle>(e =>
            {
                e.HasKey(v => v.Id);
                e.HasIndex(v => v.Plaka).IsUnique();
                e.Property(v => v.Plaka).IsRequired();
            });

            modelBuilder.Entity<Job>(e =>
            {
                e.HasKey(j => j.Id);
                e.Property(j => j.Durum).HasDefaultValue("Atölyede");
            });
        }
    }
}

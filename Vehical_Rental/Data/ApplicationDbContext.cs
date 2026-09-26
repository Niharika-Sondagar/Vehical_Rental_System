using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using Vehical_Rental.Models;

namespace Vehical_Rental.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => v.LicensePlate).IsUnique();
                entity.Property(v => v.DailyRate).HasPrecision(18, 2);
            });

            // Booking configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.TotalPrice).HasPrecision(18, 2);

                entity.HasOne(b => b.User)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Vehicle)
                      .WithMany(v => v.Bookings)
                      .HasForeignKey(b => b.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Amount).HasPrecision(18, 2);

                entity.HasOne(p => p.Booking)
                      .WithOne(b => b.Payment)
                      .HasForeignKey<Payment>(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.User)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Vehicle)
                      .WithMany(v => v.Reviews)
                      .HasForeignKey(r => r.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

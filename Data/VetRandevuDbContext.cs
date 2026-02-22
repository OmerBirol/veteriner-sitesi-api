using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Models;

namespace VetRandevu.Api.Data;

public class VetRandevuDbContext : IdentityDbContext<ApplicationUser>
{
    public VetRandevuDbContext(DbContextOptions<VetRandevuDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<AvailabilitySlot> Slots => Set<AvailabilitySlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<VaccinationReminder> VaccinationReminders => Set<VaccinationReminder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Clinic>()
            .HasMany<Service>()
            .WithOne()
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Clinic>()
            .HasMany<AvailabilitySlot>()
            .WithOne()
            .HasForeignKey(s => s.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Clinic>()
            .HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(a => a.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Clinic>()
            .HasMany<Review>()
            .WithOne()
            .HasForeignKey(r => r.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasMany<ReviewReport>()
            .WithOne()
            .HasForeignKey(r => r.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Pet>()
            .HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(a => a.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);

        builder.Entity<Service>()
            .HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(a => a.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Pet>()
            .HasMany<VaccinationRecord>()
            .WithOne()
            .HasForeignKey(v => v.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Clinic>()
            .HasMany<VaccinationRecord>()
            .WithOne()
            .HasForeignKey(v => v.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VaccinationRecord>()
            .HasMany<VaccinationReminder>()
            .WithOne()
            .HasForeignKey(r => r.VaccinationRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

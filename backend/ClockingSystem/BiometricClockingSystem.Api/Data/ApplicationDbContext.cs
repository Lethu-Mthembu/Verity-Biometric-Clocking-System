using Microsoft.EntityFrameworkCore;
using BiometricClockingSystem.Api.Models;

namespace BiometricClockingSystem.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<User> Users => Set<User>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    public DbSet<OverrideRequest> OverrideRequests { get; set; }  
    // Queue of "call administrator" requests raised from the kiosk.

    public DbSet<AdminOverride> AdminOverrides => Set<AdminOverride>();
    // Audit log of completed fingerprint overrides.

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    // Fluent API configurations

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Employee entity configuration
        base.OnModelCreating(modelBuilder);

        // User ↔ Employee (One-to-One)
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeNumber);
            // Employee Number is unique
            entity.HasIndex(e => e.EmployeeNumber)
                  .IsUnique();

            // Store the captured face image
            entity.Property(e => e.FaceImage)
                  .HasColumnType("bytea");
                  
             entity.Property(e => e.FaceDescriptor).HasColumnType("real[]");     
        });

        //ATTENDCE
        modelBuilder.Entity<Attendance>(entity =>
        {

            // Primary key
            entity.HasKey(a => a.AttendanceId);
            
            entity.HasOne(a => a.Employee)
                  .WithMany()
                  .HasForeignKey(a => a.EmployeeNumber)
                  .HasPrincipalKey(e => e.EmployeeNumber);

            entity.Property(a => a.EmployeeNumber)
                  .IsRequired();
        });

        //fingerprint method for ovrride
        modelBuilder.Entity<User>(entity =>
               {
                   entity.HasKey(u => u.Id);

                   entity.HasIndex(u => u.Email)
                         .IsUnique();

                   entity.Property(u => u.FingerprintTemplate)
                         .HasColumnType("bytea");

                   entity.Property(u => u.SecurityStamp)
                         .HasMaxLength(64);
               });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.HasIndex(log => log.OccurredAt);
            entity.HasIndex(log => new { log.TargetType, log.TargetId });
            entity.Property(log => log.Action).HasMaxLength(100);
            entity.Property(log => log.TargetType).HasMaxLength(100);
            entity.Property(log => log.TargetId).HasMaxLength(256);
            entity.Property(log => log.ActorEmail).HasMaxLength(256);
            entity.Property(log => log.ClientIpAddress).HasMaxLength(64);
            entity.Property(log => log.Details).HasMaxLength(2000);
        });

        modelBuilder.Entity<PasskeyCredential>(entity =>
        {
            entity.HasKey(credential => credential.Id);
            entity.HasIndex(credential => credential.UserId);
            entity.HasIndex(credential => credential.CredentialId).IsUnique();
            entity.Property(credential => credential.CredentialId).HasColumnType("bytea");
            entity.Property(credential => credential.PublicKey).HasColumnType("bytea");
            entity.Property(credential => credential.UserHandle).HasColumnType("bytea");
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(credential => credential.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        //override
        // modelBuilder.Entity<OverrideRequest>(entity =>
        //{
        //     entity.HasIndex(o => o.Status);
        //});

        modelBuilder.Entity<AdminOverride>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Employee)
                  .WithMany()
                  .HasForeignKey(a => a.EmployeeNumber)
                  .HasPrincipalKey(e => e.EmployeeNumber);

            entity.HasOne(a => a.Admin)
                  .WithMany()
                  .HasForeignKey(a => a.AdminId);
        });

    }


}

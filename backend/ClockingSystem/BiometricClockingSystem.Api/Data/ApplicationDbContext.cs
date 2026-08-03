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
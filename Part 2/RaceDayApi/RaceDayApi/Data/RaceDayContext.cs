using Microsoft.EntityFrameworkCore;
using RaceDayApi.Models;

namespace RaceDayApi.Data;

public class RaceDayContext : DbContext
{
    public RaceDayContext(DbContextOptions<RaceDayContext> options) : base(options) { }

    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        
        b.Entity<Organiser>(e =>
        {
            e.ToTable("Organiser");
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        b.Entity<Participant>(e =>
        {
            e.ToTable("Participant");
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255);
            e.Property(x => x.DateOfBirth).HasColumnType("date");
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Event" , t => t.HasCheckConstraint("CK_Event_EventType", "EventType IN ('Run', 'Walk', 'Cycle')"));
            e.Property(x => x.EventType).HasMaxLength(10).HasDefaultValue("Run");
            e.Property(x => x.EventName).HasMaxLength(100);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.HasOne<Organiser>().WithMany().HasForeignKey(x => x.OrganiserID);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Category");
            e.Property(x => x.CategoryName).HasMaxLength(50);
            e.Property(x => x.Distance).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)");
            e.HasOne<Event>().WithMany().HasForeignKey(x => x.EventID);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolment", t => t.HasCheckConstraint("CK_Enrolment_Status", "Status IN ('Confirmed', 'Cancelled')"));
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasOne<Participant>().WithMany().HasForeignKey(x => x.ParticipantID);
            e.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryID);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Result");
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.HasIndex(x => x.EnrolmentID).IsUnique();  
            e.HasOne<Enrolment>().WithMany().HasForeignKey(x => x.EnrolmentID);
            e.HasOne<Organiser>().WithMany().HasForeignKey(x => x.CapturedBy);
        });

        
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
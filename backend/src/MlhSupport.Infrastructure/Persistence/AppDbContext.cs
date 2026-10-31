using Microsoft.EntityFrameworkCore;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ParentChild> ParentChildren => Set<ParentChild>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<TeacherConcern> TeacherConcerns => Set<TeacherConcern>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

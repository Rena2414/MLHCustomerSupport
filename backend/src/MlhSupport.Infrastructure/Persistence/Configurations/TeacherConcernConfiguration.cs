using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence.Configurations;

public class TeacherConcernConfiguration : IEntityTypeConfiguration<TeacherConcern>
{
    public void Configure(EntityTypeBuilder<TeacherConcern> builder)
    {
        builder.ToTable("teacher_concerns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Text).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.Summary).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.TeacherId, c.CreatedAt });
    }
}

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Actor).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Details).HasMaxLength(1000).IsRequired();
        builder.HasIndex(a => a.TargetUserId);
        builder.HasIndex(a => a.ActorUserId);
    }
}

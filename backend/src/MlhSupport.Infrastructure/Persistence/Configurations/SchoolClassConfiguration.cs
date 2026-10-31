using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Subject).HasMaxLength(100).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Enrollments).WithOne().HasForeignKey(e => e.ClassId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ClassEnrollment> builder)
    {
        builder.ToTable("class_enrollments");
        builder.HasKey(e => new { e.ClassId, e.StudentId });
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.StudentId);
    }
}

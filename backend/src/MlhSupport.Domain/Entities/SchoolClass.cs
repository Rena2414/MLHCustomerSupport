namespace MlhSupport.Domain.Entities;

public class SchoolClass
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClassEnrollment> Enrollments { get; set; } = [];
}

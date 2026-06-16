namespace Domain.Entities
{
    public class Classroom
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public string SchoolCode { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ClassroomStudent> Students { get; set; } = new();
    }

    public class ClassroomStudent
    {
        public Guid ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = null!;
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}

namespace Domain.Entities
{
    public class StudentGroup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }
        public Teacher Teacher { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<StudentGroupMember> Members { get; set; } = new();
    }
}

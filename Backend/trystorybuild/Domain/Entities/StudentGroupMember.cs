namespace Domain.Entities
{
    public class StudentGroupMember
    {
        public Guid GroupId { get; set; }
        public StudentGroup Group { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}

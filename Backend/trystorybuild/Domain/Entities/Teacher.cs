namespace Domain.Entities
{
    public class Teacher
    {
        public Guid Id { get; set; }
        public User User { get; set; } = null!;
        public bool IsPrivate { get; set; } = true;
        public string? SchoolCode { get; set; }
        public List<Student> Students { get; set; } = new();
    }
}

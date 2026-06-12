namespace Domain.Entities
{
    public class Student
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Username { get; set; } = string.Empty;
        public StudentLoginMethod LoginMethod { get; set; } = StudentLoginMethod.ImagePin;
        public int ImagePin1 { get; set; }
        public int? ImagePin2 { get; set; }
        public string? PasswordHash { get; set; }
        public string? AvatarUrl { get; set; }
        public int Level { get; set; } = 1;
        public bool PlacementDone { get; set; } = false;

        public Guid? ParentId { get; set; }
        public Parent? Parent { get; set; }

        public Guid? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum StudentLoginMethod
    {
        ImagePin     = 0,
        TextPassword = 1
    }
}

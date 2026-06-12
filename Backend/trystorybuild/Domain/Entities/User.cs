namespace Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsBlocked { get; set; } = false;

        public Parent? Parent { get; set; }
        public Teacher? Teacher { get; set; }
    }

    public enum UserRole
    {
        Student     = 0,
        Parent      = 1,
        Teacher     = 2,
        SchoolAdmin = 3,
        SystemAdmin = 4
    }
}

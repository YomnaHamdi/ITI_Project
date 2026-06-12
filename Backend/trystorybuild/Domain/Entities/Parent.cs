namespace Domain.Entities
{
    public class Parent
    {
        public Guid Id { get; set; }
        public User User { get; set; } = null!;
        public List<Student> Children { get; set; } = new();
    }
}

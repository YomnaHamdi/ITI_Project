namespace Domain.Entities
{
    public class LessonAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;
        public Guid TeacherId { get; set; }
        public string TargetType { get; set; } = "Student"; // "Student" | "Group"
        public Guid? TargetStudentId { get; set; }
        public Guid? TargetGroupId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}

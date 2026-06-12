namespace Application.DTOs
{
    // ── Adult auth ─────────────────────────────────────────────────────────────
    public record RegisterRequest(
        string FullName,
        string Email,
        string Password,
        string Role,
        string? SchoolCode = null);

    public record LoginRequest(string Email, string Password);

    public record AuthResponse(
        string Token,
        string UserId,
        string Name,
        string Role,
        DateTime ExpiresAt);

    // ── Student management ─────────────────────────────────────────────────────
    public record CreateStudentRequest(
        string Name,
        int    Age,
        string Username,
        int    ImagePin1,
        int?   ImagePin2 = null,
        int    Level     = 1);

    public record StudentLoginRequest(
        string Username,
        int    ImagePin1,
        int?   ImagePin2 = null);

    public record StudentAuthResponse(
        string   Token,
        string   StudentId,
        string   Name,
        int      Level,
        bool     PlacementDone,
        DateTime ExpiresAt);

    public record StudentProfileDto(
        Guid   Id,
        string Name,
        int    Age,
        string Username,
        int    Level,
        bool   PlacementDone,
        string? AvatarUrl);
}

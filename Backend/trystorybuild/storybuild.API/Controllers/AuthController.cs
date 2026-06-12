using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        // ── Adult register ──────────────────────────────────────────────────────
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try   { return Ok(await authService.RegisterAsync(request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
            catch (ArgumentException         ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Adult login ─────────────────────────────────────────────────────────
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try   { return Ok(await authService.LoginAsync(request)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        // ── Create student profile (parent or teacher only) ─────────────────────
        [HttpPost("students")]
        [Authorize(Roles = "Parent,Teacher")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            var creatorId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            try   { return Ok(await authService.CreateStudentAsync(creatorId, request)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ── Student login (username + image PIN) ────────────────────────────────
        [HttpPost("students/login")]
        [ProducesResponseType(typeof(StudentAuthResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> StudentLogin([FromBody] StudentLoginRequest request)
        {
            try   { return Ok(await authService.StudentLoginAsync(request)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        // ── List children for logged-in parent ──────────────────────────────────
        [HttpGet("students")]
        [Authorize(Roles = "Parent,Teacher")]
        [ProducesResponseType(typeof(List<StudentProfileDto>), 200)]
        public async Task<IActionResult> GetStudents()
        {
            var userId = Guid.Parse(
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new InvalidOperationException("Invalid token."));

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var list = role == "Parent"
                ? await authService.GetChildrenAsync(userId)
                : await authService.GetStudentsAsync(userId);

            return Ok(list);
        }

        // ── Current user profile ────────────────────────────────────────────────
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(200)]
        public IActionResult Me()
        {
            var id   = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { id, name, role });
        }
    }
}

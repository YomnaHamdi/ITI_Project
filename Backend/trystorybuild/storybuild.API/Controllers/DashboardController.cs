using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController(IDashboardService dashboardService) : ControllerBase
    {
        [HttpGet("student/{childName}")]
        [ProducesResponseType(typeof(StudentDashboardDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStudent(string childName)
        {
            var data = await dashboardService.GetStudentDashboardAsync(childName);
            if (data is null)
                return NotFound(new { error = "لم يتم العثور على بيانات لهذا الطالب." });
            return Ok(data);
        }

        [HttpGet("parent/{childName}")]
        [ProducesResponseType(typeof(ParentDashboardDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetParent(string childName)
        {
            var data = await dashboardService.GetParentDashboardAsync(childName);
            if (data is null)
                return NotFound(new { error = "لم يتم العثور على بيانات لهذا الطفل." });
            return Ok(data);
        }

        [HttpGet("teacher")]
        [ProducesResponseType(typeof(TeacherDashboardDto), 200)]
        public async Task<IActionResult> GetTeacher()
        {
            var data = await dashboardService.GetTeacherDashboardAsync();
            return Ok(data);
        }

        [HttpGet("school")]
        [ProducesResponseType(typeof(SchoolDashboardDto), 200)]
        public async Task<IActionResult> GetSchool()
        {
            var data = await dashboardService.GetSchoolDashboardAsync();
            return Ok(data);
        }

        [HttpGet("students")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> GetStudentNames()
        {
            var names = await dashboardService.GetKnownChildNamesAsync();
            return Ok(names);
        }

        [HttpGet("levels/progress/{childName}")]
        [ProducesResponseType(typeof(List<LevelProgressDto>), 200)]
        public async Task<IActionResult> GetLevelProgress(string childName)
        {
            var data = await dashboardService.GetLevelProgressAsync(childName);
            return Ok(data);
        }
    }
}

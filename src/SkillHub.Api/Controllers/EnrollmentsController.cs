using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common;
using SkillHub.Application.DTOs.Enrollments;
using SkillHub.Application.Interfaces;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly IReportService _reportService;

    public EnrollmentsController(IEnrollmentService enrollmentService, IReportService reportService)
    {
        _enrollmentService = enrollmentService;
        _reportService = reportService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Participant)]
    public async Task<IActionResult> Enroll([FromBody] CreateEnrollmentRequestDto request, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var enrollment = await _enrollmentService.EnrollParticipantAsync(request.WorkshopId, currentUserId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, enrollment);
    }

    [HttpGet("mine")]
    [Authorize(Roles = AppRoles.Participant)]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(currentUserId, cancellationToken);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var currentUserEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@skillhub.com";
        var isEmployee = User.IsInRole(AppRoles.Employee);

        await _enrollmentService.CancelEnrollmentAsync(id, currentUserId, currentUserEmail, isEmployee, cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = AppRoles.Employee)]
    public async Task<IActionResult> ImportFromExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Please provide a valid Excel (.xlsx) file." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".xls")
            return BadRequest(new { error = "Only Excel spreadsheet files (.xlsx, .xls) are supported." });

        using var stream = file.OpenReadStream();
        var result = await _enrollmentService.ImportEnrollmentsFromExcelAsync(stream, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/certificate")]
    [Authorize]
    public async Task<IActionResult> DownloadCertificate(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isEmployee = User.IsInRole(AppRoles.Employee);

        var pdfBytes = await _reportService.GenerateCertificatePdfAsync(id, currentUserId, isEmployee, cancellationToken);
        return File(pdfBytes, "application/pdf", $"Constancia_Inscripcion_{id}.pdf");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common;
using SkillHub.Application.DTOs.Workshops;
using SkillHub.Application.Interfaces;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;
    private readonly IEnrollmentService _enrollmentService;

    public WorkshopsController(IWorkshopService workshopService, IEnrollmentService enrollmentService)
    {
        _workshopService = workshopService;
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetUpcomingWorkshops(CancellationToken cancellationToken)
    {
        var workshops = await _workshopService.GetUpcomingWorkshopsAsync(cancellationToken);
        return Ok(workshops);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetWorkshopById(Guid id, CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        return Ok(workshop);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Employee)]
    public async Task<IActionResult> CreateWorkshop([FromBody] CreateWorkshopRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _workshopService.CreateWorkshopAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkshopById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = AppRoles.Employee)]
    public async Task<IActionResult> GetWorkshopEnrollments(Guid id, CancellationToken cancellationToken)
    {
        var enrollments = await _enrollmentService.GetWorkshopEnrollmentsAsync(id, cancellationToken);
        return Ok(enrollments);
    }
}

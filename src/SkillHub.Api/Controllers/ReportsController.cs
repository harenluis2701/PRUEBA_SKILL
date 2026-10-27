using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.Common;
using SkillHub.Application.Interfaces;

namespace SkillHub.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("top-workshops")]
    [Authorize(Roles = AppRoles.Employee)]
    public async Task<IActionResult> GetTopWorkshops(CancellationToken cancellationToken)
    {
        var topWorkshops = await _reportService.GetTopWorkshopsAsync(cancellationToken);
        return Ok(topWorkshops);
    }
}

using HBP.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/dashboard")]
public sealed class DashboardController(IAdminDashboardService service) : ControllerBase
{
    [HttpGet]
    public Task<AdminDashboardResponse> Get(CancellationToken ct) => service.GetAsync(ct);
}

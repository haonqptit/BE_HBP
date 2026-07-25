using HBP.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/settings")]
public sealed class SettingsController(IAdminSystemSettingService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AdminSystemSettingResponse>> List(CancellationToken ct) => service.ListAsync(ct);

    [HttpGet("{key}")]
    public Task<AdminSystemSettingResponse> Get(string key, CancellationToken ct) => service.GetAsync(key, ct);

    [HttpPut("{key}")]
    public Task<AdminSystemSettingResponse> Update(string key, UpdateSystemSettingRequest request, CancellationToken ct) =>
        service.UpdateAsync(key, request, ct);
}

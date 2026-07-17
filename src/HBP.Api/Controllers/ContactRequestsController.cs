using HBP.Application.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HBP.Api.Controllers;

[ApiController, Route("api/contact-requests"), EnableRateLimiting("public-submit")]
public sealed class ContactRequestsController(IContactRequestService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateContactRequestRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));
}

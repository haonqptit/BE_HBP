using HBP.Application.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HBP.Api.Controllers;

[ApiController, Route("api/booking-requests"), EnableRateLimiting("public-submit")]
public sealed class BookingRequestsController(IBookingRequestService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequestRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));
}

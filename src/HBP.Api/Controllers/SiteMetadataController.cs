using HBP.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers;

[ApiController, Route("api/site-metadata")]
public sealed class SiteMetadataController(IPublicSiteMetadataQueryService service) : ControllerBase
{
    [HttpGet]
    public Task<SiteMetadataResponse> Get(CancellationToken ct) => service.GetAsync(ct);
}

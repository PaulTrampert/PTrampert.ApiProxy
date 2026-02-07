using Microsoft.AspNetCore.Mvc;

namespace PTrampert.ApiProxy.SampleApp.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthEchoController : ControllerBase
    {
        public Task<IActionResult> Get()
        {
            return Request.Headers.Authorization.Count > 0 
                ? Task.FromResult<IActionResult>(Ok(Request.Headers.Authorization)) 
                : Task.FromResult<IActionResult>(NotFound());
        }
    }
}

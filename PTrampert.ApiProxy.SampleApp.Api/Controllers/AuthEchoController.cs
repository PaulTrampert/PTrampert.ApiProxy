using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PTrampert.ApiProxy.SampleApp.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthEchoController : ControllerBase
    {
        public async Task<IActionResult> Get()
        {
            if (Request.Headers.Authorization.Count > 0)
            {
                return Ok(Request.Headers.Authorization);
            }
            else
            {
                return NotFound();
            }
        }
    }
}

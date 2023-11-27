using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Preservation.API.Controllers
{
    [Route("")]
    [ApiController]
    public class ValuesController : ControllerBase
    {

        [HttpGet]
        public string Get()
        {
            return "I'm alive";
        }
    }
}

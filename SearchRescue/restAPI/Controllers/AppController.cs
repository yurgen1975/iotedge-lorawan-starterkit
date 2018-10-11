using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GeoCoordinatePortable;

namespace restAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        // GET api/app
        [HttpGet]
        public ActionResult<string> Get()
        {
            throw new NotImplementedException();
        }
    }
}
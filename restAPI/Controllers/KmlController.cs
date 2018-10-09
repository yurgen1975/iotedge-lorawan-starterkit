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
    public class KmlController : ControllerBase
    {
        // GET api/klm
        [HttpGet]
        public ActionResult<IEnumerable<DeviceCoordinates>> Get()
        {
            throw new NotImplementedException();
        }

        // GET api/klm/52549 - specific device ID
        [HttpGet("{id}")]
        public ActionResult<IEnumerable<GeoCoordinate>> Get(ulong deviceEUI)
        {
            throw new NotImplementedException();
        }
    }
}

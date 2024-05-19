using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyServer.Sevices;

namespace MyServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistributionController : ControllerBase
    {
        private IDistributionCalculation _distributionCalculation;
        public DistributionController(IDistributionCalculation distributionCalculation)
        {
            _distributionCalculation = distributionCalculation;
        }

        [HttpPost]
        [Route("GetRouteMatrix")]
        public async Task<ActionResult<string>> GetShortestRouteWithMatrix([FromQuery] string origin, [FromQuery] decimal priceForCar, [FromQuery] decimal pricePerKm, [FromQuery] int max_passengers, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetShortestRouteWithMatrix(origin, priceForCar, pricePerKm,max_passengers, coordinates);
            return Ok(result);
        }

        [HttpPost]
        [Route("GetRoute")]
        public async Task<ActionResult<string>> GetRoute([FromQuery] string origin, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetRoute(origin, coordinates);
            return Ok(result);
        }

        [HttpPost]
        [Route("GetOptimalRoute")]
        public async Task<ActionResult<string>> GetDistance([FromQuery] string origin, [FromQuery] decimal priceForCar, [FromQuery] decimal pricePerKm, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetPriceDistributionForOneCar(origin,priceForCar,pricePerKm, coordinates);

            return Ok(result);
        }

        [HttpPost]
        [Route("GetDistribution")]
        public async Task<ActionResult<string>> GetDistribution([FromQuery] string origin, [FromQuery] decimal priceForCar, [FromQuery] decimal pricePerKm, [FromQuery] int max_passengers, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetDistribution(origin, priceForCar,pricePerKm, max_passengers,coordinates);

            return Ok(result);
        }
    }
}

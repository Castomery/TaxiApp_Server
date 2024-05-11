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
        [Route("GetRoute")]
        public async Task<ActionResult<string>> GetRoute([FromQuery] string origin, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetRoute(origin, coordinates);
            return Ok(result);
        }

        [HttpPost]
        [Route("GetOptimalRoute")]
        public async Task<ActionResult<string>> GetDistance([FromQuery] string origin, [FromQuery] double priceForCar, [FromQuery] double pricePerKm, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetPriceDistributionForOneCar(origin,priceForCar,pricePerKm, coordinates);

            return Ok(result);
        }

        [HttpPost]
        [Route("GetDistribution")]
        public async Task<ActionResult<string>> GetDistribution([FromQuery] string origin, [FromQuery] double priceForCar, [FromQuery] double pricePerKm, [FromBody] List<string> coordinates)
        {
            var result = await _distributionCalculation.GetDistribution(origin, priceForCar,pricePerKm,coordinates);

            return Ok(result);
        }
    }
}

using MyServer.Models;

namespace MyServer.Sevices
{
    public interface IDistributionCalculation
    {
        Task<Dictionary<string, List<ShortestRoute>>> GetShortestRouteWithMatrix(string origin, decimal priceForCar, decimal pricePerKm, int max_passengers, List<string> coordinates);
        Task<List<string>> GetRoute(string origin, List<string> coordinates);
        Task<ShortestRoute> GetPriceDistributionForOneCar(string origin, decimal priceForCar, decimal pricePerKm ,List<string> coordinates);
        Task<List<ShortestRoute>> GetDistribution(string origin, decimal priceForCar, decimal pricePerKm, int max_passengers, List<string> coordinates);
    }
}

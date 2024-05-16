using MyServer.Models;

namespace MyServer.Sevices
{
    public interface IDistributionCalculation
    {
        Task<List<string>> GetRoute(string origin, List<string> coordinates);
        Task<ShortestRoute> GetPriceDistributionForOneCar(string origin, double priceForCar, double pricePerKm ,List<string> coordinates);
        Task<List<ShortestRoute>> GetDistribution(string origin, double priceForCar, double pricePerKm, int max_passengers, List<string> coordinates);
    }
}

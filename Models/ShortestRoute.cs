namespace MyServer.Models
{
    public class ShortestRoute
    {
        public List<string> route { get; set; }
        public List<string> pointsNames { get; set; }
        public double distance { get; set; }
        public double duration { get; set; }
        public decimal totalPrice { get; set; }

        public decimal[] priceDistribution { get; set; }

        public ShortestRoute()
        {
            distance = double.MaxValue;
        }
    }
}

namespace MyServer.Models
{
    public class ShortestRoute
    {
        public List<string> route { get; set; }
        public List<string> pointsNames { get; set; }
        public double distance { get; set; }
        public double duration { get; set; }
        public double totalPrice { get; set; }

        public double[] priceDistribution { get; set; }

        public ShortestRoute()
        {
            distance = double.MaxValue;
        }
    }
}

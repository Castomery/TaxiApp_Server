namespace MyServer.Models
{
    public class OptimizationResponse
    {
        public string code { get; set; }
        public Waypoint[] waypoints { get; set; }
        public Trip[] trips { get; set; }
    }
}

namespace MyServer.Models
{
    public class Waypoint
    {
        public string name { get; set; }
        public double[]location { get; set; }
        public int waypoint_index {  get; set; }
        public int trips_index {  get; set; }
    }
}

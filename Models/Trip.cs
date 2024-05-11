namespace MyServer.Models
{
    public class Trip
    {
        public string geometry {  get; set; }
        public Leg[] legs { get; set; }
        public string weight_name { get; set; }
        public double weight { get; set; }
        public double duration {  get; set; }
        public double distance {  get; set; }
    }
}

namespace NetworkService.Model
{
    /// <summary>
    /// Udeo jednog tipa entiteta u ukupnom broju (za CG4 grafikon raspodele tipova).
    /// </summary>
    public class TypeShare
    {
        public string TypeName { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; } // 0 - 100
    }
}

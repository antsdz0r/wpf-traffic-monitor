namespace NetworkService.Model
{
   
    public class EntityType
    {
        public string Name { get; set; }        

        
        public double MaxValidValue { get; set; }

        public EntityType()
        {
        }

        public EntityType(string name, double maxValidValue)
        {
            Name = name;
            MaxValidValue = maxValidValue;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

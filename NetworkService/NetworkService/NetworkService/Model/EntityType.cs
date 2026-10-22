namespace NetworkService.Model
{
    /// <summary>
    /// Tip entiteta (T3 - Dnevni saobracaj): putevi mogu biti tipa "IA" ili "IB".
    /// Po specifikaciji, tip je posebna klasa sa poljem za ime i putanju do slike.
    /// Tipovi su unapred definisani (videti EntityType.PredefinedTypes).
    /// </summary>
    public class EntityType
    {
        public string Name { get; set; }          // npr. "IA" ili "IB"
        public string ImagePath { get; set; }      // putanja do unapred definisane slike tipa

        // Maksimalna validna izmerena vrednost za dati tip puta.
        // IA putevi: validno do 15000, IB putevi: validno do 7000 (sve iznad je nevalidno).
        public double MaxValidValue { get; set; }

        public EntityType()
        {
        }

        public EntityType(string name, string imagePath, double maxValidValue)
        {
            Name = name;
            ImagePath = imagePath;
            MaxValidValue = maxValidValue;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

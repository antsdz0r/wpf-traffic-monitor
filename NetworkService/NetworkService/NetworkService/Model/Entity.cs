using NetworkService.Helpers;

namespace NetworkService.Model
{
    /// <summary>
    /// Entitet pod monitoringom. Za temu T3 (Dnevni saobracaj) entitet je PUT.
    /// Atributi: Id (jedinstven ceo broj), Name (naziv), Type (tip - IA/IB) i
    /// Value (poslednja izmerena vrednost saobracaja).
    /// </summary>
    public class Entity : BindableBase
    {
        private int _id;
        private string _name;
        private EntityType _type;
        private double _value;
        private bool _hasMeasurement;

        public int Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        public EntityType Type
        {
            get { return _type; }
            set
            {
                if (SetProperty(ref _type, value))
                {
                    // Promena tipa moze da promeni i validnost trenutne vrednosti.
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        // Poslednja izmerena vrednost koju je poslao MeteringSimulator.
        public double Value
        {
            get { return _value; }
            set
            {
                if (SetProperty(ref _value, value))
                {
                    HasMeasurement = true;
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        // Da li je do sada stigla makar jedna vrednost merenja za ovaj entitet.
        public bool HasMeasurement
        {
            get { return _hasMeasurement; }
            set { SetProperty(ref _hasMeasurement, value); }
        }

        /// <summary>
        /// Validnost poslednje izmerene vrednosti.
        /// T3: IA putevi su validni do 15000, IB do 7000; sve iznad je nevalidno.
        /// (Negativne vrednosti tretiramo kao nevalidne.)
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (!HasMeasurement || _type == null)
                {
                    return true; // jos nema merenja -> ne prikazujemo alarm
                }

                return _value >= 0 && _value <= _type.MaxValidValue;
            }
        }

        public Entity()
        {
        }

        public Entity(int id, string name, EntityType type)
        {
            _id = id;
            _name = name;
            _type = type;
        }

        public override string ToString()
        {
            return Name + " (ID: " + Id + ")";
        }
    }
}

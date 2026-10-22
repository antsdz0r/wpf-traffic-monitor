using NetworkService.Helpers;

namespace NetworkService.Model
{
    
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
                    
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        
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

        
        public bool HasMeasurement
        {
            get { return _hasMeasurement; }
            set { SetProperty(ref _hasMeasurement, value); }
        }

       
        public bool IsValid
        {
            get
            {
                if (!HasMeasurement || _type == null)
                {
                    return true; 
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

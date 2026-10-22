using NetworkService.Helpers;

namespace NetworkService.Model
{
    /// <summary>
    /// Jedno polje (canvas) na Drag&Drop mrezi u Network Display pogledu.
    /// Moze biti prazno ili sadrzati jedan entitet.
    /// </summary>
    public class DisplaySlot : BindableBase
    {
        public int Index { get; }

        private Entity _entity;
        public Entity Entity
        {
            get { return _entity; }
            set
            {
                if (SetProperty(ref _entity, value))
                {
                    OnPropertyChanged(nameof(IsOccupied));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool IsOccupied => _entity != null;
        public bool IsEmpty => _entity == null;

        public DisplaySlot(int index)
        {
            Index = index;
        }
    }
}

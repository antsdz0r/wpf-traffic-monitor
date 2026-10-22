using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using NetworkService.Helpers;
using NetworkService.Model;
using NetworkService.Services;

namespace NetworkService.ViewModel
{
    
    public class MeasurementGraphViewModel : BindableBase
    {
        private readonly IShellContext _shell;

        
        private readonly Dictionary<Entity, List<MeasurementRecord>> _history =
            new Dictionary<Entity, List<MeasurementRecord>>();

        public IEnumerable<Entity> Entities => _shell.Entities;

        private Entity _selectedEntity;
        public Entity SelectedEntity
        {
            get { return _selectedEntity; }
            set
            {
                if (SetProperty(ref _selectedEntity, value))
                {
                    RefreshCurrentMeasurements();
                }
            }
        }

        
        public ObservableCollection<MeasurementRecord> CurrentMeasurements { get; } =
            new ObservableCollection<MeasurementRecord>();

        
        public ObservableCollection<TypeShare> TypeDistribution { get; } =
            new ObservableCollection<TypeShare>();

        
        public double MaxValidForSelected =>
            SelectedEntity?.Type?.MaxValidValue ?? 0;

       
        public event Action RedrawRequested;

        public MeasurementGraphViewModel(IShellContext shell)
        {
            _shell = shell;
            _shell.Entities.CollectionChanged += Entities_CollectionChanged;
            RecalculateTypeDistribution();
        }

        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Entities));
            RecalculateTypeDistribution();

            
            if (SelectedEntity != null && !_shell.Entities.Contains(SelectedEntity))
            {
                SelectedEntity = null;
            }
        }

       
        public void OnMeasurementReceived(Entity entity)
        {
            if (!_history.TryGetValue(entity, out var list))
            {
                list = new List<MeasurementRecord>();
                _history[entity] = list;
            }

            list.Add(new MeasurementRecord(entity.Value, DateTime.Now, entity.IsValid));

            
            while (list.Count > 5)
            {
                list.RemoveAt(0);
            }

            if (entity == SelectedEntity)
            {
                RefreshCurrentMeasurements();
            }
        }

        private void RefreshCurrentMeasurements()
        {
            CurrentMeasurements.Clear();
            if (SelectedEntity != null && _history.TryGetValue(SelectedEntity, out var list))
            {
                foreach (var record in list)
                {
                    CurrentMeasurements.Add(record);
                }
            }

            OnPropertyChanged(nameof(MaxValidForSelected));
            RedrawRequested?.Invoke();
        }

        private void RecalculateTypeDistribution()
        {
            TypeDistribution.Clear();
            int total = _shell.Entities.Count;

            foreach (EntityType type in _shell.Types)
            {
                int count = _shell.Entities.Count(en => en.Type == type);
                TypeDistribution.Add(new TypeShare
                {
                    TypeName = type.Name,
                    Count = count,
                    Percentage = total > 0 ? (count * 100.0 / total) : 0
                });
            }

            RedrawRequested?.Invoke();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using NetworkService.Helpers;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.Undo;

namespace NetworkService.ViewModel
{
    
    public class NetworkDisplayViewModel : BindableBase
    {
        public const int SlotCount = 12; 

        private readonly IShellContext _shell;

        public ObservableCollection<DisplaySlot> Slots { get; } = new ObservableCollection<DisplaySlot>();
        public ObservableCollection<TypeGroup> TreeGroups { get; } = new ObservableCollection<TypeGroup>();
        public ObservableCollection<Connection> Connections { get; } = new ObservableCollection<Connection>();

        
        public event Action RedrawRequested;

        
        private bool _isConnectMode;
        public bool IsConnectMode
        {
            get { return _isConnectMode; }
            set
            {
                if (SetProperty(ref _isConnectMode, value))
                {
                    _connectionSource = null;
                    OnPropertyChanged(nameof(ConnectHint));
                }
            }
        }

        private Entity _connectionSource;

        public string ConnectHint => IsConnectMode
            ? "Rezim povezivanja: kliknite dva entiteta na mrezi da ih povezete."
            : string.Empty;

        public MyICommand ArrangeAllCommand { get; }
        public MyICommand ToggleConnectModeCommand { get; }
        public MyICommand UndoCommand { get; }
        public MyICommand UndoAllCommand { get; }

        public NetworkDisplayViewModel(IShellContext shell)
        {
            _shell = shell;

            for (int i = 0; i < SlotCount; i++)
            {
                Slots.Add(new DisplaySlot(i));
            }

            ArrangeAllCommand = new MyICommand(ArrangeAll);
            ToggleConnectModeCommand = new MyICommand(() => IsConnectMode = !IsConnectMode);

            
            UndoCommand = ((MainWindowViewModel)shell).UndoLastCommand;
            UndoAllCommand = ((MainWindowViewModel)shell).UndoAllCommand;

            _shell.Entities.CollectionChanged += Entities_CollectionChanged;

            RebuildTree();
        }

        
        private void Entities_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Entity removed in e.OldItems)
                {
                    DisplaySlot slot = Slots.FirstOrDefault(s => s.Entity == removed);
                    if (slot != null)
                    {
                        slot.Entity = null;
                    }
                    RemoveConnectionsOf(removed);
                }
            }

            RebuildTree();
            RedrawRequested?.Invoke();
        }

        
        public void RebuildTree()
        {
            TreeGroups.Clear();
            foreach (EntityType type in _shell.Types)
            {
                var group = new TypeGroup(type.Name + " putevi");
                foreach (Entity entity in _shell.Entities.Where(en => en.Type == type && !IsOnGrid(en)))
                {
                    group.Items.Add(entity);
                }
                TreeGroups.Add(group);
            }
        }

        public bool IsOnGrid(Entity entity)
        {
            return Slots.Any(s => s.Entity == entity);
        }

       
        public void PlaceEntity(Entity entity, int slotIndex)
        {
            if (entity == null || slotIndex < 0 || slotIndex >= Slots.Count)
            {
                return;
            }

            DisplaySlot target = Slots[slotIndex];
            if (target.IsOccupied)
            {
                return; 
            }

            target.Entity = entity;
            RebuildTree();
            RedrawRequested?.Invoke();

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Postavljen \"" + entity.Name + "\" na polje " + (slotIndex + 1),
                () =>
                {
                    if (Slots[slotIndex].Entity == entity)
                    {
                        Slots[slotIndex].Entity = null;
                        RemoveConnectionsOf(entity);
                        RebuildTree();
                        RedrawRequested?.Invoke();
                    }
                }));

            NotificationService.Instance.Show(ToastType.Success, "Mreza",
                "Entitet \"" + entity.Name + "\" je postavljen na mrezu.");
        }

        
        public void MoveEntity(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0)
            {
                return;
            }

            DisplaySlot from = Slots[fromIndex];
            DisplaySlot to = Slots[toIndex];
            if (!from.IsOccupied || to.IsOccupied)
            {
                return;
            }

            Entity moved = from.Entity;
            to.Entity = moved;
            from.Entity = null;
            RedrawRequested?.Invoke();

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Premesten \"" + moved.Name + "\" na polje " + (toIndex + 1),
                () =>
                {
                    if (Slots[toIndex].Entity == moved && !Slots[fromIndex].IsOccupied)
                    {
                        Slots[fromIndex].Entity = moved;
                        Slots[toIndex].Entity = null;
                        RedrawRequested?.Invoke();
                    }
                }));
        }

        
        public void RemoveFromGrid(int slotIndex)
        {
            DisplaySlot slot = Slots[slotIndex];
            if (!slot.IsOccupied)
            {
                return;
            }

            Entity entity = slot.Entity;
            slot.Entity = null;
            RemoveConnectionsOf(entity);
            RebuildTree();
            RedrawRequested?.Invoke();

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Uklonjen \"" + entity.Name + "\" sa mreze",
                () =>
                {
                    if (!Slots[slotIndex].IsOccupied)
                    {
                        Slots[slotIndex].Entity = entity;
                        RebuildTree();
                        RedrawRequested?.Invoke();
                    }
                }));
        }

        
        public void HandleConnectClick(Entity entity)
        {
            if (!IsConnectMode || entity == null)
            {
                return;
            }

            if (_connectionSource == null)
            {
                _connectionSource = entity;
                return;
            }

            if (_connectionSource == entity)
            {
                _connectionSource = null; 
                return;
            }

            AddConnection(_connectionSource, entity);
            _connectionSource = null;
        }

        private void AddConnection(Entity a, Entity b)
        {
            
            if (Connections.Any(c => c.IsSamePair(a, b)))
            {
                NotificationService.Instance.Show(ToastType.Warning, "Veza",
                    "Veza izmedju ovih entiteta vec postoji.");
                return;
            }

            var connection = new Connection(a, b);
            Connections.Add(connection);
            RedrawRequested?.Invoke();

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Povezani \"" + a.Name + "\" i \"" + b.Name + "\"",
                () =>
                {
                    Connections.Remove(connection);
                    RedrawRequested?.Invoke();
                }));

            NotificationService.Instance.Show(ToastType.Success, "Veza",
                "Kreirana veza: " + a.Name + " - " + b.Name + ".");
        }

        private void RemoveConnectionsOf(Entity entity)
        {
            var toRemove = Connections.Where(c => c.Involves(entity)).ToList();
            foreach (var c in toRemove)
            {
                Connections.Remove(c);
            }
        }

        
        private void ArrangeAll()
        {
            var placed = new System.Collections.Generic.List<Tuple<int, Entity>>();

            foreach (Entity entity in _shell.Entities.Where(en => !IsOnGrid(en)).ToList())
            {
                DisplaySlot free = Slots.FirstOrDefault(s => s.IsEmpty);
                if (free == null)
                {
                    break; 
                }
                free.Entity = entity;
                placed.Add(Tuple.Create(free.Index, entity));
            }

            if (placed.Count == 0)
            {
                return;
            }

            RebuildTree();
            RedrawRequested?.Invoke();

            _shell.AddUndoableAction(new RelayUndoableAction(
                "Raspodeljeno " + placed.Count + " entiteta na mrezu",
                () =>
                {
                    foreach (var p in placed)
                    {
                        if (Slots[p.Item1].Entity == p.Item2)
                        {
                            Slots[p.Item1].Entity = null;
                            RemoveConnectionsOf(p.Item2);
                        }
                    }
                    RebuildTree();
                    RedrawRequested?.Invoke();
                }));

            NotificationService.Instance.Show(ToastType.Success, "Mreza",
                "Raspodeljeno " + placed.Count + " entiteta na slobodna polja.");
        }

        
        public void OnMeasurementReceived(Entity entity)
        {
            
            if (IsOnGrid(entity))
            {
                RedrawRequested?.Invoke();
            }
        }
    }
}

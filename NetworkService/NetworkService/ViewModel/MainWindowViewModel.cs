using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using NetworkService.Helpers;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.Undo;

namespace NetworkService.ViewModel
{
    
    public class MainWindowViewModel : BindableBase, IShellContext
    {
       
        public ObservableCollection<Entity> Entities { get; } = new ObservableCollection<Entity>();
        public ObservableCollection<EntityType> Types { get; } = new ObservableCollection<EntityType>();

        
        public ObservableCollection<IUndoableAction> History { get; } = new ObservableCollection<IUndoableAction>();

        
        public ObservableCollection<ToastNotification> Notifications => NotificationService.Instance.Notifications;

        
        public NetworkEntitiesViewModel EntitiesViewModel { get; }
        public NetworkDisplayViewModel DisplayViewModel { get; }
        public MeasurementGraphViewModel GraphViewModel { get; }

        private BindableBase _currentViewModel;
        public BindableBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set { SetProperty(ref _currentViewModel, value); }
        }

        private string _currentViewTitle;
        public string CurrentViewTitle
        {
            get { return _currentViewTitle; }
            set { SetProperty(ref _currentViewTitle, value); }
        }

        
        public MyICommand NavigateEntitiesCommand { get; }
        public MyICommand NavigateDisplayCommand { get; }
        public MyICommand NavigateGraphCommand { get; }
        public MyICommand UndoLastCommand { get; }
        public MyICommand UndoAllCommand { get; }

        private int _nextId = 1;

        public MainWindowViewModel()
        {
            SeedTypes();
            SeedEntities();

            
            NavigateEntitiesCommand = new MyICommand(() => NavigateTo(EntitiesViewModel, "Network Entities View"));
            NavigateDisplayCommand = new MyICommand(() => NavigateTo(DisplayViewModel, "Network Display View"));
            NavigateGraphCommand = new MyICommand(() => NavigateTo(GraphViewModel, "Measurement Graph View"));

            UndoLastCommand = new MyICommand(UndoLast, () => History.Count > 0);
            UndoAllCommand = new MyICommand(UndoAll, () => History.Count > 0);

            EntitiesViewModel = new NetworkEntitiesViewModel(this);
            DisplayViewModel = new NetworkDisplayViewModel(this);
            GraphViewModel = new MeasurementGraphViewModel(this);

            NavigateTo(EntitiesViewModel, "Network Entities View");

            CreateListener(); 
        }

        
        private void SeedTypes()
        {
            Types.Add(new EntityType("IA", 15000)); 
            Types.Add(new EntityType("IB", 7000)); 
        }

        
        private void SeedEntities()
        {
            Entities.Add(new Entity(GetNextAvailableId(), "Put A1", Types[0]));
            Entities.Add(new Entity(GetNextAvailableId(), "Put A2", Types[0]));
            Entities.Add(new Entity(GetNextAvailableId(), "Put B1", Types[1]));
            Entities.Add(new Entity(GetNextAvailableId(), "Put B2", Types[1]));
        }

        private void NavigateTo(BindableBase viewModel, string title)
        {
            CurrentViewModel = viewModel;
            CurrentViewTitle = title;
        }

        public int GetNextAvailableId()
        {
            
            return _nextId++;
        }

       
        public void AddUndoableAction(IUndoableAction action)
        {
            History.Add(action);
            UndoLastCommand.RaiseCanExecuteChanged();
            UndoAllCommand.RaiseCanExecuteChanged();
        }

        private void UndoLast()
        {
            if (History.Count == 0)
            {
                return;
            }

            var action = History[History.Count - 1];
            History.RemoveAt(History.Count - 1);
            action.Undo();

            UndoLastCommand.RaiseCanExecuteChanged();
            UndoAllCommand.RaiseCanExecuteChanged();

            NotificationService.Instance.Show(ToastType.Info, "Undo",
                "Ponistena akcija: " + action.Description);
        }

        private void UndoAll()
        {
            
            int count = History.Count;
            while (History.Count > 0)
            {
                var action = History[History.Count - 1];
                History.RemoveAt(History.Count - 1);
                action.Undo();
            }

            UndoLastCommand.RaiseCanExecuteChanged();
            UndoAllCommand.RaiseCanExecuteChanged();

            NotificationService.Instance.Show(ToastType.Info, "Undo sve",
                "Ponisteno akcija: " + count);
        }

        public void RestartSimulator()
        {
            SimulatorService.Restart();
        }

        public void OnMeasurementReceived(Entity entity)
        {
            
            GraphViewModel.OnMeasurementReceived(entity);
            DisplayViewModel.OnMeasurementReceived(entity);
        }

        
        private void CreateListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        byte[] bytes = new byte[1024];
                        int i = stream.Read(bytes, 0, bytes.Length);
                        string incomming = Encoding.ASCII.GetString(bytes, 0, i);

                        if (incomming.Equals("Need object count"))
                        {
                            
                            int currentCount = 0;
                            Application.Current.Dispatcher.Invoke(() => currentCount = Entities.Count);

                            Byte[] data = Encoding.ASCII.GetBytes(currentCount.ToString());
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            
                            ProcessMeasurement(incomming);
                        }

                        stream.Close();
                        tcpClient.Close();
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }

        private void ProcessMeasurement(string message)
        {
            try
            {
                
                string[] mainParts = message.Split(':');
                if (mainParts.Length < 2)
                {
                    return;
                }

                string indexPart = mainParts[0].Split('_')[1];
                int index = int.Parse(indexPart);
                double value = double.Parse(mainParts[1], CultureInfo.InvariantCulture);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (index < 0 || index >= Entities.Count)
                    {
                        return;
                    }

                    Entity entity = Entities[index];
                    entity.Value = value;         

                    
                    Logger.Log(entity.Id, entity.Name, value);

                    OnMeasurementReceived(entity);
                });
            }
            catch
            {
                
            }
        }
    }
}

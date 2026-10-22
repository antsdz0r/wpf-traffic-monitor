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
    /// <summary>
    /// Glavni ViewModel (shell). Drzi zajednicke podatke (entitete, tipove),
    /// upravlja navigacijom izmedju pogleda, globalnom istorijom akcija (Undo),
    /// i komunikacijom sa MeteringSimulator-om preko TCP-a.
    /// Ciljna grupa: CG4 (Sluzbenici) - Undo, Undo sve, History paleta, precice.
    /// </summary>
    public class MainWindowViewModel : BindableBase, IShellContext
    {
        // ---- Zajednicki podaci ----
        public ObservableCollection<Entity> Entities { get; } = new ObservableCollection<Entity>();
        public ObservableCollection<EntityType> Types { get; } = new ObservableCollection<EntityType>();

        // Globalna istorija svih izvrsenih akcija (CG4 - History paleta).
        public ObservableCollection<IUndoableAction> History { get; } = new ObservableCollection<IUndoableAction>();

        // Toast notifikacije se vezuju u MainWindow-u.
        public ObservableCollection<ToastNotification> Notifications => NotificationService.Instance.Notifications;

        // ---- Pod-ViewModel-i (kreiraju se jednom i cuvaju, da se stanje ne gubi) ----
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

        // ---- Navigacione i Undo komande ----
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

            // Komande se kreiraju PRE pod-ViewModel-a, jer Network Display View
            // koristi globalne Undo komande (izbegavamo referencu na null).
            NavigateEntitiesCommand = new MyICommand(() => NavigateTo(EntitiesViewModel, "Network Entities View"));
            NavigateDisplayCommand = new MyICommand(() => NavigateTo(DisplayViewModel, "Network Display View"));
            NavigateGraphCommand = new MyICommand(() => NavigateTo(GraphViewModel, "Measurement Graph View"));

            UndoLastCommand = new MyICommand(UndoLast, () => History.Count > 0);
            UndoAllCommand = new MyICommand(UndoAll, () => History.Count > 0);

            EntitiesViewModel = new NetworkEntitiesViewModel(this);
            DisplayViewModel = new NetworkDisplayViewModel(this);
            GraphViewModel = new MeasurementGraphViewModel(this);

            NavigateTo(EntitiesViewModel, "Network Entities View");

            CreateListener(); // povezivanje sa MeteringSimulator-om
        }

        // Unapred definisani tipovi za temu T3 (Dnevni saobracaj): IA i IB putevi.
        private void SeedTypes()
        {
            Types.Add(new EntityType("IA", "/Images/type_IA.png", 15000)); // IA: validno do 15000
            Types.Add(new EntityType("IB", "/Images/type_IB.png", 7000));  // IB: validno do 7000
        }

        // Minimum tri unapred kreirana entiteta sa validnim podacima (zahtev za odbranu).
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
            // Jedinstven Id: koristimo brojac koji nikad ne ponavlja vrednost.
            return _nextId++;
        }

        // ---- Undo / History ----
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
            // CG4: na Network Entities i Network Display pogledima moguce je ponistiti
            // sve do tada izvrsene akcije odjednom (od najnovije ka najstarijoj).
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
            // Prosledjujemo dogadjaj pogledima koji ga prikazuju u realnom vremenu.
            GraphViewModel.OnMeasurementReceived(entity);
            DisplayViewModel.OnMeasurementReceived(entity);
        }

        // ====================================================================
        //  TCP komunikacija sa MeteringSimulator-om (zadrzana logika iz starter
        //  projekta, uz dodatu obradu pristiglih merenja).
        // ====================================================================
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
                            // Saljemo UKUPAN broj entiteta (ne brojimo od nule).
                            int currentCount = 0;
                            Application.Current.Dispatcher.Invoke(() => currentCount = Entities.Count);

                            Byte[] data = Encoding.ASCII.GetBytes(currentCount.ToString());
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            // Format poruke: "Entitet_<index>:<vrednost>"
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
                // "Entitet_2:12345" -> index = 2, value = 12345
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
                    entity.Value = value;          // okida i IsValid i UI osvezavanje

                    // Upis u Log .txt (vreme, referenca na entitet, vrednost).
                    Logger.Log(entity.Id, entity.Name, value);

                    OnMeasurementReceived(entity);
                });
            }
            catch
            {
                // Neispravna poruka se ignorise da ne srusi listener.
            }
        }
    }
}

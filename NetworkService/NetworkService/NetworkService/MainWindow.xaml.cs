using System.Windows;
using NetworkService.ViewModel;

namespace NetworkService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Glavni ViewModel je DataContext celog prozora.
            DataContext = new MainWindowViewModel();
        }
    }
}

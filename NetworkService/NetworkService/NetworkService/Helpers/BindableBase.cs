using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetworkService.Helpers
{
    /// <summary>
    /// Bazna klasa za sve ViewModel-e i Model-e koji ucestvuju u DataBinding-u.
    /// Implementira INotifyPropertyChanged tako da UI moze automatski da se osvezi
    /// kada se vrednost neke osobine (property) promeni.
    /// </summary>
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Poziva se iz set-blokova property-ja. [CallerMemberName] automatski
        // ubacuje ime property-ja koji je pozvao metodu, pa ne moramo da kucamo string.
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Pomocna metoda: postavlja polje na novu vrednost i okida notifikaciju
        // samo ako se vrednost zaista promenila. Vraca true ako je doslo do promene.
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace NetworkService.Services
{
    /// <summary>
    /// Centralni servis za prikaz Toast notifikacija. View (MainWindow) se vezuje
    /// na kolekciju Notifications i prikazuje ih. Notifikacija se sama uklanja
    /// nakon nekoliko sekundi.
    /// </summary>
    public class NotificationService
    {
        // Singleton kako bi svi ViewModel-i slali notifikacije na isto mesto.
        private static readonly Lazy<NotificationService> _instance =
            new Lazy<NotificationService>(() => new NotificationService());

        public static NotificationService Instance => _instance.Value;

        public ObservableCollection<ToastNotification> Notifications { get; } =
            new ObservableCollection<ToastNotification>();

        private NotificationService()
        {
        }

        public void Show(ToastType type, string title, string message, int durationMs = 3500)
        {
            // Notifikacije se uvek dodaju na UI niti.
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            dispatcher.Invoke(() =>
            {
                var toast = new ToastNotification(type, title, message);
                Notifications.Add(toast);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    Notifications.Remove(toast);
                };
                timer.Start();
            });
        }
    }
}

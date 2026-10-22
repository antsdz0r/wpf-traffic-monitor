using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace NetworkService.Services
{
    
    public class NotificationService
    {
        
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

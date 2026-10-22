using NetworkService.Helpers;

namespace NetworkService.Services
{
    public enum ToastType
    {
        Success,
        Info,
        Warning
    }

    
    public class ToastNotification : BindableBase
    {
        public ToastType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public ToastNotification(ToastType type, string title, string message)
        {
            Type = type;
            Title = title;
            Message = message;
        }
    }
}

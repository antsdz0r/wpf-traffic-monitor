using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NetworkService.Services;

namespace NetworkService.Converters
{
    /// <summary>
    /// bool (IsValid) -> boja. Validno = zelena, nevalidno (van granica) = crvena.
    /// </summary>
    public class ValidityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isValid = value is bool b && b;
            return isValid
                ? new SolidColorBrush(Color.FromRgb(46, 160, 90))
                : new SolidColorBrush(Color.FromRgb(214, 69, 69));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// bool (IsValid) -> putanja statusne slike (normalno / opasnost).
    /// </summary>
    public class ValidityToStatusImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isValid = value is bool b && b;
            string path = isValid
                ? "/Images/status_ok.png"
                : "/Images/status_alarm.png";
            return new System.Windows.Media.Imaging.BitmapImage(
                new Uri("pack://application:,,," + path, UriKind.Absolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Obrce bool vrednost (npr. za omogucavanje kontrole kada nesto NIJE izabrano).
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }
    }

    /// <summary>
    /// null -> false, ne-null -> true. Koristi se za omogucavanje dugmadi
    /// (npr. "Obrisi selektovano" je aktivno samo kada je nesto selektovano).
    /// </summary>
    public class NotNullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// bool -> Visibility. parameter="invert" obrce logiku.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (parameter as string == "invert")
            {
                flag = !flag;
            }
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    /// <summary>
    /// ToastType -> boja pozadine notifikacije.
    /// </summary>
    public class ToastTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToastType type)
            {
                switch (type)
                {
                    case ToastType.Success: return new SolidColorBrush(Color.FromRgb(46, 160, 90));
                    case ToastType.Warning: return new SolidColorBrush(Color.FromRgb(214, 110, 40));
                    default: return new SolidColorBrush(Color.FromRgb(54, 126, 210));
                }
            }
            return new SolidColorBrush(Color.FromRgb(54, 126, 210));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

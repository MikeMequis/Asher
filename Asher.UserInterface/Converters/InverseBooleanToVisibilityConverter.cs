using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Asher.UserInterface.Converters
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value as bool? ?? false;

            // Default behavior
            bool useHidden = false;

            if (parameter != null)
            {
                var param = parameter.ToString().ToLower();

                if (param.Contains("hidden"))
                    useHidden = true;
                if (param.Contains("collapse"))
                    useHidden = false;
                if (param.Contains("invert"))
                    boolValue = !boolValue;
            }

            var invisible = useHidden ? Visibility.Hidden : Visibility.Collapsed;
            return boolValue ? invisible : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility != Visibility.Visible;

            return false;
        }
    }
}

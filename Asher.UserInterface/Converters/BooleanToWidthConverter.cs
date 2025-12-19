using System.Globalization;
using System.Windows.Data;

namespace Asher.UserInterface.Converters
{
    public class BooleanToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double expandedWidth) && 
                        double.TryParse(parts[1], out double collapsedWidth))
                    {
                        return isExpanded ? expandedWidth : collapsedWidth;
                    }
                }
            }
            return 250.0; // Default width
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
using System;
using System.Globalization;
using System.Windows.Data;

namespace CablePlugin.UI.Converters
{
    /// <summary>
    /// Парсинг double с поддержкой точки и запятой (0.1 и 0,01).
    /// </summary>
    public class FlexibleDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is double d)
                return d.ToString("0.##########", CultureInfo.CurrentCulture);
            if (value is float f)
                return f.ToString("0.##########", CultureInfo.CurrentCulture);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return 0.0;

            s = s.Trim().Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0.0;
        }
    }

    public class FlexibleNullableDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is double d)
                return d.ToString("0.##########", CultureInfo.CurrentCulture);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Trim().Replace(',', '.');
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return (double?)result;

            return null;
        }
    }
}

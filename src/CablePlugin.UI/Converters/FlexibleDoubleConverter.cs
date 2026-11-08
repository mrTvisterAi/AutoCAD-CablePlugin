using System;
using System.Globalization;
using System.Windows.Data;

namespace CablePlugin.UI.Converters
{
    /// <summary>
    /// Парсинг double: точка и запятая. Пока ввод неполный ("1.", "0,") — не трогаем binding.
    /// </summary>
    public class FlexibleDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is double d)
                return Format(d);
            if (value is float f)
                return Format(f);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim() ?? string.Empty;
            if (s.Length == 0)
                return 0.0;

            // Промежуточный ввод — не сбрасывать точку/запятую
            if (IsIncompleteNumber(s))
                return Binding.DoNothing;

            var normalized = s.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;

            return Binding.DoNothing;
        }

        private static string Format(double d)
        {
            // Без лишних нулей; десятичный разделитель — текущей культуры (обычно запятая в RU)
            return d.ToString("0.##########", CultureInfo.CurrentCulture);
        }

        internal static bool IsIncompleteNumber(string s)
        {
            if (s == "-" || s == "." || s == "," || s == "-." || s == "-,")
                return true;
            if (s.EndsWith(".") || s.EndsWith(","))
                return true;
            // "1." already covered; also "1.0e" style skip
            return false;
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
            var s = (value as string)?.Trim() ?? string.Empty;
            if (s.Length == 0)
                return null;

            if (FlexibleDoubleConverter.IsIncompleteNumber(s))
                return Binding.DoNothing;

            var normalized = s.Replace(',', '.');
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return (double?)result;

            return Binding.DoNothing;
        }
    }
}

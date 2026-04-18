using System.Globalization;

namespace IoTChat.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isOn = value is true;
            string color = parameter?.ToString()?.ToLower() ?? "";
            return color switch
            {
                "red" => isOn ? Color.FromArgb("#FF1744") : Color.FromArgb("#4A1A1A"),
                "yellow" => isOn ? Color.FromArgb("#FFD600") : Color.FromArgb("#4A4418"),
                "green" => isOn ? Color.FromArgb("#00E676") : Color.FromArgb("#1A4A2A"),
                _ => Colors.Gray
            };
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? 1.0 : 0.3;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? LayoutOptions.End : LayoutOptions.Start;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}

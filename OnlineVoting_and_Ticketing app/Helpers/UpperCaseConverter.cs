using System.Globalization;

namespace OnlineVoting_and_Ticketing_app.Helpers
{
    /// <summary>
    /// XAML value converter that returns the upper-case string representation
    /// of its input. Used mainly to display enum names in ALL CAPS labels.
    /// </summary>
    public class UpperCaseConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value?.ToString()?.ToUpperInvariant();

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

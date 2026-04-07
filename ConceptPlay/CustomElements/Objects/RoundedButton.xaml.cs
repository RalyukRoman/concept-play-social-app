using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ConceptPlay.CustomElements.Objects
{
    public class RoundedButton : Button
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register
            (
                nameof(CornerRadius), 
                typeof(CornerRadius),
                typeof(RoundedButton), 
                new PropertyMetadata(new CornerRadius(15))
            );

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
    }

    public class BrushOpacityConverter : IValueConverter
    {
        public object? Convert(
            object? value, Type targetType, 
            object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), 
                                    NumberStyles.Any, 
                                    CultureInfo.InvariantCulture, 
                                    out var factor))
                {
                    var color = brush.Color;

                    return new SolidColorBrush(Color.FromArgb
                    (
                        color.A,
                        (byte)(color.R * factor),
                        (byte)(color.G * factor),
                        (byte)(color.B * factor))
                    );
                }
            }

            return value;
        }

        public object ConvertBack(
            object? value, Type targetType,
            object? parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
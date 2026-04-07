using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConceptPlay.CustomElements.Objects
{
    /// <summary>
    /// Interaction logic for TextButton.xaml
    /// </summary>
    public class GeometryCheckBox : CheckBox
    {
        public static readonly DependencyProperty GeometryBoxProperty =
            DependencyProperty.Register
            (
                nameof(GeometryBox),
                typeof(Geometry),
                typeof(GeometryCheckBox),
                new PropertyMetadata(null)
            );

        public Geometry GeometryBox
        {
            get => (Geometry)GetValue(GeometryBoxProperty);
            set => SetValue(GeometryBoxProperty, value);
        }

        public static readonly DependencyProperty FillNotCheckedProperty =
            DependencyProperty.Register
            (
                nameof(FillNotChecked),
                typeof(Brush),
                typeof(GeometryCheckBox),
                new PropertyMetadata(Brushes.Black)
            );

        public Brush FillNotChecked
        {
            get => (Brush)GetValue(FillNotCheckedProperty);
            set => SetValue(FillNotCheckedProperty, value);
        }

        public static readonly DependencyProperty FillCheckedProperty =
            DependencyProperty.Register
            (
                nameof(FillChecked),
                typeof(Brush),
                typeof(GeometryCheckBox),
                new PropertyMetadata(Brushes.Black)
            );

        public Brush FillChecked
        {
            get => (Brush)GetValue(FillCheckedProperty);
            set => SetValue(FillCheckedProperty, value);
        }
    }
}
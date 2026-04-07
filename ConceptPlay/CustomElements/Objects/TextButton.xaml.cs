using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConceptPlay.CustomElements.Objects
{
    /// <summary>
    /// Interaction logic for TextButton.xaml
    /// </summary>
    public class TextButton : Button
    {
        public static readonly DependencyProperty HoverForegroundProperty =
            DependencyProperty.Register
            (
                nameof(HoverForeground), 
                typeof(Brush), 
                typeof(TextButton), 
                new PropertyMetadata(Brushes.Blue)
            );

        public Brush HoverForeground
        {
            get => (Brush)GetValue(HoverForegroundProperty);
            set => SetValue(HoverForegroundProperty, value);
        }
    }
}

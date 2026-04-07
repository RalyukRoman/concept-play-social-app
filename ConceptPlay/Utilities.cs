using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConceptPlay
{
    static class Utilities
    {
        public static void HandleException(Exception ex)
        {
            MessageBox.Show
            (
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        public static BitmapImage? GetBitmapImage(
            string imageDataBase64Str)
        {
            if (string.IsNullOrEmpty(imageDataBase64Str))
                return null;

            byte[] bytes = Convert.FromBase64String(imageDataBase64Str);
            using var ms = new MemoryStream(bytes);

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        public static async Task<bool> CheckValidityOfEmailAsync(string email)
        {
            var client = new HttpClient();

            string api = @"lbid0ie98id5dv4irf1loi2u2h9g6qrq02s0ku4pj85fotllkg4i8";
            string url = $@"https://anyapi.io/api/v1/email?email={email}&apiKey={api}";

            string content = await client.GetStringAsync(url);
            var js = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);

            return js?.valid == true;
        }

        // -- WPF METHODS --

        public static async Task HidePlaceholderTextAsync(
            TextBox textBox, TextBlock placeholder)
        {
            await Application.Current
                .Dispatcher.InvokeAsync(() =>
                {
                    placeholder.Visibility =
                        string.IsNullOrEmpty(textBox.Text)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                });
        }

        public static async Task<FormattedText?> GetFormattedTextAsync(
            dynamic element, string? text)
        {
            return await Application.Current
                .Dispatcher.InvokeAsync(() =>
                {
                    if (element is not (Button or TextBlock))
                        return null;

                    var typefaceOfTextBlock = new Typeface
                    (
                        element.FontFamily,
                        element.FontStyle,
                        element.FontWeight,
                        element.FontStretch
                    );

                    var formattedText = new FormattedText
                    (
                        text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        typefaceOfTextBlock,
                        element.FontSize,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(element).PixelsPerDip
                    );

                    return formattedText;
                });
        }

        // -- SCROLL --

        public static void HandleScrollBubbling(
            ListBox listBox, MouseWheelEventArgs e)
        {
            var listScroll = FindVisualChild<ScrollViewer>(listBox);
            var parentScroll = FindVisualParent<ScrollViewer>(listBox);

            if (listScroll == null || parentScroll == null)
                return;

            bool scrollUp = e.Delta > 0;
            bool scrollDown = e.Delta < 0;

            bool atTop =
                listScroll.VerticalOffset == 0;

            bool atBottom =
                listScroll.VerticalOffset >= listScroll.ScrollableHeight;

            if ((!scrollUp || !atTop) && (!scrollDown || !atBottom))
                return;

            e.Handled = true;

            parentScroll.RaiseEvent
            (
                new MouseWheelEventArgs(
                    e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = listBox
                }
            );
        }

        private static T? FindVisualChild<T>(
            DependencyObject obj) 
        where T : DependencyObject
        {
            int childrenCount =
                VisualTreeHelper.GetChildrenCount(obj);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);

                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);

                if (childResult != null)
                    return childResult;
            }

            return null;
        }

        private static T? FindVisualParent<T>(
            DependencyObject? child) 
        where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T result)
                    return result;

                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}

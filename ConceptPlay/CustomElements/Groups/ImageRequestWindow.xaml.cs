using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for ImageRequestWindow.xaml
    /// </summary>
    public partial class ImageRequestWindow : UserControl
    {
        private static readonly int _maxSizeOfImageInBytes = 1024 * 1024;

        private static readonly List<string> _allowableExtension =
            [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".ico"];

        private readonly MainWindow? _window;

        // -- CONSTRUCTOR --

        public ImageRequestWindow()
        {
            try
            {
                InitializeComponent();

                _window = Application.Current.MainWindow as MainWindow;

                ButtonClose.Click += ButtonClickHandler;
                ButtonOk.Click += ButtonClickHandler;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
        
        public event EventHandler<string>? Submitted;
        public event EventHandler? Closed;

        // -- METHODS --

        private bool CheckImagePath(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                TextBlockStatus.Text = "Image with this path do not exist";
                return false;
            }

            var fileInfo = new FileInfo(imagePath);

            if (fileInfo.Length > _maxSizeOfImageInBytes)
            {
                TextBlockStatus.Text = "Image size is more than 1 MB";
                return false;
            }

            if (!_allowableExtension.Contains(fileInfo.Extension))
            {
                TextBlockStatus.Text = "Image are in an invalid format";
                return false;
            }

            return true;
        }

        private async Task TryToIssuePath()
        {
            if (!CheckImagePath(TextBoxImagePath.Text))
            {
                TextBlockStatus.Visibility = Visibility.Visible;
                return;
            }

            string? fileName = await _window!.SocketManager!
                .SendImageToServerAsync(TextBoxImagePath.Text);

            if (fileName is null)
                return;

            Submitted?.Invoke(this, fileName);
            await Close();
        }

        private async Task Close()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                Visibility = Visibility.Collapsed;
                TextBlockStatus.Visibility = Visibility.Collapsed;
            });

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void CallPathSelectionWindow()
        {
            var openFile = new OpenFileDialog
            {
                Title = "Choose image",
                InitialDirectory = @"C:\"
            };

            if (openFile.ShowDialog() == true)
            {
                string imagePath = openFile.FileName;

                if (!CheckImagePath(imagePath))
                {
                    TextBlockStatus.Visibility = Visibility.Visible;
                    return;
                }

                TextBoxImagePath.Text = imagePath;
            }
        }

        // -- EVENTS --

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonClose):
                        await Close();
                        break;

                    case nameof(ButtonOk):
                        await TryToIssuePath();
                        break;

                    case nameof(ButtonPath):
                        CallPathSelectionWindow();
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void TextChangedImagePathHandler(
            object sender, TextChangedEventArgs e)
        {
            try
            {
                await Utilities.HidePlaceholderTextAsync(
                    TextBoxImagePath, PlaceholderTextImagePath);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ConceptPlay.Pages;

namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for TopPanel.xaml
    /// </summary>
    public partial class TopPanel
    {
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR --

        public TopPanel()
        {
            try
            {
                InitializeComponent();

                _window = Application.Current.MainWindow as MainWindow;

                _ = InitAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- METHODS --

        public async Task InitAsync()
        {
            await UpdateButtonsContentAsync();
            await SetLoginColumnWidthAsync();
        }

        public async Task UpdateButtonsContentAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ButtonLogin.Content =
                    _window!.UserInfo is not null
                        ? $"👤 {_window.UserInfo.Username}"
                        : $"🔑 Login";

                ButtonAdmin.Visibility =
                    _window!.UserInfo?.IsAdmin == true
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task SetLoginColumnWidthAsync()
        {
            FormattedText? formattedText = 
                await Utilities.GetFormattedTextAsync(
                    ButtonLogin, ButtonLogin.Content as string);

            if (formattedText is null) 
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                double maxByColumn = ColumnButtonLogin.MinWidth + 20;
                double maxByButton = formattedText.Width + 40;

                ColumnButtonLogin.MaxWidth =
                    maxByButton > maxByColumn
                        ? maxByButton
                        : maxByColumn;
            });
        }

        public async Task GoToNewConceptPage()
        {
            if (_window!.UserInfo is null)
            {
                MessageBox.Show("You are not logged in!");
                return;
            }

            await _window!.GoToPageAsync(
                new PageEditConcept(_window, null));
        }

        public async Task GoToUserPage()
        {
            Page? destinationPage =
                _window!.UserInfo != null
                    ? new PageUser(_window, _window.UserInfo.Id)
                    : new PageLogin(_window);

            await _window.GoToPageAsync(
                destinationPage);
        }

        // -- EVENTS --

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonLogin):
                        await GoToUserPage();
                        break;

                    case nameof(ButtonNewConcept):
                        await GoToNewConceptPage();
                        break;

                    case nameof(ButtonAdmin):
                        await _window!.GoToPageAsync(
                            new PageAdmin(_window));
                        break;

                    case nameof(ButtonHub): 
                        await _window!.GoToPageAsync(
                            new PageHub(_window));
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

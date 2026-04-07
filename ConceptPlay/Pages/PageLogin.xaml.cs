using System.Windows;
using System.Windows.Controls;
using DLL_ConnectionToDatabase;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageLogin.xaml
    /// </summary>
    public partial class PageLogin
    {
        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public PageLogin(MainWindow window)
        {
            try
            {
                InitializeComponent();

                _window = window;

                DbContext = DbMenu.CreateContext(
                    SocketManager.Ip,
                    SocketManager.DbLogin,
                    SocketManager.DbPassword);

                ButtonBack.Click += ButtonClickHandler;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- METHODS --

        private bool CheckUserInfo(UserInfo? user)
        {
            string enteredPassword = PasswordBox.Password;

            if (user is null)
            {
                MessageBox.Show("Invalid username!");
                return false;
            }

            if (user.Password != enteredPassword)
            {
                MessageBox.Show("Invalid password!");
                return false;
            }

            if (user.BannedUntil != null &&
                user.BannedUntil > DateOnly.FromDateTime(DateTime.Now))
            {
                MessageBox.Show($"This user has been banned until " +
                                $"{user.BannedUntil} by {user.BannedBy!.Username}");

                return false;
            }

            return true;
        }

        private async Task LoginUserAsync()
        {
            string username = 
                await Dispatcher.InvokeAsync(() =>
                    TextBoxUsername.Text);

            UserInfo? user = await DbMenu.FirstOrDefaultAsync<UserInfo>(
                x => x.Username == username, DbContext!);

            if (!CheckUserInfo(user))
                return;

            _window!.UserInfo = user;

            await _window.GoToPageAsync(
                new PageHub(_window));
        }

        // -- EVENTS --

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonContinue):
                        await LoginUserAsync();
                        break;

                    case nameof(ButtonRegistration):
                        await _window!.GoToPageAsync( 
                            new PageRegistration(_window));
                        break;

                    case nameof(ButtonBack):
                        await _window!.GoToPageAsync(
                            new PageHub(_window));
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

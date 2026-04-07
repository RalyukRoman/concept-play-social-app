using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Controls;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageRegistration.xaml
    /// </summary>
    public partial class PageRegistration
    {
        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public PageRegistration(MainWindow window)
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

        public bool CheckPassword()
        {
            string password = PasswordBox.Password;

            if ( password.Length < 8 ||
                !password.Any(char.IsUpper) ||
                !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit))
            {
                MessageBox.Show("Password must be at least 8 characters " +
                                "with uppercase, lowercase, and number");
                return false;
            }

            if (password == TextBoxUsername.Text)
            {
                MessageBox.Show("Password must not match username");
                return false;
            }

            if (password != PasswordBoxConfirm.Password)
            {
                MessageBox.Show("Confirmation password does not match");
                return false;
            }

            return true;
        }

        public bool CheckInputs()
        {
            if (string.IsNullOrWhiteSpace(TextBoxUsername.Text) ||
                string.IsNullOrWhiteSpace(TextBoxEmail.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("There are empty fields!");
                return false;
            }

            if (TextBoxUsername.Text.Length >= 30 ||
                TextBoxEmail.Text.Length >= 30 ||
                PasswordBox.Password.Length >= 30)
            {
                MessageBox.Show("Fields should be less than 30 characters");
                return false;
            }

            return true;
        }

        private async Task<bool> CheckUserExistsAsync()
        {
            string username =
                await Dispatcher.InvokeAsync(() =>
                    TextBoxUsername.Text);

            UserInfo? user = await DbMenu.FirstOrDefaultAsync<UserInfo>(
                x => x.Username.ToLower() == username.ToLower(),
                DbContext!);

            if (user is not null)
            {
                MessageBox.Show("Such an account already exists!");
                return false;
            }

            return true;
        }

        private async Task<bool> CheckEmailAsync()
        {
            string email =
                await Dispatcher.InvokeAsync(() =>
                    TextBoxEmail.Text);

            if (!await Utilities.CheckValidityOfEmailAsync(email))
            {
                MessageBox.Show("Email is not valid!");
                return false;
            }

            var user = await DbMenu.FirstOrDefaultAsync<UserInfo>(
                x => x.Email == email, DbContext!);

            if (user is not null)
            {
                MessageBox.Show("This email is already registered!");
                return false;
            }

            return true;
        }

        public async Task<UserInfo> CreateUserAsync()
        {
            var stats =
                await Dispatcher.InvokeAsync(() => new
                {
                    Username = TextBoxUsername.Text,
                    Email = TextBoxEmail.Text,
                    PasswordBox.Password
                });

            return new UserInfo
            {
                Username = stats.Username,
                Email = stats.Email,
                Password = stats.Password,
                CreationDate = DateTime.Now,
                IsAdmin = false
            };
        }

        public async Task InsertUserAsync()
        {
            if (!CheckInputs() || !CheckPassword())
                return;

            if (!await CheckUserExistsAsync())
                return;

            if (!await CheckEmailAsync())
                return;

            UserInfo newUser = await CreateUserAsync();

            await DbMenu.InsertAsync(
                newUser, DbContext!);

            _window!.UserInfo = await DbMenu.FirstOrDefaultAsync<UserInfo>(
                x => x.Username == newUser.Username, 
                DbContext!);

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
                    case nameof(ButtonCreate):
                        await InsertUserAsync();
                        break;

                    case nameof(ButtonBack):
                        await _window!.GoToPageAsync(
                            new PageLogin(_window));
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}
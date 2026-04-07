using System.Windows;
using System.Windows.Controls;
using DLL_ConnectionToDatabase;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageUser.xaml
    /// </summary>
    public partial class PageAdmin
    {
        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public PageAdmin(MainWindow window)
        {
            try
            {
                InitializeComponent();

                DataContext = this;
                _window = window;

                _ = InitAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- STATIC METHODS --

        private static bool CheckForAddGenre(
            string genreName, Genre? genre)
        {
            if (genreName.Length >= 30)
            {
                MessageBox.Show("Too long a genre name (more than 30 characters)");
                return false;
            }

            if (genre is not null)
            {
                MessageBox.Show("Such a genre already exists");
                return false;
            }

            return true;
        }

        // -- METHODS --

        private async Task InitAsync()
        {
            if (_window!.UserInfo is null || !_window.UserInfo.IsAdmin)
            {
                await _window.GoToPageAsync(
                    new PageHub(_window));

                return;
            }

            DbContext = DbMenu.CreateContext(
                SocketManager.Ip,
                SocketManager.DbLogin,
                SocketManager.DbPassword);

            await SetUiAsync();
        }

        private async Task SetUiAsync()
        {
            await SetComboBoxesUsersAsync();
            await SetComboBoxGenreAsync();
            await SetDatePickerAsync();
            await SetPromoteGroupBoxAsync();
        }

        private async Task SetPromoteGroupBoxAsync()
        {
            if (_window!.UserInfo!.Username == "ADMIN")
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                LabelPromoteUser.Visibility    = Visibility.Collapsed;
                ComboBoxPromoteUser.Visibility = Visibility.Collapsed;
                ButtonPromote.Visibility       = Visibility.Collapsed;
            });
        }

        private async Task SetComboBoxesUsersAsync()
        {
            List<UserInfo> usersList = await DbMenu
                .SelectAsync<UserInfo>(null, DbContext!);

            await Dispatcher.InvokeAsync(() =>
            {
                ComboBoxBanUser.Items.Clear();
                ComboBoxPromoteUser.Items.Clear();

                foreach (var users in usersList)
                {
                    ComboBoxBanUser.Items.Add(users.Username);
                    ComboBoxPromoteUser.Items.Add(users.Username);
                }
            });
        }

        private async Task SetComboBoxGenreAsync()
        {
            List<Genre> genresList = await DbMenu.SelectAsync<Genre>(
                null, DbContext!);

            genresList.Sort((x, y) =>
                String.CompareOrdinal(x.Name, y.Name));

            await Dispatcher.InvokeAsync(() =>
            {
                ComboBoxGenreName.Items.Clear();

                foreach (var genre in genresList)
                    ComboBoxGenreName.Items.Add(genre.Name);
            });
        }

        private async Task SetDatePickerAsync()
        {
            await Dispatcher.InvokeAsync(() =>
               DatePickerBanUser.DisplayDateStart = DateTime.Now);
        }

        private bool CheckForBan(UserInfo? selectedUser)
        {
            if (selectedUser is null)
                return false;

            if (selectedUser.Id == _window!.UserInfo?.Id)
            {
                MessageBox.Show("You cannot ban your account");
                return false;
            }

            if (selectedUser.IsAdmin)
            {
                MessageBox.Show("You cannot ban an admin");
                return false;
            }

            return true;
        }

        private bool CheckForPromote(UserInfo? selectedUser)
        {
            if (selectedUser is null)
                return false;

            if (_window!.UserInfo!.Username != "ADMIN")
                return false;

            if (selectedUser?.Id == _window!.UserInfo?.Id)
            {
                MessageBox.Show("You are already the main admin");
                return false;
            }

            if (selectedUser?.IsAdmin == true)
            {
                MessageBox.Show("This account is already an admin");
                return false;
            }

            return true;
        }

        private async Task<UserInfo?> FindUserFromComboBox(ComboBox comboBoxUser)
        {
            string selectedUsername = 
                await Dispatcher.InvokeAsync(() =>
                    comboBoxUser.Text);

            UserInfo? selectedUser = await DbMenu.FirstOrDefaultAsync<UserInfo>(
                x => x.Username == selectedUsername, DbContext!);

            return selectedUser;
        }

        private async Task DeleteUserAsync()
        {
            UserInfo? selectedUser = 
                await FindUserFromComboBox(ComboBoxBanUser);

            if (selectedUser is null)
                return;

            if (selectedUser.Id == _window!.UserInfo?.Id)
            {
                MessageBox.Show("You cannot delete your account");
                return;
            }

            await DbMenu.DeleteAsync(selectedUser, DbContext!);
        }

        private async Task<DateOnly?> GetBannedUntilDateAsync()
        {
            return await Dispatcher.InvokeAsync<DateOnly?>(() =>
            {
                if (!string.IsNullOrWhiteSpace(DatePickerBanUser.Text))
                    return null;

                if (!DateOnly.TryParse(DatePickerBanUser.Text, out var date))
                    return null;

                return date;
            });
        }

        private async Task BanUserAsync()
        {
            UserInfo? selectedUser = 
                await FindUserFromComboBox(ComboBoxBanUser);

            if (selectedUser is null)
                return;

            if (!CheckForBan(selectedUser))
                return;

            DateOnly? bannedUntilDate = 
                await GetBannedUntilDateAsync();

            if (bannedUntilDate is null) 
                return;

            selectedUser!.BannedById = _window!.UserInfo!.Id;
            selectedUser.BannedUntil = bannedUntilDate;

            await DbMenu.UpdateAsync(selectedUser, DbContext!);
        }

        private async Task PromoteUserAsync()
        {
            UserInfo? selectedUser =
                await FindUserFromComboBox(ComboBoxPromoteUser);

            if (!CheckForPromote(selectedUser))
                return;

            selectedUser!.IsAdmin = true;

            await DbMenu.UpdateAsync(selectedUser, DbContext!);
        }

        private async Task<Genre?> GetGenreByName(string? genreName = null)
        {
            genreName ??= 
                await Dispatcher.InvokeAsync(() => 
                    ComboBoxGenreName.Text);

            if (genreName is null)
                return null;

            return await DbMenu.FirstOrDefaultAsync<Genre>(
                x => x.Name == genreName, DbContext!);
        }

        private async Task SetVisibilityOfButtonsOfGenre(Genre? genre)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                bool hasText = !string.IsNullOrWhiteSpace(
                    ComboBoxGenreName.Text);

                ButtonUpdateGenre.Visibility = 
                    hasText && genre != null 
                        ? Visibility.Visible 
                        : Visibility.Hidden;

                ButtonDeleteGenre.Visibility = 
                    hasText && genre != null 
                        ? Visibility.Visible 
                        : Visibility.Hidden;

                ButtonAddGenre.Visibility = 
                    hasText && genre == null 
                        ? Visibility.Visible 
                        : Visibility.Hidden;
            });
        }

        private async Task SetGenreDescription(Genre? genre)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                TextBoxGenreDescription.Text = 
                    genre?.Description ?? "";
            });
        }

        private async Task AddGenreAsync()
        {
            var (genreName, genreDescription) = 
                await Dispatcher.InvokeAsync(() =>
                    (ComboBoxGenreName.Text, TextBoxGenreDescription.Text));

            var existingGenre = 
                await GetGenreByName(genreName);

            if (!CheckForAddGenre(genreName, existingGenre))
                return;

            var newGenre = new Genre()
            {
                Name = genreName,
                Description = genreDescription
            };

            await DbMenu.InsertAsync(newGenre, DbContext!);

            await Task.WhenAll
            (
                SetComboBoxGenreAsync(),
                SetVisibilityOfButtonsOfGenre(newGenre)
            );
        }

        private async Task UpdateGenreAsync()
        {
            string description =
                await Dispatcher.InvokeAsync(() =>
                    TextBoxGenreDescription.Text);

            Genre? genre = 
                await GetGenreByName();

            if (genre is null)
            {
                MessageBox.Show("No such genre found!");
                return;
            }

            genre.Description = description;

            await DbMenu.UpdateAsync(genre, DbContext!);
        }

        private async Task DeleteGenreAsync()
        {
            Genre? genre = 
                await GetGenreByName();

            if (genre is null)
            {
                MessageBox.Show("No such genre found!");
                return;
            }

            await DbMenu.DeleteAsync(genre, DbContext!);

            await Dispatcher.InvokeAsync(() =>
            {
                ComboBoxGenreName.Text = "";
                TextBoxGenreDescription.Text = "";
            });

            await Task.WhenAll
            (
                SetComboBoxGenreAsync(),
                SetVisibilityOfButtonsOfGenre(null)
            );
        }

        // -- EVENTS --

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonDelete):
                        await DeleteUserAsync();
                        break;

                    case nameof(ButtonBan):
                        await BanUserAsync();
                        break;

                    case nameof(ButtonPromote):
                        await PromoteUserAsync();
                        break;

                    case nameof(ButtonAddGenre):
                        await AddGenreAsync();
                        break;

                    case nameof(ButtonUpdateGenre):
                        await UpdateGenreAsync();
                        break;

                    case nameof(ButtonDeleteGenre):
                        await DeleteGenreAsync();
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void TextChangedHandler(
            object sender, TextChangedEventArgs e)
        {
            try
            {
                string genreName = ((TextBox)sender).Text;

                Genre? genre = 
                    await GetGenreByName(genreName);

                await Task.WhenAll
                (
                    SetGenreDescription(genre),
                    SetVisibilityOfButtonsOfGenre(genre)
                );
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private void ComboBoxLoadedHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = (TextBox) ComboBoxGenreName.Template
                    .FindName("PART_EditableTextBox", ComboBoxGenreName);
            
                textBox.TextChanged += TextChangedHandler;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

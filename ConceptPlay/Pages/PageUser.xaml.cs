using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageUser.xaml
    /// </summary>
    public partial class PageUser
    {
        public UserInfo? SelectedUser { get; private set; }
        public ObservableCollection<Concept> ConceptsCollection { get; set; } = [];

        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public PageUser(MainWindow window, int userId)
        {
            try
            {
                InitializeComponent();

                DataContext = this;
                _window = window;

                _ = InitAsync(userId);

                ButtonUploadImage.Click += ButtonClickHandler;
            }
            catch (Exception ex){ Utilities.HandleException(ex); }
        }

        // -- STATIC METHODS --

        private static bool ConfirmDeleteMessageBox()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete your account?",
                "Delete Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        private static bool ConfirmExitMessageBox()
        {
            var result = MessageBox.Show(
                "Do you want to exit?",
                "Exit Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        // -- METHODS --

        public async Task InitAsync(int userId)
        {
            DbContext = DbMenu.CreateContext(
                SocketManager.Ip,
                SocketManager.DbLogin,
                SocketManager.DbPassword);

            SelectedUser = await DbMenu.GetByIdAsync<UserInfo>(
                userId, DbContext!);

            if (SelectedUser is null)
            {
                await _window!.GoToPageAsync(
                    new PageHub(_window));

                return;
            }

            await SetUiAsync();
        }

        public async Task SetUiAsync()
        {
            await Task.WhenAll
            (
                 SetInfoAsync(),
                 SetAvatarAsync(),
                 LoadUserConceptsAsync(),
                 SetFunctionButtonsVisibilityAsync()
            );
        }

        public async Task SetInfoAsync()
        {
            var stats =  new
            {
                Username = 
                    SelectedUser?.Id != _window!.UserInfo?.Id &&
                    SelectedUser?.AvatarImage is null
                        ? $"👤 {SelectedUser!.Username}"
                        : $"{SelectedUser!.Username}",

                Email = $"✉️ {SelectedUser.Email}",
                Description = SelectedUser.Description ?? ""
            };

            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockUsername.Text  = stats.Username;
                TextBlockEmail.Text     = stats.Email;
                TextBoxDescription.Text = stats.Description;

                ButtonSave.Visibility = Visibility.Collapsed;
            });

            await Task.WhenAll
            (
                 SetColumnUsernameWidthAsync(),
                 SetStateOfDescriptionAsync()
            );
        }

        public async Task SetAvatarAsync()
        {
            string? imageName = SelectedUser!.AvatarImage;

            var bitmap = await _window!.SocketManager!
                .GetImageFromServerAsync(imageName);

            if (bitmap is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ImageAvatar.Source = bitmap;

                BorderAvatar.Visibility =
                    SelectedUser?.AvatarImage is not null
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task<List<Concept>?> SelectPageOfConceptsAsync()
        {
            return await Dispatcher.InvokeAsync(() =>
            {
                int currentPage = (int)ListControllerConcepts.CurrentPage;
                int count = SelectedUser!.Concepts.Count;

                int startIndex = (currentPage - 1) * 30;

                if (startIndex >= count)
                    return null;

                int take = Math.Min(30, count - startIndex);

                return SelectedUser.Concepts
                    .GetRange(startIndex, take);
            });
        }

        public async Task SetPageOfConceptsAsync()
        {
            List<Concept>? conceptsList =
                await SelectPageOfConceptsAsync();

            if (conceptsList is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ConceptsCollection.Clear();

                foreach (var concept in conceptsList)
                    ConceptsCollection.Add(concept);
            });
        }

        public async Task LoadUserConceptsAsync()
        {
            await SetConceptsVisibilityAsync();

            if (SelectedUser!.Concepts.Count == 0)
                return;

            await Task.Run(() =>
            {
                SelectedUser!.Concepts
                    .Sort((x, y) => 
                        y.ChangeDate.CompareTo(x.ChangeDate));
            });

            await ListControllerConcepts
                .SetMaxPagesAsync(SelectedUser!.Concepts.Count);

            await SetPageOfConceptsAsync();
        }

        public async Task SetStateOfDescriptionAsync()
        {
            bool isCurrentUserOrAdmin =
                _window!.UserInfo?.Id == SelectedUser?.Id ||
                _window.UserInfo?.IsAdmin == true;

            string userDescription =
                SelectedUser!.Description?.Trim() ?? "";

            await Dispatcher.InvokeAsync(() =>
            {
                TextBoxDescription.IsReadOnly = !isCurrentUserOrAdmin;

                PlaceholderTextDescription.Visibility =
                    string.IsNullOrEmpty(TextBoxDescription.Text) && isCurrentUserOrAdmin
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                ButtonSave.Visibility =
                    TextBoxDescription.Text.Trim() != userDescription && isCurrentUserOrAdmin
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task SetConceptsVisibilityAsync()
        {
            bool hasConcepts =
                SelectedUser!.Concepts.Count > 0;

            await Dispatcher.InvokeAsync(() =>
            {
                LabelConcepts.Visibility = hasConcepts 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;

                ListBoxConcepts.Visibility = hasConcepts 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            });
        }

        public async Task SetFunctionButtonsVisibilityAsync()
        {
            bool isCurrentUser =
                SelectedUser?.Id == _window!.UserInfo?.Id;

            bool isAdmin =
                _window!.UserInfo?.IsAdmin == true;

            await Dispatcher.InvokeAsync(() =>
            {
                ButtonExit.Visibility = isCurrentUser 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;

                ButtonDelete.Visibility = isCurrentUser || isAdmin 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;

                ButtonUploadImage.Visibility =
                    SelectedUser?.Id == _window!.UserInfo?.Id &&
                    SelectedUser?.AvatarImage is null
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task SetColumnUsernameWidthAsync()
        {
            var formattedText =
                await Utilities.GetFormattedTextAsync(
                    TextBlockUsername, TextBlockUsername.Text);

            if (formattedText is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ColumnUsername.MaxWidth = formattedText.Width + 15;
            });
        }

        private bool CheckDescription()
        {
            if (_window!.UserInfo is null)
                return false;

            if (TextBoxDescription.Text.Length >= 500)
            {
                MessageBox.Show("Too long a description " +
                                "(more than 500 characters)");

                return false;
            }

            return true;
        }

        private async Task UpdateDescriptionAsync()
        {
            if (!CheckDescription())
                return;

            SelectedUser!.Description = 
                TextBoxDescription.Text;

            await DbMenu.UpdateAsync(
                SelectedUser!, DbContext!);

            await SetStateOfDescriptionAsync();
        }

        private async Task UpdateAvatarImageAsync(
            string imageName)
        {
            SelectedUser!.AvatarImage = imageName;

            await DbMenu.UpdateAsync(
                SelectedUser!, DbContext!);

            await SetAvatarAsync();
        }

        private async Task DeleteUserAsync()
        {
            if (!ConfirmDeleteMessageBox())
                return;

            if (SelectedUser!.Id == _window!.UserInfo?.Id)
                _window!.UserInfo = null;

            await DbMenu.DeleteAsync(
                SelectedUser!, DbContext!);

            await _window.GoToPageAsync(
                new PageHub(_window));
        }

        private async Task LogOutUserAsync()
        {
            if(!ConfirmExitMessageBox())
                return;

            _window!.UserInfo = null;

            await _window.GoToPageAsync(
                new PageHub(_window));
        }

        private async Task OpenImageUploadWindow()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (ImageWindow.Visibility != Visibility.Visible)
                    ImageWindow.Visibility = Visibility.Visible;

                ScrollViewerContent.Effect = 
                    Application.Current.Resources["BlurMedium"] as Effect;

                ScrollViewerContent.IsEnabled = false;
            });
        }

        private async Task CloseImageUploadWindow()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (ImageWindow.Visibility != Visibility.Collapsed)
                    ImageWindow.Visibility = Visibility.Collapsed;

                ScrollViewerContent.Effect = null;

                ScrollViewerContent.IsEnabled = true;
            });
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

                    case nameof(ButtonExit):
                        await LogOutUserAsync();
                        break;

                    case nameof(ButtonSave):
                        await UpdateDescriptionAsync();
                        break;

                    case nameof(ButtonUploadImage):
                        await OpenImageUploadWindow();
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
                await SetStateOfDescriptionAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void SelectionChangedHandler(
            object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ListBoxConcepts.SelectedItem is Concept selectedConcept)
                {
                    await _window!.GoToPageAsync(
                        new PageConcept(_window, selectedConcept.Id));
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void PreviewMouseWheelHandler(
            object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (sender is not ListBox listBox)
                    return;

                await Dispatcher.InvokeAsync(() =>
                    Utilities.HandleScrollBubbling(listBox, e));
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void PageChangedHandler(
            object sender, EventArgs e)
        {
            try
            {
                await SetPageOfConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void SubmittedImageWindowHandler(
            object sender, string imageName)
        {
            try
            {
                await UpdateAvatarImageAsync(imageName);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void ClosedImageWindowHandler(
            object sender, EventArgs e)
        {
            try
            {
                await CloseImageUploadWindow();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void MouseUpHandler(
            object sender, MouseButtonEventArgs e)
        {
            try
            {
                await OpenImageUploadWindow();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

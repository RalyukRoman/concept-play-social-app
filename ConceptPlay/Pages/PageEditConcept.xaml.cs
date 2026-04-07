using System.Collections.ObjectModel;
using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageEditConcept.xaml
    /// </summary>
    public partial class PageEditConcept
    {
        public ObservableCollection<Genre> GenresCollection { get; set; } = [];
        public List<string> TempImages { get; set; } = [];
        public Concept? SelectedConcept { get; private set; }

        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        private bool _isCreated;

        // -- CONSTRUCTOR --

        public PageEditConcept(MainWindow window, int? conceptId)
        {
            try
            {
                InitializeComponent();

                DataContext = this;
                _window = window;

                _ = InitAsync(conceptId);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- STATIC METHODS --

        private static bool ConfirmDeleteMessageBox()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete this concept?",
                "Delete Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        // -- METHODS --

        private async Task InitAsync(int? conceptId)
        {
            if (_window!.UserInfo is null)
            {
                await _window.GoToPageAsync(
                    new PageHub(_window)); 

                return;
            }

            DbContext = DbMenu.CreateContext(
                SocketManager.Ip,
                SocketManager.DbLogin,
                SocketManager.DbPassword);

            await GetOrCreateConceptAsync(conceptId);

            if (_window.UserInfo.Id != SelectedConcept!.UserId &&
                !_window.UserInfo.IsAdmin)
            {
                await _window.GoToPageAsync(
                    new PageHub(_window));

                return;
            }

            await SetUiAsync();
        }

        private async Task SetUiAsync()
        {
            await Task.WhenAll
            (
                SetTextBoxesAsync(),
                SetComboBoxGenresAsync(),
                SetWrapPanelGenresAsync()
            );

            foreach (var image in SelectedConcept!.ConceptImages)
            {
                await SetImageAsync(image.Image);
            }
        }

        private async Task SetTextBoxesAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                TextBoxTitle.Text = SelectedConcept!.Title;
                TextBoxDescription.Text = SelectedConcept.Description ?? "";
            });
        }

        private async Task SetComboBoxGenresAsync()
        {
            List<Genre> genresList = await DbMenu
                .SelectAsync<Genre>(null, DbContext!);

            genresList.Sort((x, y) =>
                String.CompareOrdinal(x.Name, y.Name));

            await Dispatcher.InvokeAsync(() =>
            {
                ComboBoxGenres.Items.Clear();

                foreach (var genre in genresList)
                    ComboBoxGenres.Items.Add(genre.Name);
            });
        }

        private async Task SetWrapPanelGenresAsync()
        {
            SelectedConcept!.Genres.Sort((x, y) =>
                String.CompareOrdinal(x.Name, y.Name));

            await Dispatcher.InvokeAsync(() =>
            {
                GenresCollection.Clear();

                foreach (var genre in SelectedConcept!.Genres)
                    GenresCollection.Add(genre);
            });
        }

        public async Task GetOrCreateConceptAsync(int? conceptId)
        {
            if (conceptId is not null)
            {
                Concept? concept = await DbMenu
                    .GetByIdAsync<Concept>(
                        (int) conceptId, DbContext!);

                if (concept is not null)
                {
                    SelectedConcept = concept;
                    _isCreated = true;
                }
            }

            if (!_isCreated)
            {
                SelectedConcept = new Concept
                {
                    Title = "",
                    CreationDate = DateTime.Now,
                    UserId = _window!.UserInfo!.Id
                };
            }
        }

        public bool CheckInputs()
        {
            string conceptTitle = TextBoxTitle.Text;

            if (string.IsNullOrWhiteSpace(conceptTitle))
            {
                MessageBox.Show("There are empty fields!");
                return false;
            }

            if (conceptTitle.Length >= 50)
            {
                MessageBox.Show("The game title should not exceed 50 characters!");
                return false;
            }

            return true;
        }

        public async Task<bool> CheckUniquenessAsync()
        {
            string conceptTitle = TextBoxTitle.Text;

            if (_isCreated)
                return true;

            Concept? existingConcept = await DbMenu
                .FirstOrDefaultAsync<Concept>(
                    x => x.Title == conceptTitle, 
                    DbContext!);

            if (existingConcept is not null)
            {
                MessageBox.Show("That's title of game already!");
                return false;
            }

            return true;
        }

        private async Task UpdateConceptInfoAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                SelectedConcept!.Title = TextBoxTitle.Text.Trim();
                SelectedConcept.Description = TextBoxDescription.Text.Trim();
                SelectedConcept.ChangeDate = DateTime.Now;
            });
        }

        private async Task PublishConceptAsync()
        {
            if (!CheckInputs() || !await CheckUniquenessAsync())
                return;

            await UpdateConceptInfoAsync();

            if (!_isCreated)
            {
                SelectedConcept!.CreationDate = DateTime.Now;

                await DbMenu.InsertAsync(
                    SelectedConcept!, DbContext!);

                SelectedConcept = await DbMenu
                    .FirstOrDefaultAsync<Concept>(
                        x =>  x.Title == SelectedConcept!.Title, 
                        DbContext!);
            }
            else
            {
                await DbMenu.UpdateAsync(
                    SelectedConcept!, DbContext!);
            }

            _isCreated = true;

            foreach (string imageName in TempImages)
            {
                await AddImageAsync(imageName, true);
            }

            await _window!.GoToPageAsync(
                new PageConcept(_window, SelectedConcept!.Id));
        }

        private async Task DeleteConceptAsync()
        {
            if (!ConfirmDeleteMessageBox())
                return;

            if (_isCreated)
            {
                await DbMenu.DeleteAsync(
                    SelectedConcept!, DbContext!);
            }

            await _window!.GoToPageAsync(
                new PageHub(_window));
        }

        private async Task AddGenreAsync()
        {
            string selectedGenreName =
                await Dispatcher.InvokeAsync(() =>
                    (string)ComboBoxGenres.SelectedItem);

            Genre? genre = await DbMenu
                .FirstOrDefaultAsync<Genre>(
                    x => x.Name == selectedGenreName, 
                    DbContext!);

            if (genre is null)
                return;

            if (SelectedConcept!.Genres.Contains(genre))
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                SelectedConcept.Genres.Add(genre);
                GenresCollection.Add(genre);
            });
        }

        private async Task RemoveGenreAsync(object sender)
        {
            if (sender is not FrameworkElement { DataContext: Genre genre })
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                GenresCollection.Remove(genre);
                SelectedConcept?.Genres.Remove(genre);
            });
        }

        private async Task SetImageAsync(
            string imageName)
        {
            var bitmap = await _window!.SocketManager!
                .GetImageFromServerAsync(imageName);

            if (bitmap is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                var item = new Image()
                {
                    Tag = imageName,
                    Source = bitmap,
                    Height = 100,
                    Margin = new Thickness(10, 0, 10, 0),
                };

                item.MouseRightButtonDown += MouseRigthClickHandler;

                StackPanelImages.Children.Add(item);
            });
        }

        private async Task AddImageAsync(
            string imageName, bool isOnlyAdd = false)
        {
            if (_isCreated)
            {
                var item = new ConceptImages()
                {
                    Image = imageName,
                    ConceptId = SelectedConcept!.Id
                };

                await DbMenu.InsertAsync(
                    item, DbContext!);
            }
            else
                TempImages.Add(imageName);

            if (!isOnlyAdd)
                await SetImageAsync(imageName);
        }

        private async Task DeleteSelectedImageAsync(
            string imageName)
        {
            if (_isCreated)
            {
                var conceptImage = await DbMenu.FirstOrDefaultAsync<ConceptImages>(
                x => x.Image == imageName &&
                     x.ConceptId == SelectedConcept!.Id,
                DbContext!);

                if (conceptImage is null)
                    return;

                await DbMenu.DeleteAsync(
                    conceptImage, DbContext!);
            }
            else
                TempImages.Remove(imageName);
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

        private async void TextChangedHandler(
            object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = (TextBox) sender;

                var placeholder = 
                    textBox == TextBoxTitle
                        ? PlaceholderTextTitle
                        : PlaceholderTextDescription;

                await Utilities.HidePlaceholderTextAsync(
                    textBox, placeholder);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonGenre):
                        await AddGenreAsync();
                        break;

                    case nameof(ButtonPublish):
                        await PublishConceptAsync();
                        break;

                    case nameof(ButtonDelete):
                        await DeleteConceptAsync();
                        break;

                    case nameof(ButtonAddImage):
                        await OpenImageUploadWindow();
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void GenreBlockClickHandler
            (object sender, MouseButtonEventArgs e)
        {
            try
            {
                await RemoveGenreAsync(sender);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void SubmittedImageWindowHandler(
            object sender, string imageName)
        {
            try
            {
                await AddImageAsync(imageName);
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

        private async void MouseRigthClickHandler(
            object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Image image && 
                    StackPanelImages.Children.Contains(image))
                {
                    StackPanelImages.Children.Remove(image);
                    await DeleteSelectedImageAsync((string)image!.Tag);
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}
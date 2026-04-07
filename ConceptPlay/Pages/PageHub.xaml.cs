using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using ConceptPlay.CustomElements.Groups;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageHub.xaml
    /// </summary>
    public partial class PageHub
    {
        private readonly List<Genre> _selectedGenres = [];
        private List<Concept> _conceptsList = [];

        public ObservableCollection<Concept> ConceptsCollection { get; set; } = [];
        public ObservableCollection<Genre> GenresCollection { get; set; } = [];

        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public PageHub(MainWindow window)
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

        // -- METHODS --

        private async Task InitAsync()
        {
            DbContext = DbMenu.CreateContext(
                SocketManager.Ip,
                SocketManager.DbLogin,
                SocketManager.DbPassword);

            await SetUiAsync();
        }

        private async Task SetUiAsync()
        {
            await Task.WhenAll
            (
                SetGenresAsync(),
                SetConceptsAsync()
            );
        }

        private async Task SetConceptsAsync()
        {
            _conceptsList = await DbMenu
                .SelectAsync<Concept>(null, DbContext!);

            await FilterConceptsByGenresAsync();
            await FilterConceptsByTitleAsync();
            await SortConceptsAsync();
            await SortGenresOfConceptsAsync();

            await ListControllerConcepts
                .SetMaxPagesAsync(_conceptsList.Count);

            await InsertPageOfConceptsAsync();
        }

        private async Task FilterConceptsByGenresAsync()
        {
            if (_selectedGenres.Count == 0) 
                return;

            await Task.Run(() =>
            {
                _conceptsList = _conceptsList
                    .Where(concept => _selectedGenres
                        .All(selGenre => concept.Genres
                            .Any(conGenre => conGenre.Id == selGenre.Id)))
                    .ToList();
            });
        }

        private async Task FilterConceptsByTitleAsync()
        {
            string enteredTitle = TextBoxConcept.Text;

            if (string.IsNullOrWhiteSpace(enteredTitle))
                return;

            await Task.Run(() =>
            {
                _conceptsList = _conceptsList
                    .FindAll(x => x.Title
                        .Contains(enteredTitle));
            });
        }

        private async Task SortConceptsAsync()
        {
            var selectedSort =
                await GetSelectedSortRadBtnAsync();

            if (selectedSort?.Content is not string sortBy)
                sortBy = "By Newest";

            await Task.Run(() =>
            {
                _conceptsList.Sort(sortBy switch
                {
                    "By Title" => (a, b) =>
                        string.Compare(a.Title, b.Title, StringComparison.Ordinal),

                    "By Views" => (a, b) =>
                        b.Reviews.Count
                            .CompareTo(a.Reviews.Count),

                    "By Likes" => (a, b) =>
                        b.Reviews.FindAll(x => x.IsLiked).Count
                            .CompareTo(a.Reviews.FindAll(x => x.IsLiked).Count),

                    "By Newest" => (a, b) =>
                        b.CreationDate
                            .CompareTo(a.CreationDate),

                    "By Oldest" => (a, b) =>
                        a.CreationDate
                            .CompareTo(b.CreationDate),

                    _ => throw new ArgumentOutOfRangeException()
                });
            });
        }

        private async Task SortGenresOfConceptsAsync()
        {
            await Task.Run(() =>
            {
                foreach (var concept in _conceptsList)
                    concept.Genres.Sort((x, y) =>
                        String.CompareOrdinal(x.Name, y.Name));
            });
        }

        private async Task<RadioButton?> GetSelectedSortRadBtnAsync()
        {
            return await Dispatcher.InvokeAsync(() =>
                RadioGroupSort.Children
                    .OfType<RadioButton>()
                    .FirstOrDefault(x => x.IsChecked == true));
        }

        private async Task<List<Concept>?> GetCurrentPageConceptsAsync()
        {
            return await Dispatcher.InvokeAsync(() =>
            {
                int currentPage = (int) ListControllerConcepts.CurrentPage;

                int startIndex = (currentPage - 1) * 30;

                if (startIndex >= _conceptsList.Count)
                    return null;

                int endIndex = currentPage * 30 - 1;

                if (endIndex >= _conceptsList.Count)
                    endIndex = _conceptsList.Count - 1;

                return _conceptsList
                    .GetRange(startIndex, endIndex - startIndex + 1);
            });
        }

        private async Task InsertPageOfConceptsAsync()
        {
            List<Concept>? conceptsList = 
                await GetCurrentPageConceptsAsync();

            await Dispatcher.InvokeAsync(() =>
            {
                ConceptsCollection.Clear();

                if (conceptsList is null)
                    return;

                foreach (Concept concept in conceptsList)
                    ConceptsCollection.Add(concept);
            });
        }

        private async Task SetGenresAsync()
        {
            List<Genre> genresList = await DbMenu
                .SelectAsync<Genre>(null, DbContext!);

            genresList.Sort((x, y) => 
                String.CompareOrdinal(x.Name, y.Name));

            await Dispatcher.InvokeAsync(() =>
            {
                GenresCollection.Clear();

                foreach (var genre in genresList)
                    GenresCollection.Add(genre);
            });
        }

        private async Task ToggleGenreSelectionAsync(object sender)
        {
            if (sender is not FrameworkElement { DataContext: Genre genre } genreBlock)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                bool isContains = _selectedGenres.Contains(genre);

                if (isContains)
                {
                    _selectedGenres.Remove(genre);

                    if (genreBlock is GenreBlock gb)
                    {
                        gb.Foreground = Brushes.Black;
                        gb.BorderBasic.BorderBrush = Brushes.Black;
                    }
                }
                else
                {
                    _selectedGenres.Add(genre);

                    if (genreBlock is GenreBlock gb)
                    {
                        gb.Foreground = Brushes.Green;
                        gb.BorderBasic.BorderBrush = Brushes.Green;
                    }
                }
            });
        }

        // -- EVENTS --

        private async void RadioButtonCheckedHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                await SetConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void GenreClickHandler(
            object sender, MouseButtonEventArgs e)
        {
            try
            {
                await ToggleGenreSelectionAsync(sender);
                await SetConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void PageChangedHandler(
            object sender, EventArgs e)
        {
            try
            {
                await InsertPageOfConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void ListBoxSelectionChangedHandler(
            object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ListBoxConcepts.SelectedItem is Concept concept)
                {
                    await _window!.GoToPageAsync(
                        new PageConcept(_window, concept.Id));
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void TextBoxKeyDownHandler(
            object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                    await SetConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                await SetConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

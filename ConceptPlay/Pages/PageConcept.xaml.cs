using System.Collections.ObjectModel;
using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace ConceptPlay.Pages
{
    /// <summary>
    /// Interaction logic for PageConcept.xaml
    /// </summary>
    public partial class PageConcept
    {
        public ObservableCollection<Genre> GenresCollection { get; set; } = [];
        public ObservableCollection<int> CommentIdsCollection { get; set; } = [];

        public Concept? SelectedConcept { get; private set; }
        public Review? CurrentReview { get; private set; }

        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR --    

        public PageConcept(MainWindow window, int conceptId)
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

        // -- METHODS --

        public async Task InitAsync(int conceptId)
        {
            DbContext = DbMenu.CreateContext(
                SocketManager.Ip,
                SocketManager.DbLogin,
                SocketManager.DbPassword);

            SelectedConcept = await DbMenu.GetByIdAsync<Concept>(
                conceptId, DbContext!);

            if (SelectedConcept == null)
            {
                await _window!.GoToPageAsync(
                    new PageHub(_window));

                return;
            }

            await GetOrCreateReviewAsync();
            await SetUiAsync();
        }

        private async Task GetOrCreateReviewAsync()
        {
            if (_window!.UserInfo is null)
                return;

            CurrentReview = await DbMenu.FirstOrDefaultAsync<Review>(
                x => x.ConceptId == SelectedConcept!.Id &&
                     x.UserId == _window.UserInfo.Id, 
                DbContext!);

            if (CurrentReview is null)
            {
                CurrentReview = new Review
                {
                    IsLiked = false,
                    UserId = _window.UserInfo.Id,
                    ConceptId = SelectedConcept!.Id
                };

                await DbMenu.InsertAsync(
                    CurrentReview, DbContext!);
            }
        }

        public async Task SetUiAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ButtonEdit.Visibility = 
                    SelectedConcept?.UserId != _window!.UserInfo?.Id &&
                    _window.UserInfo?.IsAdmin != true
                       ? Visibility.Collapsed
                       : Visibility.Visible;
            });

            await Task.WhenAll
            (
                SetTextBlocksInfoAsync(),
                SetAvatarAsync(),
                SetLikeAsync(),
                SetCommentsAsync(),
                SetGenresAsync(),
                SetImagesAsync()
            );
        }

        public async Task SetTextBlocksInfoAsync()
        {
            var stats = new
            {
                Author = SelectedConcept!.User.AvatarImage is null
                    ? $"👤 {SelectedConcept!.User.Username}"
                    : $"{SelectedConcept!.User.Username}",

                Title       = $"{SelectedConcept!.Title}",
                Description = $"{SelectedConcept.Description}",

                Views       = $"👁️ {SelectedConcept.Reviews.Count}",
                Comments    = $"💬 {SelectedConcept.Comments.Count}",

                CreateDate  = $"📅 {SelectedConcept.CreationDate}",
                ChangeDate  = $"⚙️ {SelectedConcept.ChangeDate}"
            };

            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockTitle.Text       = stats.Title;
                ButtonAuthor.Content      = stats.Author;
                TextBlockDescription.Text = stats.Description;

                TextBlockViews.Text       = stats.Views;
                TextBlockComments.Text    = stats.Comments;

                TextBlockCreateDate.Text  = stats.CreateDate;
                TextBlockChangeDate.Text  = stats.ChangeDate;

                if (string.IsNullOrWhiteSpace(TextBlockDescription.Text))
                    TextBlockDescription.Visibility = Visibility.Collapsed;
            });
        }

        public async Task SetAvatarAsync()
        {
            string? imageName = SelectedConcept!.User.AvatarImage;
            var bitmap = await _window!.SocketManager!.GetImageFromServerAsync(imageName);

            if (bitmap is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ImageAvatar.Source = bitmap;

                BorderAvatar.Visibility =
                    SelectedConcept!.User.AvatarImage is not null
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task SetLikeAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                CheckedBoxLike.Content = SelectedConcept!.Reviews
                    .FindAll(x => x.IsLiked).Count
                    .ToString();

                CheckedBoxLike.IsChecked = CurrentReview?.IsLiked;
                CheckedBoxLike.IsEnabled = CurrentReview != null;
            });
        }

        public async Task<List<Comment>?> GetCurrentPageCommentsAsync()
        {
            int currentPage = await Dispatcher.InvokeAsync(() => 
                (int)ListControllerComments.CurrentPage);

            int count = SelectedConcept!.Comments.Count;

            int startIndex = (currentPage - 1) * 30;

            if (startIndex >= count)
                return null;

            int take = Math.Min(30, count - startIndex);

            return SelectedConcept.Comments
                .GetRange(startIndex, take);
        }

        public async Task InsertPageOfConceptsAsync()
        {
            var commentsList = await GetCurrentPageCommentsAsync();

            if (commentsList == null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                CommentIdsCollection.Clear();

                foreach (var comment in commentsList)
                    CommentIdsCollection.Add(comment.Id);
            });
        }

        public async Task SetCommentsAsync()
        {
            SelectedConcept!.Comments
                .Sort((x, y) =>
                    y.CreationDate.CompareTo(x.CreationDate));

            await ListControllerComments
                .SetMaxPagesAsync(SelectedConcept!.Comments.Count);

            await InsertPageOfConceptsAsync();
        }

        public async Task SetGenresAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                GenresCollection.Clear();

                if (SelectedConcept!.Genres.Count == 0)
                {
                    WrapPanelGenres.Visibility = Visibility.Collapsed;
                    return;
                }

                foreach (Genre genre in SelectedConcept!.Genres)
                    GenresCollection.Add(genre);
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

                StackPanelImages.Children.Add(item);
            });
        }

        public async Task SetImagesAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ScrollViewerImages.Visibility =
                    SelectedConcept!.ConceptImages.Count == 0
                        ? Visibility.Collapsed
                        : Visibility.Visible;
            });

            foreach (var image in SelectedConcept!.ConceptImages)
            {
                await SetImageAsync(image.Image);
            }
        }

        private bool CheckCommentContent(string commentContent)
        {
            if (string.IsNullOrWhiteSpace(commentContent))
                return false;

            if (_window!.UserInfo is null)
            {
                MessageBox.Show("Log in to your account!");
                return false;
            }

            if (commentContent.Length >= 300)
            {
                MessageBox.Show("Too long a comment (more than 300 characters)");
                return false;
            }

            return true;
        }

        private async Task SendCommentAsync()
        {
            string commentContent = 
                await Dispatcher.InvokeAsync(() =>
                {
                    string content = TextBoxComment.Text;
                    TextBoxComment.Text = string.Empty;

                    return content;
                });

            if (!CheckCommentContent(commentContent))
                return;

            Comment newComment = new()
            {
                Content = commentContent,
                CreationDate = DateTime.Now,
                UserId = _window!.UserInfo!.Id,
                ConceptId = SelectedConcept!.Id
            };

            await DbMenu.InsertAsync(newComment, DbContext!);

            SelectedConcept = await DbMenu.GetByIdAsync<Concept>(
                SelectedConcept!.Id, DbContext!);

            await SetCommentsAsync();
        }

        private async Task ChangeLikeAsync()
        {
            if (CurrentReview is null)
                return;

            await Dispatcher.InvokeAsync(() =>
                CurrentReview.IsLiked = CheckedBoxLike.IsChecked is true);

            await DbMenu.UpdateAsync(CurrentReview, DbContext!);

            SelectedConcept!.Reviews
                .Find(x => x.Id == CurrentReview.Id)!
                .IsLiked = CurrentReview.IsLiked;

            await SetLikeAsync();
        }

        // -- EVENTS --

        private async void ButtonClickHandler(
           object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonComment):
                        await SendCommentAsync();
                        break;

                    case nameof(CheckedBoxLike):
                        await ChangeLikeAsync();
                        break;

                    case nameof(ButtonEdit):
                        await _window!.GoToPageAsync(
                            new PageEditConcept(_window, SelectedConcept?.Id));
                        break;

                    case nameof(ButtonAuthor):
                        await _window!.GoToPageAsync(
                            new PageUser(_window, SelectedConcept!.UserId));
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void TextBoxKeyDownHandler(
            object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key != Key.Enter)
                    return;

                await SendCommentAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void CheckedChangeHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                await ChangeLikeAsync();
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
                await InsertPageOfConceptsAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void CommentDeletedHandler(
            object? sender, EventArgs e)
        {
            try
            {
                await InitAsync(SelectedConcept!.Id);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

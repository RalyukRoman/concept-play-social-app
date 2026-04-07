using DLL_ConnectionToDatabase;
using System.Windows;
using System.Windows.Controls;
using ConceptPlay.CustomElements.Objects;
using ConceptPlay.Pages;

namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for CommentBlock.xaml
    /// </summary>
    public partial class CommentBlock
    {
        public Comment? SelectedComment { get; set; }

        public AppDbContext? DbContext;
        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public CommentBlock()
        {
            try 
            {
                InitializeComponent();

                _window = Application.Current.MainWindow as MainWindow;

                DataContextChanged += DataContextChangedHandler;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void DataContextChangedHandler(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                int? commentId =  e.NewValue as int?;

                if (commentId is null)
                    return;

                await InitAsync((int) commentId);
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- STATIC METHODS --

        private static bool ConfirmDeleteMessageBox()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete this comment?",
                "Delete Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }

        // -- METHODS --

        private async Task InitAsync(int commentId)
        {
            DbContext = DbMenu.CreateContext(
                SocketManager.Ip, 
                SocketManager.DbLogin, 
                SocketManager.DbPassword);

            SelectedComment = await DbMenu
                .GetByIdAsync<Comment>(commentId, DbContext!);

            if (SelectedComment is null)
                return;

            await Task.WhenAll
            (
                SetCommentInfoAsync(),
                SetAvatarAsync(),
                UpdateLikeCountsAsync(),
                SetDeleteButtonVisibilityAsync(),
                SetAuthorColumnWidthAsync()
            );
        }

        private async Task SetCommentInfoAsync()
        {
            var info = new
            {
                Commentator = SelectedComment!.User.AvatarImage is null
                    ? $"👤 {SelectedComment!.User.Username}"
                    : $"{SelectedComment.User.Username}",

                Content     = $"{SelectedComment!.Content}",
                CreateDate  = $"📅 {SelectedComment.CreationDate}",
            };

            await Dispatcher.InvokeAsync(() =>
            {
                ButtonCommentator.Content = info.Commentator;
                TextBlockContent.Text     = info.Content;
                TextBlockCreateDate.Text  = info.CreateDate;
            });
        }

        private async Task SetAvatarAsync()
        {
            string? imageName = SelectedComment!.User.AvatarImage;
            var bitmap = await _window!.SocketManager!.GetImageFromServerAsync(imageName);

            if (bitmap is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                ImageAvatar.Source = bitmap;

                BorderAvatar.Visibility =
                    SelectedComment!.User.AvatarImage is not null
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        private async Task UpdateLikeCountsAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                CheckedBoxDislike.Content = 
                    SelectedComment!.NumberOfDislikes;

                CheckedBoxLike.Content = 
                    SelectedComment.NumberOfLikes;
            });
        }

        private async Task SetDeleteButtonVisibilityAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                ButtonDelete.Visibility = CanDeleteComment()
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        public async Task SetAuthorColumnWidthAsync()
        {
            var formattedText =
                await Utilities.GetFormattedTextAsync(
                    ButtonCommentator, ButtonCommentator.Content as string);

            if (formattedText is null)
                return;

            await Dispatcher.InvokeAsync(() =>
                ColumnCommentator.MaxWidth = formattedText.Width + 15);
        }

        private async Task ChangeCheckedOfLikeAsync(
            bool isLike, bool isAdd)
        {
            if (SelectedComment is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                if (isLike)
                {
                    SelectedComment.NumberOfLikes += isAdd ? 1 : -1;

                    if (isAdd)
                        CheckedBoxDislike.IsChecked = false;
                }
                else
                {
                    SelectedComment.NumberOfDislikes += isAdd ? 1 : -1;

                    if (isAdd)
                        CheckedBoxLike.IsChecked = false;
                }

                return Task.CompletedTask;
            });

            await DbMenu
                .UpdateAsync(SelectedComment!, DbContext!);

            await UpdateLikeCountsAsync();
        }

        private async Task DeleteCommentAsync()
        {
            if (!CanDeleteComment() || !ConfirmDeleteMessageBox()) 
                return;

            await DbMenu
                .DeleteAsync(SelectedComment!, DbContext!);

            OnCommentDeleted();
        }

        private bool CanDeleteComment()
        {
            return SelectedComment != null &&
                   (_window!.UserInfo?.Id == SelectedComment!.UserId ||
                    _window!.UserInfo?.IsAdmin == true);
        }

        // -- EVENTS --

        public event EventHandler? CommentDeleted;

        protected virtual void OnCommentDeleted() =>
            CommentDeleted?.Invoke(this, EventArgs.Empty);


        private async void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonCommentator):
                        await _window!.GoToPageAsync(
                            new PageUser(_window, SelectedComment!.UserId));
                        break;

                    case nameof(ButtonDelete):
                        await DeleteCommentAsync();
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }


        private async void CheckedChangedHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((GeometryCheckBox)sender).Name, e.RoutedEvent.Name)
                {
                    case (nameof(CheckedBoxLike), "Checked"):
                        await ChangeCheckedOfLikeAsync(true, true);
                        break;

                    case (nameof(CheckedBoxDislike), "Checked"):
                        await ChangeCheckedOfLikeAsync(false, true);
                        break;

                    case (nameof(CheckedBoxLike), "Unchecked"):
                        await ChangeCheckedOfLikeAsync(true, false);
                        break;

                    case (nameof(CheckedBoxDislike), "Unchecked"):
                        await ChangeCheckedOfLikeAsync(false, false);
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

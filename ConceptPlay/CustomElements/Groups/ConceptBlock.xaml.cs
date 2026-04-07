using DLL_ConnectionToDatabase;
using System.Windows;

namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for ConceptBlock.xaml
    /// </summary>
    public partial class ConceptBlock
    {
        public Concept? SelectedConcept { get; set; }

        private readonly MainWindow? _window;

        // -- CONSTRUCTOR -- 

        public ConceptBlock()
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
                SelectedConcept = e.NewValue as Concept;

                if (SelectedConcept is null)
                    return;

                await InitAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- METHODS --

        private async Task InitAsync()
        {
            if (SelectedConcept is null)
                return;

            await Task.WhenAll
            (
                SetConceptInfoAsync(),
                SetAvatarAsync(),
                SetChangeDateVisibilityAsync(),
                SetGenresItemsControlVisibilityAsync(),
                SetDescriptionPreviewAsync()
            );
        }

        private async Task SetConceptInfoAsync()
        {
            var stats = new
            {
                Author =  SelectedConcept?.User.AvatarImage is null
                    ? $"👤 {SelectedConcept!.User.Username}"
                    : $"{SelectedConcept.User.Username}",

                Title      = $"{SelectedConcept!.Title}",
                CreateDate = $"📅 {SelectedConcept.CreationDate}",

                Views      = $"👁️ {SelectedConcept.Reviews.Count}",
                Likes      = $"❤️ {SelectedConcept.Reviews.Count(r => r.IsLiked)}",
                Comments   = $"💬 {SelectedConcept.Comments.Count}"
            };

            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockTitle.Text      = stats.Title;
                TextBlockAuthor.Text     = stats.Author;
                TextBlockCreateDate.Text = stats.CreateDate;

                TextBlockViews.Text      = stats.Views;
                TextBlockLikes.Text      = stats.Likes;
                TextBlockComments.Text   = stats.Comments;
            });
        }


        private async Task SetAvatarAsync()
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

        private async Task SetChangeDateVisibilityAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockChangeDate.Text = $"⚙️ {SelectedConcept!.ChangeDate}";

                bool showChangeDate = 
                    SelectedConcept!.CreationDate != SelectedConcept.ChangeDate;

                TextBlockChangeDate.Visibility = showChangeDate
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                RectangleBetweenDates.Visibility = showChangeDate
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        private async Task SetGenresItemsControlVisibilityAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                bool showItemsControl =
                    SelectedConcept!.Genres.Count != 0;

                ItemsControlGenres.Visibility = showItemsControl
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            });
        }

        private async Task SetDescriptionPreviewAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockDescription.Visibility = 
                    string.IsNullOrWhiteSpace(SelectedConcept?.Description)
                        ? Visibility.Collapsed
                        : Visibility.Visible;

                TextBlockDescription.Text = 
                    SelectedConcept!.Description!.Length <= 105
                        ? $"{SelectedConcept.Description}"
                        : $"{SelectedConcept.Description[..100]}...";
            });
        }
    }
}

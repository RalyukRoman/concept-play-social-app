using System.Windows;
using System.Windows.Controls;
using ConceptPlay.CustomElements.Groups;
using ConceptPlay.Pages;
using DLL_ConnectionToDatabase;

namespace ConceptPlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public UserInfo? UserInfo { get; set; }
        public readonly SocketManager? SocketManager;

        public Page? CurrentPage => 
            MainFrame.Content as Page;

        // -- CONSTRUCTOR --

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                SocketManager = new(this);

                _ = SocketManager.StartSocketConnectionAsync();
                _ = GoToPageAsync(new PageHub(this));
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- METHODS --

        public async Task GoToPageAsync(Page? page)
        {
            if (page is null)
                return;

            MainFrame.Content = page;
            await SetPageLayoutAsync(page);

            SocketManager!.ImagesBuffer.Clear();

            if (page is PageHub)
                await TopPanel.InitAsync();
        }

        private async Task SetPageLayoutAsync(Page page)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                bool isAuthPage = page is PageLogin or PageRegistration;

                TopPanel.Visibility = isAuthPage
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                Grid.SetRow(MainFrame, isAuthPage ? 0 : 1);
                Grid.SetRowSpan(MainFrame, isAuthPage ? 2 : 1);
            });
        }
    }
}
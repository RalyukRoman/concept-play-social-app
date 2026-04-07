using DLL_ConnectionToDatabase;
using System.Windows;

namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for GenreBlock.xaml
    /// </summary>
    public partial class GenreBlock
    {
        public Genre? SelectedGenre { get; set; }

        // -- CONSTRUCTOR -- 

        public GenreBlock()
        {
            try
            {
                InitializeComponent();

                DataContextChanged += GenreBlock_DataContextChanged;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        private async void GenreBlock_DataContextChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                SelectedGenre = e.NewValue as Genre;
                await InitAsync();
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- METHODS --

        private async Task InitAsync()
        {
            if (SelectedGenre is null)
                return;

            await Dispatcher.InvokeAsync(() =>
            {
                TextBlockBasic.Text = SelectedGenre.Name;
                BorderBasic.ToolTip = SelectedGenre.Description;
            });
        }
    }
}

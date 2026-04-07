using System.Windows;
using System.Windows.Controls;

namespace ConceptPlay.CustomElements.Groups
{
    /// <summary>
    /// Interaction logic for PageController.xaml
    /// </summary>
    public partial class ListController
    {
        public ListController()
        {
            try
            {
                InitializeComponent();

                ButtonNext.Click += ButtonClickHandler;
                ButtonPrevious.Click += ButtonClickHandler;
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }

        // -- DEPENDENCY PROPERTY --

        public static readonly DependencyProperty MaxPageProperty =
            DependencyProperty.Register
            (
                nameof(MaxPage),
                typeof(uint),
                typeof(ListController),
                new PropertyMetadata((uint) 0, OnPagePropertyChanged)
            );

        public uint MaxPage
        {
            get => (uint) GetValue(MaxPageProperty);
            set => SetValue(MaxPageProperty, value);
        }

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register
            (
                nameof(CurrentPage),
                typeof(uint),
                typeof(ListController),
                new PropertyMetadata((uint) 0, OnPagePropertyChanged)
            );

        public uint CurrentPage
        {
            get => (uint) GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        private static void OnPagePropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListController controller)
                controller.UpdatePageDisplay();
        }

        // -- METHOD --

        private void UpdatePageDisplay()
        {
            TextBlockPageNumber.Text = $"{CurrentPage} - {MaxPage}";

            Visibility = MaxPage <= 1 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }

        private void NavigateToPage(uint newPage)
        {
            if (newPage < 1 || newPage > MaxPage) 
                return;

            CurrentPage = newPage;
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task SetMaxPagesAsync(int itemCount)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                CurrentPage = 1;
                MaxPage = (uint) Math.Max(1, (itemCount + 29) / 30);
            });
        }

        // -- EVENT --

        public event EventHandler? PageChanged;

        private void ButtonClickHandler(
            object sender, RoutedEventArgs e)
        {
            try
            {
                switch (((Button)sender).Name)
                {
                    case nameof(ButtonPrevious):
                        NavigateToPage(CurrentPage - 1);
                        break;

                    case nameof(ButtonNext):
                        NavigateToPage(CurrentPage + 1);
                        break;
                }
            }
            catch (Exception ex) { Utilities.HandleException(ex); }
        }
    }
}

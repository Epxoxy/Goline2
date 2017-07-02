using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static NavigationService FlyoutNavigateServices { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;
            FlyoutNavigateServices = FlyoutFrame.NavigationService;
            MainFrame.Navigate(new HomePage());
        }

        //Disable the ManipulationBoundaryFeedback event to prevent window shake.
        private void OnManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

        //Disable Navigate event of back or forward.
        private void FlyoutFrameNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                var element = e.Content as FrameworkElement;
                if (element != null && NavigationHelper.GetIsHome(element)) e.Cancel = true;
            }
        }

        //Disable navigate forward
        private void MainFrameNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Forward)
            {
                e.Cancel = true;
            }
        }
    }
    
}

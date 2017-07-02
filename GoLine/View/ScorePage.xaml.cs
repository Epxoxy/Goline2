using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using SqlServices;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for ScorePage.xaml
    /// </summary>
    public partial class ScorePage : Page
    {
        public ScorePage()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;
            LoadScores();
            RoutedEventHandler unloadHandler = null;
            unloadHandler = (obj, args) =>
            {
                this.Unloaded -= unloadHandler;
            };
            this.Unloaded += unloadHandler;
        }

        private async void LoadScores()
        {
            System.Collections.IEnumerable itemsource = null;
            Processing.IsEnabled = true;
            await System.Threading.Tasks.Task.Run(() =>
            {
                IList<GameScore> scores = null;
                scores = App.LocalDBService.GetAllScore();
                itemsource = scores.OrderByDescending(score => score.Score);
            });
            ItemListView.ItemsSource = itemsource;
            Processing.IsEnabled = false;
        }

        private void RefreshBtnClick(object sender, RoutedEventArgs e)
        {
            if (Processing.IsEnabled) return;
            LoadScores();
        }
    }
}

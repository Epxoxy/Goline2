using LogicUnit;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GoLine2
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        IList<SelectionItem<GameMode>> AvailbleMode { get; set; }
        public HomePage()
        {
            InitializeComponent();
            this.Loaded += OnHomePageLoaded;
        }

        private void OnHomePageLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnHomePageLoaded;
            this.Unloaded += OnUnloaded;
            Application.Current.Activated += OnApplicationActive;
            Application.Current.Deactivated += OnApplicationDeactivated;
            //Get all allow mode and set to modelist in order to binding value
            AvailbleMode = new List<SelectionItem<GameMode>>()
            {
                new SelectionItem<GameMode>(Properties.Resources.PVP, GameMode.PvP),
                new SelectionItem<GameMode>(Properties.Resources.PVE, GameMode.PvE),
                //new SelectionItem<GameMode>(Properties.Resources.AIVSAI, GameMode.AIvsAI),
                new SelectionItem<GameMode>(Properties.Resources.Online,  GameMode.PvPOnline),
            };
            ModeList.ItemsSource = AvailbleMode;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= OnUnloaded;
            Application.Current.Activated -= OnApplicationActive;
            Application.Current.Deactivated -= OnApplicationDeactivated;
        }
        
        private void OnApplicationActive(object sender, EventArgs e)
        {
            StartBtn.IsEnabled = true;
        }

        private void OnApplicationDeactivated(object sender, EventArgs e)
        {
            StartBtn.IsEnabled = false;
        }

        #region -----------Basic button click navigate handler-------------

        private void ExitBtnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void AccountBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new UserPage());
        }

        private void SettingBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new SettingPage());
        }

        private void HelpBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new HelpPage());
        }

        private void TopBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new ScorePage());
        }

        private void LangBtnClick(object sender, RoutedEventArgs e)
        {
            App.RequestChangeLang();
        }

        private void LoginStateBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new UserPage());
        }

        #endregion

        private async void OnModeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Check the selected item, when the selector initilize,
            //there will raise a selection changed will not selected
            if (e.AddedItems.Count < 1) return;
            var selectedItem = e.AddedItems[0];
            if (selectedItem == null) return;
            //Get selected mode
            var selectedMode = selectedItem as SelectionItem<GameMode>;
            ////Ask for login when game mode is the type will record score
            //Online mode is in testing, so refuse to enter online mode.
            if(selectedMode.Value == GameMode.PvPOnline)
            {
                /*ModeList.SelectedIndex = -1;
                var dialog = new Epx.Controls.MessageDialog()
                {
                    TopTitle = Properties.Resources.Message,
                    Title = Properties.Resources.Tips,
                    Content = Properties.Resources.TestingTips,
                    PrimaryButtonText = Properties.Resources.OK,
                    SecondaryButtonText = Properties.Resources.Cancel
                };
                await dialog.ShowAsync();
                return;*/
            }
            if (selectedMode.Value == GameMode.PvPOnline && App.LoginedAccount == null)
            {
                //Show dialog
                var dialog = new Epx.Controls.MessageDialog()
                {
                    TopTitle = Properties.Resources.Message,
                    Title = Properties.Resources.RequireLogin,
                    Content = Properties.Resources.LoginRequestTips,
                    PrimaryButtonText = Properties.Resources.Login,
                    SecondaryButtonText = Properties.Resources.Guest
                };
                var result = await dialog.ShowAsync();
                //If user select to login
                if (result == Epx.Controls.MessageDialogResult.Primary)
                {
                    //Register eventhandler to navigate to game page for login successful 
                    //Navigate to user page
                    var userpage = new UserPage();
                    EventHandler handler = null;
                    handler = (obj, args) =>
                    {
                        userpage.LoginSuccessed -= handler;
                        NavigateGamePage(selectedMode.Value);
                    };
                    userpage.LoginSuccessed += handler;
                    MainWindow.FlyoutNavigateServices.Navigate(userpage);
                    ModeList.SelectedIndex = -1;
                }
                else if (result == Epx.Controls.MessageDialogResult.Secondary)
                {
                    //If user select to guest
                    //Go to the game page
                    //Game page will create guest account if there is no logined account
                    NavigateGamePage(selectedMode.Value);
                }
                else
                {
                    ModeList.SelectedIndex = -1;
                }
            }
            else
            {
                //When game mode is not the type will record score
                //Go to game page
                NavigateGamePage(selectedMode.Value);
            }
        }

        private void NavigateGamePage(GameMode gameMode)
        {
            ModeList.SelectedIndex = -1;
            if (gameMode == GameMode.PvPOnline)
            {
                //TODO Check network and check user login
                //MainWindow.FlyoutNavigateServices.Navigate(new UserPage());
                ModeSelection.IsOpen = false;
                this.NavigationService.Navigate(new GamePage(gameMode));
            }
            else
            {
                //TODO
                //If gameMode == GameMode.PVE and bad network
                //Tip if play in local mode(Can't update data)

                ModeSelection.IsOpen = false;
                this.NavigationService.Navigate(new GamePage(gameMode));
            }
        }

    }
}

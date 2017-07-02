using Epx.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogicUnit;
using LogicUnit.Data;
using LogicUnit.Interface;
using System.ComponentModel;
using System.Windows.Input;

namespace GoLine2
{
    /// <summary>
    /// Interaction logic for GamePage.xaml
    /// </summary>
    public partial class GamePage : Page
    {
        private SolidColorBrush backTransBrush = new SolidColorBrush(Colors.DimGray) { Opacity = 0.6 };
        private string hostName = "Host", joinName = "Join";
        private string hostToken, joinToken;
        private string ip;
        private int port;
        private int inport;
        private bool isHost = true;
        private bool isEnd = true;
        private bool alwaysAllowPay = false;
        private IBoard myBoard { get; set; }
        private IGameCoreResolver core;
        public GameMode GameMode { get; private set; }
        public int FirstIndex { get; set; }

        public GamePage() : this(GameMode.PvE) { }
        public GamePage(GameMode gameMode)
        {
            InitializeComponent();
            GameMode = gameMode;
            //TODO  Wait for selected to init game service when :
            //0.Game mode is Online need to select player by player
            //1.Game mode is PVE or AIVSAI need to select AI level
            //Else init game service when load completed
            Loaded += onPageLoaded;
        }
        

        private void onPageLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= onPageLoaded;
            onLoaded();
            Unloaded += onPageUnloaded;
        }

        private void onPageUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= onPageUnloaded;
            if (core != null) core.Dispose();
            core = null;
        }

        private void onLoaded()
        {
            var cur = (Brush)(new BrushConverter()).ConvertFromString(App.Settings.ChessBrush);
            chessboard.UpdateChessFill(chessboard.HostBrush, cur);
            releaseResolver();
            if (GameMode == GameMode.PvE)
            {
                showLevelOpetion();
            }
            else if (GameMode == GameMode.PvPOnline)
            {
                showConnectionBox();
            }
            else initialize();
        }


        #region --------   Set first   --------

        private void showFirstOption()
        {
            //if (TryInitPlayers() == false) return;
            var players = new List<SelectionItem<string>>()
            {
                new SelectionItem<string>(hostName, hostToken),
                new SelectionItem<string>(joinName, joinToken)
            };
            FirstStartComboBox.ItemsSource = players;
            FirstStartComboBox.SelectedIndex = 0;
            FadeExtension.FadeInBoxOf(FirstSelectBox, FirstSelectBoxContent);
        }       

        private void setFirstBtnOKClick(object sender, RoutedEventArgs e)
        {
            if (FirstStartComboBox.SelectedIndex < 0) return;
            else FirstIndex = FirstStartComboBox.SelectedIndex;
            //if (TryInitPlayers() == false) return;
            FadeExtension.FadeOutBoxOf(FirstSelectBox, FirstSelectBoxContent);
            if(GameMode != GameMode.PvPOnline)
                ensureStart();
        }

        #endregion
        
        #region -------- Connection box --------

        private void showConnectionBox()
        {
            FadeExtension.FadeInBoxOf(ConnectionBox, ConnectionBoxContent);
        }

        private void ipOkBtnClick(object sender, RoutedEventArgs e)
        {
            ip = IPBox.Text;
            System.Net.IPAddress address = null;
            if (System.Net.IPAddress.TryParse(ip, out address))
            {
                if (int.TryParse(PortBox.Text, out port) &&
                    int.TryParse(InPortBox.Text, out inport))
                {
                    isHost = HostCheck.IsChecked == true;
                    //Do something
                    initialize();
                    FadeExtension.FadeOutBoxOf(ConnectionBox, ConnectionBoxContent);
                }
            }
        }

        #endregion
        
        #region -------- Tips pay --------

        private void showPayTipsBox()
        {
            FadeExtension.FadeInBoxOf(PayTipsBox, PayTipsBoxContent);
        }

        private void payTipsOkBtnClick(object sender, RoutedEventArgs e)
        {
            FadeExtension.FadeOutBoxOf(PayTipsBox, PayTipsBoxContent);
            string auth = TipsAuthCodeBox.Text;
            alwaysAllowPay = AlwaysPayCheck.IsChecked == true;
            tips(auth);
        }

        private void payTipsCancelBtnClick(object sender, RoutedEventArgs e)
        {
            FadeExtension.FadeOutBoxOf(PayTipsBox, PayTipsBoxContent);
        }

        #endregion

        #region --------Level option --------

        private string getAIName(AILevel type)
        {
            switch (type)
            {
                case AILevel.Advanced:return Properties.Resources.AdvancedAI;
                case AILevel.Intermediate: return Properties.Resources.IntermediateAI;
                case AILevel.Elementary: return Properties.Resources.ElementaryAI;
            }
            return string.Empty;
        }

        private void showLevelOpetion()
        {
            var availbleType = new List<SelectionItem<AILevel>>()
            {
                new SelectionItem<AILevel>(getAIName(AILevel.Advanced), AILevel.Advanced),
                new SelectionItem<AILevel>(getAIName(AILevel.Intermediate), AILevel.Intermediate),
                new SelectionItem<AILevel>(getAIName(AILevel.Elementary), AILevel.Elementary)
            };
            if (GameMode == GameMode.PvE)
            {
                SecLevelContent.Visibility = Visibility.Collapsed;
                FirstLevelComboBox.ItemsSource = availbleType;
                FirstLevelComboBox.SelectedIndex = 0;
            }
            else
            {
                FirstLevelComboBox.ItemsSource = availbleType;
                SecLevelComboBox.ItemsSource = availbleType;
                FirstLevelComboBox.SelectedIndex = 0;
                SecLevelComboBox.SelectedIndex = 0;
            }
            FadeExtension.FadeInBoxOf(LevelSelectBox, LevelBoxContent);
        }
        
        private AILevel getSelectedLevel(int index)
        {
            if (index == 0 && FirstLevelComboBox.SelectedIndex >= 0)
                return (AILevel)FirstLevelComboBox.SelectedValue;
            if (index != 0 && SecLevelComboBox.SelectedIndex >= 0)
                return (AILevel)FirstLevelComboBox.SelectedValue;
            return AILevel.None;
        }

        private void levelOkBtnClick(object sender, RoutedEventArgs e)
        {
            FadeExtension.FadeOutBoxOf(LevelSelectBox, LevelBoxContent);
            //Active first player selection when selected level
            initialize();
        }

        private void cancelBtnClick(object sender, RoutedEventArgs e)
        {
            if(this.NavigationService.CanGoBack)this.NavigationService.GoBack();
        }

        #endregion


        private void initialize(bool autoStart = false)
        {
            if (myBoard == null)
            {
                PropertyChangedEventHandler onsettingsChanged = null;
                onsettingsChanged = (obj, args) =>
                {
                    var name = nameof(App.Settings.ChessBrush);
                    var converter = new BrushConverter();
                    if (args.PropertyName == name)
                    {
                        var newBrush = (Brush)converter.ConvertFromString(App.Settings.ChessBrush);
                        var oldBrush = chessboard.HostBrush;
                        chessboard.UpdateChessFill(oldBrush, newBrush);
                        System.Diagnostics.Debug.WriteLine($"{oldBrush.ToString()} {newBrush.ToString()}");
                    }
                };
                //Chessboard
                myBoard = chessboard;
                //Unsubsribe handler
                RoutedEventHandler unsubscribeAll = null;
                unsubscribeAll = (obj, args) =>
                {
                    this.Unloaded -= unsubscribeAll;
                    App.Settings.PropertyChanged -= onsettingsChanged;
                    chessboard.PreviewMouseDown -= onBoardPrevMouseDown;
                    releaseResolver();
                    App.BGMService.Stop();
                };
                App.Settings.PropertyChanged += onsettingsChanged;
                this.Unloaded += unsubscribeAll;
                chessboard.PreviewMouseDown += onBoardPrevMouseDown;
                startBtn.IsEnabled = true;
                helpBtn.IsEnabled = false;
                wdBtn.IsEnabled = false;
            }
            //** Init EnvProxy
            initResolver(autoStart);
            //Init and join player
            player01TB.Foreground = chessboard.HostBrush;
            player02TB.Foreground = chessboard.JoinBrush;
            player01Rect.Fill = Brushes.DimGray;
            player02Rect.Fill = Brushes.DimGray;
            App.BGMService.Play();
            //App.BGMService.AddDirectory();
        }

        private void initResolver(bool autoStart = false)
        {
            if (core != null)
            {
                core.StateChanged -= onCoreStateChanged;
                core.Dispose();
                core = null;
            }
            joinName = "Join";
            hostToken = string.Empty;
            joinToken = string.Empty;
            myBoard = chessboard;
            switch (GameMode)
            {
                case GameMode.PvE:
                    var type = getSelectedLevel(0);
                    core = GameResolver.BuildEVP(myBoard, type);
                    joinName = getAIName(type);
                    break;
                case GameMode.PvP:
                    core = GameResolver.BuildPVP(myBoard);
                    break;
                case GameMode.PvPOnline:
                    if (isHost)
                        core = GameResolver.BuildOnlineHost(myBoard, ip, port, inport);
                    else
                        core = GameResolver.BuildOnline(myBoard, ip, port, inport);
                    break;
            }
            if (core != null)
            {
                hostToken = core.HostToken;
                joinToken = core.JoinToken;
                updateNames();
                core.StateChanged += onCoreStateChanged;
                if(GameMode == GameMode.PvPOnline)
                {
                    readyBtn.Visibility = Visibility.Visible;
                }else
                {
                    readyBtn.Visibility = Visibility.Collapsed;
                    core.Ready();
                }
                if (autoStart)
                {
                    core.SetFirst(FirstIndex == 0 ? hostToken : joinToken);
                    core.Start();
                }else
                {
                    showFirstOption();
                }
            }
        }

        private void updateNames()
        {
            player01TB.Text = hostName;
            player02TB.Text = joinName;
        }

        private void ensureStart()
        {
            if (core != null)
            {
                core.SetFirst(FirstIndex == 0 ? hostToken : joinToken);
                core.Start();
            }
        }

        private void onCoreStateChanged(JudgeCode code)
        {
            switch (code)
            {
                case JudgeCode.Started:
                    isEnd = false;
                    onStarted();
                    break;
                case JudgeCode.Ended:
                    if (string.IsNullOrEmpty(core.WinnerToken))
                        onEndedUIThread(Properties.Resources.TieEnded);
                    break;
                case JudgeCode.Active:
                    onActive(core.IsHostActive);
                    break;
                case JudgeCode.NewWinner:
                    string winner = core.WinnerToken == core.HostToken ? hostName : joinName;
                    onEndedUIThread(winner + " " + Properties.Resources.WinTheGame);
                    break;
            }
        }
        
        private void onStarted()
        {
            Debug.Log("Started");
            Dispatcher.Invoke(() =>
            {
                chessboard.CalculateLattice = true;
                chessboard.RefuseNewChess = false;
                startBtn.IsEnabled = false;
                helpBtn.IsEnabled = true;
                wdBtn.IsEnabled = true;
            });
        }

        private void onActive(bool isHost)
        {
            Dispatcher.Invoke(() =>
            {
                if (isHost)
                {
                    player01Rect.Fill = Brushes.LightSeaGreen;
                    player02Rect.Fill = Brushes.LightGray;
                }
                else
                {
                    player01Rect.Fill = Brushes.LightGray;
                    player02Rect.Fill = Brushes.LightSeaGreen;
                }
            });
        }

        private void onEndedUIThread(string text)
        {
            Dispatcher.Invoke(() =>
            {
                onEnded(text);
            });
        }

        private async void onEnded(string text)
        {
            //Update score
            isEnd = true;
            Debug.Log("Ended");
            chessboard.RefuseNewChess = true;
            chessboard.CalculateLattice = false;
            //Show ended message
            var dialog = new MessageDialog()
            {
                TopTitle = Properties.Resources.Message,
                Title = Properties.Resources.Message,
                PrimaryButtonText = Properties.Resources.OK,
                SecondaryButtonText = Properties.Resources.Cancel,
                Background = backTransBrush
            };
            //var time01 = Provider.GetInfoOf(primaryPlayer).TimeSpan.ToString("hh':'mm':'ss");
            //var time02 = Provider.GetInfoOf(secondaryPlayer).TimeSpan.ToString("hh':'mm':'ss");
            //builder.Append($"\n\n{primaryPlayer.Name} ");
            //builder.Append($"{Properties.Resources.Score} : {primaryPlayer.Score?.Score:0}, {Properties.Resources.Time} : {time01}");
            //builder.Append($"\n{secondaryPlayer.Name} ");
            //builder.Append($"{Properties.Resources.Score} : {secondaryPlayer.Score?.Score:0}, {Properties.Resources.Time} : {time02}");
            dialog.Content = text;
            await Task.Delay(200);
            await dialog.ShowAsync();
            releaseResolver();
            startBtn.IsEnabled = true;
            helpBtn.IsEnabled = false;
            wdBtn.IsEnabled = false;
        }

        private void releaseResolver()
        {
            /*ensureReleaseTimer();
            chessboard.LatticeClick -= OnLatticeClick;
            if (Provider == null) return;
            Provider.GameDataUpdated -= OnGameDataUpdated;
            Provider.GameStarting -= OnGameStarting;
            Provider.GameEnded -= OnGameEnded;
            Provider.ActivePlayerChanged -= OnActivePlayerChanged;
            Provider.Detach();
            Provider = null;
            Debug.Log("ReleaseGameService");*/
        }
        
        private async void onBoardPrevMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (core != null && core.IsStarted) return;
            var dialog = new MessageDialog()
            {
                TopTitle = Properties.Resources.Message,
                Title = Properties.Resources.Tips,
                Content = $"{Properties.Resources.StartNewGame}?",
                PrimaryButtonText = Properties.Resources.OK,
                SecondaryButtonText = Properties.Resources.Cancel,
                Background = backTransBrush
            };
            var result = await dialog.ShowAsync();
            if (result == MessageDialogResult.Secondary || result == MessageDialogResult.Fail) return;
            startBtnClick(sender, e);
        }
        
        private async void tips(string authcode)
        {
            if(alwaysAllowPay || authcode == "authcode001")
            {
                if (core != null && core.IsStarted)
                {
                    IntPoint p = await core.Tips();
                    Dispatcher.Invoke(() =>
                    {
                        chessboard.ShowStepTips(p.X, p.Y);
                    });
                    System.Diagnostics.Debug.WriteLine(p);
                }
            }
        }
        

        private void withdrawClick(object sender, RoutedEventArgs e)
        {
            if(core != null)
                core.Undo();
            /*if (humanPlayers.Count == 0) return;
            if (humanPlayers.Count == 1) { }
            else
            {
                int index = humanPlayers.IndexOf(Provider.FrontPlayer);
                if (index < 0) Debug.Log($"Can't undo now");
                else { }//undo
            }
            */
        }

        private void startBtnClick(object sender, RoutedEventArgs e)
        {
            if(core != null)
            {
                if (!core.IsStarted && core.Useful)
                {
                    core.Start();
                }
                else core.Reset();
            }
            else onLoaded();
        }
        
        private void resetdBtnClick(object sender, RoutedEventArgs e)
        {
            onLoaded();
        }
        

        private void readyBtnClick(object sender, RoutedEventArgs e)
        {
            if (core != null && !core.IsStarted && core.Useful)
            {
                core.Ready();
            }
        }

        private void helpBtnClick(object sender, RoutedEventArgs e)
        {
            if (core == null || !core.IsStarted) return;
            if (alwaysAllowPay)
            {
                tips(string.Empty);
            }else showPayTipsBox();
        }

        private void menuBtnClick(object sender, RoutedEventArgs e)
        {
            MainWindow.FlyoutNavigateServices.Navigate(new MenuControl());
        }                         

        private void userBtnClick(object sender, RoutedEventArgs e)
        {
            if(App.LoginedAccount != null)
                MainWindow.FlyoutNavigateServices.Navigate(new UserPage() { LogoutEnabled = false });
        }
        
    }
}

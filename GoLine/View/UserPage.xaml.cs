using SqlServices;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for UserPage.xaml
    /// </summary>
    public partial class UserPage : Page
    {
        public event EventHandler LoginSuccessed;
        public event EventHandler AccountOffline;
        public bool LogoutEnabled { get; set; } = true;
        public UserPage()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;
            UpdateAccount(App.LoginedAccount);
            if (!LogoutEnabled) LogoutBtn.IsEnabled = false;
        }

        private void UpdateAccount(OnlineAccount account, bool logMe = false)
        {
            AccountPart.DataContext = null;
            Processing.IsEnabled = false;
            if (account == null)
            {
                AccountPart.Visibility = Visibility.Collapsed;
                LoginSignupPart.Visibility = Visibility.Visible;
                UserNameComboBox.ItemsSource = App.Settings.StoredUser;
            }
            else
            {
                AccountPart.Visibility = Visibility.Visible;
                LoginSignupPart.Visibility = Visibility.Collapsed;
                AccountPart.DataContext = App.LoginedAccount;
                if (logMe) App.StoreUser(account.UserName);
                LoginSuccessed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ShowErrorTips(string value)
        {
            Processing.IsEnabled = false;
            ErrorTB.Text = value;
            VisualStateManager.GoToElementState(ErrorCanvas, "FadeIn", true);
        }

        private void ShowError(ConnectResult connectResult)
        {
            string errorinfo = string.Empty;
            switch (connectResult)
            {
                case ConnectResult.ConnectFail:
                    errorinfo = Properties.Resources.ConnectFail;
                    break;
                case ConnectResult.EmptyUserNameOrPsw:
                    errorinfo = Properties.Resources.EmptyUserNameOrPsw;
                    break;
                case ConnectResult.GenerateRecordFail:
                    errorinfo = Properties.Resources.GenerateRecordFail;
                    break;
                case ConnectResult.OtherError:
                    errorinfo = Properties.Resources.OtherError;
                    break;
                case ConnectResult.UserExist:
                    errorinfo = Properties.Resources.UserExist;
                    break;
                case ConnectResult.UserNotExist:
                    errorinfo = Properties.Resources.UserNotExist;
                    break;
                case ConnectResult.WrongPassword:
                    errorinfo = Properties.Resources.WrongPassword;
                    break;
            }
            if (string.IsNullOrEmpty(errorinfo)) return;
            ShowErrorTips(errorinfo);
        }

        private void HideErrorTips()
        {
            ErrorTB.Text = string.Empty;
            VisualStateManager.GoToElementState(ErrorCanvas, "FadeOut", true);
        }

        private async void SignupBtnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserNameTB.Text))
            {
                ShowErrorTips(Properties.Resources.UserNameEmptyTips);
                return;
            }
            if (string.IsNullOrEmpty(SignupPswBox.Password))
            {
                ShowErrorTips(Properties.Resources.PSWEmptyTips);
                return;
            }
            if (string.IsNullOrEmpty(SignupConfirmPB.Password))
            {
                ShowErrorTips(Properties.Resources.ConfirmPassword);
                return;
            }
            if (SignupPswBox.Password != SignupConfirmPB.Password)
            {
                ShowErrorTips(Properties.Resources.PasswordNotSame);
            }
            else
            {
                HideErrorTips();
                string nickName = NickNameTB.Text;
                string username = UserNameTB.Text;
                string password = SignupPswBox.Password;
                string errorMsg = string.Empty;
                var result = ConnectResult.ConnectFail;
                Processing.IsEnabled = true;
                await Task.Run(() => 
                {
                    result = App.NetDBService.Register(nickName, username, password, out errorMsg);
                });
                if (result == ConnectResult.Success)
                {
                    UpdateAccount(App.LoginedAccount, LogMeCheck.IsChecked == true);
                    LoginPswBox.Password = string.Empty;
                }
                else ShowError(result);
            }
        }

        private async void LoginBtnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserNameComboBox.Text))
            {
                ShowErrorTips(Properties.Resources.UserNameEmptyTips);
                return;
            }
            if (string.IsNullOrEmpty(LoginPswBox.Password))
            {
                ShowErrorTips(Properties.Resources.PSWEmptyTips);
            }
            else
            {
                HideErrorTips();
                string errorMsg = string.Empty;
                string username = UserNameComboBox.Text;
                string password = LoginPswBox.Password;
                ConnectResult result = ConnectResult.ConnectFail;
                Processing.IsEnabled = true;
                await Task.Run(() =>
                {
                    result = App.NetDBService.Login(username, password, out errorMsg);
                    Debug.Log(errorMsg);
                });
                if(result == ConnectResult.Success)
                {
                    UpdateAccount(App.LoginedAccount, LogMeCheck.IsChecked == true);
                    LoginPswBox.Password = string.Empty;
                }
                else ShowError(result);
            }
        }

        private void LogoutBtnClick(object sender, RoutedEventArgs e)
        {
            App.NetDBService.Logout();
            UpdateAccount(App.LoginedAccount);
            AccountOffline?.Invoke(this, EventArgs.Empty);
        }

        private async void RefreshBtnClick(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => 
            {
                App.NetDBService.LoginedAccount.Score = App.NetDBService.TrySync();
            });
            UpdateAccount(App.LoginedAccount);
        }
    }
}

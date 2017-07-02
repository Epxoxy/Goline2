using System.Diagnostics;
using System.Windows;

namespace GoLine2
{
    using SqlServices;
    //using GameServices;
    using System;
    using System.IO;
    using System.Text;
    using System.Windows.Media;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal static Properties.Settings Settings => GoLine2.Properties.Settings.Default;
        public static SqliteService NetDBService => SqliteService.GetService();
        public static SqliteService LocalDBService => SqliteService.GetService();
        public static OnlineAccount LoginedAccount => NetDBService.LoginedAccount;
        public static MediaService BGMService => MediaService.GetMediaService();
        public static MediaPlayer soundsEffectPlayer;
        public static MediaPlayer SoundsEffectPlayer
        {
            get
            {
                if (soundsEffectPlayer == null && Settings.EnableSounds)
                {
                    soundsEffectPlayer = new MediaPlayer();
                }
                return soundsEffectPlayer;
            }
        }
        public static void ClickEffectPlay()
        {
            if (SoundsEffectPlayer == null || !Settings.EnableSounds) return;
            SoundsEffectPlayer.Volume = BGMService.Volume / 100;
            SoundsEffectPlayer.Open(new Uri("pack://siteoforigin:,,,/Resources/Sounds/click2.mp3"));
            SoundsEffectPlayer.Play();
        }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            System.Windows.Forms.Application.EnableVisualStyles();
            GoLine2.Properties.Resources.Culture = new System.Globalization.CultureInfo(Settings.NewestLang);
            Settings.UsingLang = Settings.NewestLang;
            if (Settings.UsingLang == "zh-CN")
            {
                var fontFamily = new FontFamily(Settings.FontSetting);
                System.Windows.Documents.TextElement.FontFamilyProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(fontFamily));
                System.Windows.Controls.TextBlock.FontFamilyProperty.OverrideMetadata(typeof(System.Windows.Controls.TextBlock), new FrameworkPropertyMetadata(fontFamily));
            }
            App.BGMService.AddSong(new Uri("pack://siteoforigin:,,,/Resources/Songs/tw034.mp3"));
            BGMService.EnablePlay = Settings.EnableMusic;
            Settings.PropertyChanged += OnSettingPropertyChanged;
        }

        private void OnSettingPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Settings.EnableMusic))
            {
                BGMService.EnablePlay = Settings.EnableMusic;
            }else if(e.PropertyName == nameof(Settings.Volume))
            {
                BGMService.Volume = Settings.Volume;
            }
        }

        private void DisposeOnUnhandledException()
        {
        }

        #region Unexpected exception handler

        private void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (Exception)e.ExceptionObject;
#if DEBUG
            System.Diagnostics.Debug.WriteLine(sender.ToString() + "\n" + e.ExceptionObject);
#endif
            LogExceptionInfo(exception, "AppDomain.CurrentDomain.UnhandledException");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(e.Exception.TargetSite);
#endif
            LogExceptionInfo(e.Exception, "AppDomain.DispatcherUnhandledException");
        }

        private void LogExceptionInfo(Exception exception, string typeName = "Undefined Exception")
        {
            DisposeOnUnhandledException();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\n---------------------\n{0}\n", typeName);
            sb.AppendFormat("\n\n0.TargetSite\n{0}\n\n1.StackTrace\n{1}\n\n2.Source\n{2}\n\n3.Message\n{3}\n\n4.HResult\n{4}\n",
                exception.TargetSite,
                exception.StackTrace,
                exception.Source,
                exception.Message,
                exception.HResult);
            if (exception.InnerException != null)
            {
                sb.Append("\n---------------------\nInnerException\n");
                sb.AppendFormat("\n\n5.0.TargetSite\n{0}\n\n5.1.StackTrace\n{1}\n\n5.2.Source\n{2}\n\n5.3.Message\n{3}\n\n5.4.HResult\n{4}\n",
                    exception.InnerException.TargetSite,
                    exception.InnerException.StackTrace,
                    exception.InnerException.Source,
                    exception.InnerException.Message,
                    exception.InnerException.HResult);
            }


            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string log = Path.GetDirectoryName(path) + "\\log.txt";
            using (StreamWriter sw = new StreamWriter(log, true, Encoding.UTF8))
            {
                sw.Write(string.Format("---------------------\n\n{0}\n{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff"), sb.ToString()));
            }
        }
        
        #endregion
        
        public static void Restart()
        {
            isRestart = true;
            Current.Shutdown();
        }

        private static bool isRestart;
        protected override void OnExit(ExitEventArgs e)
        {
            Settings.Save();
            base.OnExit(e);
            if (isRestart && !string.IsNullOrEmpty(App.ResourceAssembly.Location) && !Settings.DebugMode)
            {
                Process.Start(App.ResourceAssembly.Location);
            }
        }

        public static async void RequestChangeLang()
        {
            var dialog = new Epx.Controls.MessageDialog()
            {
                TopTitle = GoLine2.Properties.Resources.Message,
                Title = GoLine2.Properties.Resources.ChangeLang,
                Content = $"{GoLine2.Properties.Resources.ChangeLangTips}\n({Settings.NextLang})",
                PrimaryButtonText = GoLine2.Properties.Resources.OK,
                SecondaryButtonText = GoLine2.Properties.Resources.Cancel
            };
            var result = await dialog.ShowAsync();
            if (result == Epx.Controls.MessageDialogResult.Fail || result == Epx.Controls.MessageDialogResult.Secondary)
                return;
            //Do if current set to debug mode
            if (Settings.DebugMode)
            {
                var debugdialog = new Epx.Controls.MessageDialog()
                {
                    TopTitle = GoLine2.Properties.Resources.Message,
                    Title = GoLine2.Properties.Resources.DebugMode,
                    Content = GoLine2.Properties.Resources.DebugExplain,
                    PrimaryButtonText = GoLine2.Properties.Resources.OK,
                    SecondaryButtonText = GoLine2.Properties.Resources.Cancel
                };
                await debugdialog.ShowAsync();
            }
            string tips = string.Empty;
            if (Settings.NewestLang == "zh-CN")
            {
                Settings.NewestLang = "en-US";
                Settings.NextLang = "中文";
            }
            else
            {
                Settings.NewestLang = "zh-CN";
                Settings.NextLang = "English";
            }
            Settings.Save();
            if (Settings.NewestLang != Settings.UsingLang)
            {
                App.Restart();
            }
        }

        public static void StoreUser(string userName)
        {
            if (Settings.StoredUser == null)
            {
                Settings.StoredUser = new System.Collections.Specialized.StringCollection();
                Settings.StoredUser.Add(userName);
                Settings.Save();
            }
            else if (!Settings.StoredUser.Contains(userName))
            {
                Settings.StoredUser.Add(userName);
                Settings.Save();
            }
        }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

    }
}

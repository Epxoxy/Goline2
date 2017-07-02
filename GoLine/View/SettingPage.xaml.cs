using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GoLine
{
    /// <summary>
    /// Interaction logic for SettingControl.xaml
    /// </summary>
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnLoaded;
            BrushComboBox.SelectedItem = App.Settings.ChessBrush;
            LineBrushComboBox.SelectedItem = App.Settings.BoardLineBrush;
            AutoNoticeBtn.IsChecked = App.Settings.AutoNoticeNewest;
             //Unsubsribe handler
             RoutedEventHandler unloadHandler = null;
            unloadHandler += (obj, args) =>
            {
                this.Unloaded -= unloadHandler;
            };
            this.Unloaded += unloadHandler;
        }

        private void SaveChangeBtnClick(object sender, RoutedEventArgs e)
        {
            if (BrushComboBox.SelectedIndex < 1) return;
            var selectedBrush = BrushComboBox.SelectedItem.ToString();
            var selectedLineBrush = LineBrushComboBox.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(selectedBrush))
            {
                if (App.Settings.ChessBrush != selectedBrush)
                {
                    App.Settings.ChessBrush = selectedBrush;
                }
            }
            if (!string.IsNullOrEmpty(selectedLineBrush))
            {
                if (App.Settings.BoardLineBrush != selectedLineBrush)
                {
                    App.Settings.BoardLineBrush = selectedLineBrush;
                }
            }
            if (AutoNoticeBtn.IsChecked != App.Settings.AutoNoticeNewest)
            {
                App.Settings.AutoNoticeNewest = AutoNoticeBtn.IsChecked == true;
            }
            App.Settings.Save();
            SaveBtn.IsEnabled = false;
        }

        private void ComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count < 1) return;
            CheckSaveBtnEnable();
        }

        private void AutoNoticeBtnBtnClick(object sender, RoutedEventArgs e)
        {
            CheckSaveBtnEnable();
        }

        private void CheckSaveBtnEnable()
        {
            if (BrushComboBox.SelectedIndex > -1 && BrushComboBox.SelectedItem.ToString() != App.Settings.ChessBrush)
                SaveBtn.IsEnabled = true;
            else if (AutoNoticeBtn.IsChecked != App.Settings.AutoNoticeNewest)
                SaveBtn.IsEnabled = true;
            else if (LineBrushComboBox.SelectedIndex > -1 && LineBrushComboBox.SelectedItem.ToString() != App.Settings.BoardLineBrush)
                SaveBtn.IsEnabled = true;
            else SaveBtn.IsEnabled = false;
        }
    }
}

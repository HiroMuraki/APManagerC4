using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;

namespace APManagerC4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RoutedCommand SaveData { get; } = new();

        public int MinimumPasswordLength => 8;
        public ViewModels.Manager Manager { get; }
        public ViewModels.AccountItemViewer Viewer { get; }

        public MainWindow()
        {
            _dataCenter = new TestDataCenter();
            Manager = new ViewModels.Manager(_dataCenter, WeakReferenceMessenger.Default);
            Viewer = new ViewModels.AccountItemViewer(_dataCenter, WeakReferenceMessenger.Default);

            var cb = new CommandBinding(SaveData);
            cb.Executed += (sender, e) => VerfiyPassword();
            cb.CanExecute += (sender, e) => e.CanExecute = true;
            CommandBindings.Add(cb);

            InitializeComponent();

            Loaded += (sender, e) =>
            {
                ShowVerficationPanel();
            };
        }

        private void RequestToViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Viewer.HasUnsavedChanges)
            {
                var r = MessageBox.Show($"对[{Viewer.Title}]的修改尚未提交，是否继续？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (r != MessageBoxResult.OK)
                {
                    var current = Manager.Groups.SelectMany(g => g.Items).FirstOrDefault(t => t.Guid == Viewer.Guid);
                    if (current is not null)
                    {
                        current.IsSelected = true;
                    }
                    return;
                }
            }
            if (Viewer.HasUnsavedChanges)
            {
                Viewer.SaveChanges();
            }
            ((ViewModels.AccountItem)e.Parameter).RequestToView(Manager.HasFilter);
        }
        private void RequestToViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void ApplyModification_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Viewer.SaveChanges();
        }
        private void ApplyModification_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Viewer.IsShownItem && !Viewer.ReadOnlyMode && Viewer.HasUnsavedChanges;
        }
        private void DeleteItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Manager.DeleteItem(Viewer.Guid);
            Viewer.Unload();
        }
        private void DeleteItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Viewer.IsShownItem && !Viewer.ReadOnlyMode;
        }
        private void NewItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Manager.NewItem();
        }
        private void NewItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !Manager.HasFilter;
        }
        private void SaveChangesCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowVerficationPanel();
        }
        private void SaveChangesCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.S)
            {
                ShowVerficationPanel();
            }
            else if (!_initialized && e.Key == Key.Enter)
            {
                VerfiyPassword();
            }
        }
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchWaiter.Reset();
            bool ok = await _searchWaiter.Wait();
            if (!ok)
            {
                return;
            }

            SearchAsync(((TextBox)sender).Text.ToUpper());
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_dataCenter.HasUnsavedChanges)
            {
                var r = MessageBox.Show($"修改尚未保存，是否关闭？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (r != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                }
            }
        }

        private bool _initialized;
        private readonly IntervalWaiter _searchWaiter = new() { Interval = TimeSpan.FromMilliseconds(150) };
        private readonly TestDataCenter _dataCenter;
        private void ShowVerficationPanel()
        {
            verficationPanel.Visibility = Visibility.Visible;
            verficationPanel.IsEnabled = true;
            passwordBox.Clear();
            passwordBox.Focus();
            mainPanel.Visibility = Visibility.Collapsed;
            mainPanel.IsEnabled = false;
        }
        private void VerfiyPassword()
        {
            string pasword = passwordBox.Password;
            passwordBox.Clear();

            if (!_initialized)
            {
                try
                {
                    _dataCenter.Initialize(pasword);
                    Manager.FetchData();
                    _initialized = true;
                }
                catch
                {
                    MessageBox.Show("解密出错，请检查密码是否正确", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    passwordBox.Focus();
                    return;
                }
            }
            else
            {
                if (pasword.Length < MinimumPasswordLength)
                {
                    MessageBox.Show($"密码长度至少为{MinimumPasswordLength}位");
                    passwordBox.Focus();
                    return;
                }
                _dataCenter.ReEncrypt(pasword);
                _dataCenter.SaveChanges();
                string sPassword = new AESTextEncrypter(GetPasswordKey()).Encrypt(pasword);
                MessageBox.Show($"已保存，请牢记密码凭证：\n{sPassword}", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            verficationPanel.Visibility = Visibility.Collapsed;
            verficationPanel.IsEnabled = false;
            mainPanel.Visibility = Visibility.Visible;
            mainPanel.IsEnabled = true;
            searchBox.Clear();
            searchBox.Focus();

            static byte[] GetPasswordKey()
            {
                long ticks = DateTime.Now.Ticks;
                var ticksKey = BitConverter.GetBytes(ticks);
                var result = new byte[32];
                Array.Copy(ticksKey, 0, result, 0, ticksKey.Length);
                Array.Copy(ticksKey, 0, result, 8, ticksKey.Length);
                Array.Copy(ticksKey, 0, result, 16, ticksKey.Length);
                Array.Copy(ticksKey, 0, result, 24, ticksKey.Length);
                return result;
            }
        }
        private void SearchAsync(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                Viewer.Unload();
                Manager.FetchData();
            }
            else
            {
                Manager.FetchDataIf(t => IsPatternMatched(pattern, t));
                if (Manager.Groups.Any())
                {
                    var t = Manager.Groups.First().Items.First();
                    t.IsSelected = true;
                    t.RequestToView(true);
                }
                else
                {
                    Viewer.Unload();
                }
                foreach (var group in Manager.Groups)
                {
                    group.IsExpanded = true;
                }
            }

            static bool IsPatternMatched(string pattern, Models.AccountItem item)
            {
                string[] keywords = Regex.Split(pattern, @"[\s]+");
                string[] text =
                {
                    item.Title.ToUpper(),
                    item.Website.ToUpper(),
                    item.Remarks.ToUpper()
                };

                if (keywords.Length == 1)
                {
                    return IsKeywordMatchedCore(keywords[0], text);
                }
                else
                {
                    foreach (var k in keywords)
                    {
                        if (!IsKeywordMatchedCore(k, text))
                        {
                            return false;
                        }
                    }
                    return true;
                }

                static bool IsKeywordMatchedCore(string keyword, string[] text)
                {
                    foreach (var item in text)
                    {
                        if (Regex.IsMatch(item, keyword))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
    }
}

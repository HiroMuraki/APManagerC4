using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace APManagerC4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RoutedCommand SaveDataCommand { get; } = new();
        public RoutedCommand CopyTextCommand { get; } = new();

        public int MinimumPasswordLength => 8;
        public ViewModels.Manager Manager { get; }
        public ViewModels.AccountItemViewer Viewer { get; }

        public MainWindow()
        {
            _dataCenter = new TestDataCenter();
            Manager = new ViewModels.Manager(WeakReferenceMessenger.Default, _dataCenter, _dataCenter);
            Viewer = new ViewModels.AccountItemViewer(WeakReferenceMessenger.Default, _dataCenter);

            {
                RegisterCommand(SaveDataCommand, (_, _) => VerfiyPassword(), (_, e) => e.CanExecute = true);
                RegisterCommand(CopyTextCommand, (_, e) =>
                    {
                        string? text = e.Parameter as string;
                        if (!string.IsNullOrEmpty(text))
                        {
                            try
                            {
                                Clipboard.SetText(text);
                            }
                            catch
                            {

                                MessageBox.Show("当前无法将文本复制到剪切板，请稍后再试", "", MessageBoxButton.OK);
                            }
                        }
                    },
                    (_, e) => e.CanExecute = Viewer.HasItemLoaded
                );
            }

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
            ((ViewModels.AccountItemLabel)e.Parameter).RequestToView(Manager.HasFilter);
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
            e.CanExecute = Viewer.HasItemLoaded && !Viewer.ReadOnlyMode && Viewer.HasUnsavedChanges;
        }
        private void DeleteItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Manager.DeleteItem(Viewer.Guid);
            Viewer.Unload();
        }
        private void DeleteItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Viewer.HasItemLoaded && !Viewer.ReadOnlyMode;
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
            if (Viewer.HasUnsavedChanges)
            {
                Viewer.SaveChanges();
            }
            ShowVerficationPanel();
        }
        private void SaveChangesCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchWaiter.Reset();
            bool ok = await _searchWaiter.Wait();
            if (!ok)
            {
                return;
            }

            Search(((TextBox)sender).Text.ToUpper());
        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var combox = (ComboBox)sender;
            string propName = combox.GetBindingExpression(ComboBox.TextProperty).ResolvedSourcePropertyName;
            var propInfo = typeof(Models.AccountItem).GetProperty(propName);

            if (propInfo is null)
            {
                throw new NullReferenceException($"No such property '{propName}'");
            }

            combox.ItemsSource = Manager.RetrieveOptions(
                p => (propInfo.GetValue(p) as string)?.Trim(),
                v => !string.IsNullOrWhiteSpace(v));
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Key == Key.S)
                {
                    ShowVerficationPanel();
                }
                else if (e.Key == Key.F)
                {
                    searchBox.Focus();
                }
            }
            else if (!_initialized && e.Key == Key.Enter)
            {
                VerfiyPassword();
            }
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
        private readonly IntervalWaiter _searchWaiter = new() { Interval = TimeSpan.FromMilliseconds(300) };
        private readonly TestDataCenter _dataCenter;
        private void RegisterCommand(ICommand command, ExecutedRoutedEventHandler executed, CanExecuteRoutedEventHandler canExecute)
        {
            var cb = new CommandBinding(command);
            cb.Executed += executed;
            cb.CanExecute += canExecute;
            CommandBindings.Add(cb);
        }
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
                MessageBox.Show($"已保存，请牢记密码凭证：\n{sPassword}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
        private void Search(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                Viewer.Unload();
                Manager.FetchData();
            }
            else
            {
                var regexs = from i in Regex.Split(pattern, @"[\s]+") select new Regex(i);
                Manager.FetchDataIf(t => IsPatternMatched(regexs.ToArray(), t));
                if (Manager.Groups.Any())
                {
                    var t = Manager.Groups.First().Items.First();
                    t.IsSelected = true;
                    t.RequestToView(true);
                    foreach (var group in Manager.Groups)
                    {
                        group.IsExpanded = true;
                    }
                }
                else
                {
                    Viewer.Unload();
                }
            }

            static bool IsPatternMatched(Regex[] regexs, Models.AccountItem item)
            {
                string[] text =
                {
                    item.Title.ToUpper(),
                    item.Website.ToUpper(),
                    item.Remarks.ToUpper()
                };

                foreach (var reg in regexs)
                {
                    if (text.Any(reg.IsMatch))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

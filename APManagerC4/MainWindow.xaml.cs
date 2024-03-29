﻿using CommunityToolkit.Mvvm.Messaging;
using HM.Common.Asynchronous;
using HM.Cryptography;
using HM.Cryptography.Cryptographers;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
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
        public static readonly DependencyProperty SaveDataCommandTextProperty =
            DependencyProperty.Register(nameof(SaveDataCommandText), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        public RoutedCommand SaveDataCommand { get; } = new();
        public RoutedCommand CopyTextCommand { get; } = new();

        public string SaveDataCommandText
        {
            get
            {
                return (string)GetValue(SaveDataCommandTextProperty);
            }
            set
            {
                SetValue(SaveDataCommandTextProperty, value);
            }
        }
        public int MinimumPasswordLength => 8;
        public ViewModels.Manager Manager { get; }
        public ViewModels.AccountItemViewer Viewer { get; }

        public MainWindow()
        {
            _dataCenter = new TestDataCenter();
            Manager = new ViewModels.Manager(WeakReferenceMessenger.Default, _dataCenter, _dataCenter);
            Viewer = new ViewModels.AccountItemViewer(WeakReferenceMessenger.Default, _dataCenter);
            Viewer.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Viewer.HasItemLoaded))
                {
                    if (Viewer.HasItemLoaded)
                    {
                        SaveDataCommandText = "提交更改";
                    }
                    else
                    {
                        SaveDataCommandText = "添加";
                    }
                }
            };

            RegisterCommand(SaveDataCommand, (_, _) => VerfiyPassword(), (_, e) => e.CanExecute = true);
            RegisterCommand(CopyTextCommand,
                (_, e) =>
                {
                    try
                    {
                        Clipboard.SetText(e.Parameter as string ?? string.Empty);
                    }
                    catch
                    {

                        MessageBox.Show("当前无法将文本复制到剪切板，请稍后再试", "", MessageBoxButton.OK);
                    }
                },
                (_, e) => e.CanExecute = !string.IsNullOrEmpty(e.Parameter as string)
            );

            InitializeComponent();

            Loaded += (sender, e) =>
            {
                Viewer.Unload();
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
                    var current = Manager.Groups.SelectMany(g => g.Items).FirstOrDefault(t => t.Uid == Viewer.Uid);
                    if (current is not null)
                    {
                        current.IsSelected = true;
                    }
                    return;
                }
            }
            ((ViewModels.AccountItemLabel)e.Parameter).RequestToView();
        }
        private void RequestToViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void ApplyModification_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Viewer.HasItemLoaded)
            {
                Viewer.SaveChanges();
            }
            else
            {
                var model = new Models.AccountItem()
                {
                    Uid = Viewer.Uid,
                    Title = Viewer.Title,
                    Website = Viewer.Website,
                    Category = Viewer.Category,
                    UserName = Viewer.UserName,
                    LoginName = Viewer.LoginName,
                    LoginPassword = Viewer.LoginPassword,
                    Email = Viewer.Email,
                    Phone = Viewer.Phone,
                    Remarks = Viewer.Remarks,
                    CreationTime = Viewer.CreationTime.Ticks,
                    UpdateTime = Viewer.UpdateTime.Ticks
                };
                Manager.AddItem(model);
            }
        }
        private void ApplyModification_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Viewer.HasUnsavedChanges;
        }
        private void DeleteItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Manager.DeleteItem(Viewer.Uid);
            Viewer.Unload();
        }
        private void DeleteItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Viewer.HasItemLoaded;
        }
        private void NewItemCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TryCreateNewAccountItem();
        }
        private void NewItemCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Manager.Filter is null;
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
        private void ExportToJsonCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                AddExtension = true,
                InitialDirectory = Environment.CurrentDirectory,
                DefaultExt = ".json",
                FileName = "data"
            };

            if (sfd.ShowDialog() == true)
            {
                var data = Manager.DataCenter.RetrieveAll(_ => true);
                string jsonString = JsonSerializer.Serialize(data.ToArray(), new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });
                using var writer = new StreamWriter(sfd.FileName);
                writer.Write(jsonString);
            }
        }
        private void ExportToJsonCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchWaiter.Reset();
            if (!await _searchWaiter.Wait(TimeSpan.FromMilliseconds(220)))
            {
                return;
            };

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
                else if (e.Key == Key.N)
                {
                    TryCreateNewAccountItem();
                }
            }
            else if (!_initialized && e.Key == Key.Enter)
            {
                VerfiyPassword();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_dataCenter.HasUnsavedChanges || Viewer.HasUnsavedChanges)
            {
                var r = MessageBox.Show($"有尚未保存的修改，是否关闭？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (r != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                }
            }
        }

        private bool _initialized;
        private readonly IntervalWaiter _searchWaiter = new();
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
                try
                {
                    _dataCenter.ReEncrypt(pasword);
                    _dataCenter.SaveChanges();
                    string sPassword = new AesTextCryptographer(GetPasswordKey()).Encrypt(pasword);
                    MessageBox.Show($"已保存，请牢记密码凭证：\n{sPassword}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("当前无法保存至文件，请检查文件是否被占用", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
                Manager.Filter = null;
                Viewer.Unload();
                Manager.FetchData();
            }
            else
            {
                var regexs = from i in Regex.Split(pattern, @"[\s]+") select new Regex(i);
                Manager.Filter = t => IsPatternMatched(regexs.ToArray(), t);
                Manager.FetchData();
                if (Manager.Groups.Any())
                {
                    var t = Manager.Groups.First().Items.First();
                    t.IsSelected = true;
                    t.RequestToView();
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
        private bool TryCreateNewAccountItem()
        {
            if (Manager.Filter is not null)
            {
                return false;
            }
            /* 新建一个Models.AccountItem并写入DataCenter */
            long time = DateTime.Now.Ticks;
            var model = new Models.AccountItem()
            {
                Uid = HM.Common.UidGenerator.Default.Next(),
                Title = ViewModels.Manager.DefaultItemTitle,
                Category = ViewModels.Manager.DefaultItemCategory,
                CreationTime = time,
                UpdateTime = time
            };
            Manager.AddItem(model);
            return true;
        }
    }
}

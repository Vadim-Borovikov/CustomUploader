using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CustomUploader.Logic;
using CustomUploader.Logic.Timepad.Data;
using System.Threading.Tasks;

namespace CustomUploader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Status.Content = "Загрузка...";

            string clientSecretPath = ConfigurationManager.AppSettings.Get("clientSecretPath");
            string parentId = ConfigurationManager.AppSettings.Get("parentId");
            _downloadPath = ConfigurationManager.AppSettings.Get("downloadPath");
            _lostPath = ConfigurationManager.AppSettings.Get("lostPath");
            _dataManager = new DataManager(clientSecretPath, parentId, OnDriveConnectedInvoker);

            int timepadHours = int.Parse(ConfigurationManager.AppSettings.Get("timepadHours"));
            _timepadLookup = TimeSpan.FromHours(timepadHours);

            int deviceDateWarningDays = int.Parse(ConfigurationManager.AppSettings.Get("deviceDateWarningDays"));
            _deviceDateWarningMonths = TimeSpan.FromDays(deviceDateWarningDays);

            _organizationId = int.Parse(ConfigurationManager.AppSettings.Get("organizationId"));

            _dataManager.StartWatch();

            LockButtons(false, true);
            Status.Content = "Готов";
        }

        private async Task<bool> MoveFromDevice(FileSystemInfo source, DirectoryInfo target)
        {
            if (MessageBox.Show($"Перенести всё из {source.FullName} в {target.FullName}?",
                    "Обнаружено устройство", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return false;
            }

            Status.Content = "Обнаружено устройство. Перенос";
            await Task.Run(() => DataManager.MoveFolder(source, target));
            return true;
        }

        private void OnDriveConnectedInvoker(string driveName)
        {
            Dispatcher.Invoke(() => OnDriveConnected(driveName));
        }

        private async void OnDriveConnected(string driveName)
        {
            _dataManager.StopWatch();
            LockButtons(true, true);
            Status.Content = "Обнаружено устройство. Анализ папок";

            List<DirectoryInfo> sourceFolders = await Task.Run(() => DataManager.EnumerateDriveFolders(driveName));
            switch (sourceFolders.Count)
            {
                case 0:
                    MessageBox.Show("На устройстве не обнаружено папок", "Обнаружено устройство", MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    break;
                case 1:
                    DirectoryInfo source = sourceFolders.Single();
                    _localFolder = null;
                    Status.Content = "Обнаружено устройство. Анализ файлов";
                    DateTime earliest = await Task.Run(() => DataManager.GetMinLastWriteTime(source));
                    DateTime now = DateTime.Now;
                    TimeSpan passed = now - earliest;
                    if (passed <= _timepadLookup)
                    {
                        // Timepad
                        Status.Content = "Обнаружено устройство. Анализ событий Timepad";
                        List<Event> events = await Task.Run(() =>
                            DataManager.GetTimepadEvents(_organizationId, earliest - _timepadLookup, earliest));
                        Event e;
                        switch (events.Count)
                        {
                            case 0:
                                // Lost
                                Status.Content = "Обнаружено устройство. Подходящие события не найдены";
                                _localFolder = await MoveFromDeviceToLost(source, now);
                                break;
                            case 1:
                                e = events.Single();
                                _localFolder = await MoveFromDeviceToEvent(e, source);
                                break;
                            default:
                                var selectionWindow = new SelectionWindow(events);
                                selectionWindow.ShowDialog();
                                e = events.FirstOrDefault(x => x.Id == selectionWindow.SelectedId);
                                if (e != null)
                                {
                                    _localFolder = await MoveFromDeviceToEvent(e, source);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (passed >= _deviceDateWarningMonths)
                        {
                            MessageBox.Show($"На устройстве обнаружен файл с датой изменения {earliest:dd.MM.yyyy}. Проверьте настройки устройства.",
                                            "Обнаружено устройство", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        // Lost
                        _localFolder = await MoveFromDeviceToLost(source, now);
                    }

                    Status.Content = "Готов";
                    if (_localFolder != null)
                    {
                        AddFolder();
                        if (MessageBox.Show("Всё готово к загрузке на Google Диск. Приступить?", "OK",
                                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            await Upload();
                        }
                    }
                    break;
                default:
                    MessageBox.Show("На устройстве обнаружено более одной папки", "Обнаружено устройство",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
            }

            _dataManager.StartWatch();
            LockButtons(false, true);
            Status.Content = "Готов";
        }

        private async Task<DirectoryInfo> MoveFromDeviceToEvent(Event e, FileSystemInfo source)
        {
            string targetName = $"{e.StartsAt:yyyy-MM-dd} {e.Name.Replace(":", " -")}";
            string targetPath = Path.Combine(_downloadPath, targetName);
            var target = new DirectoryInfo(targetPath);
            bool moved = await MoveFromDevice(source, target);
            return moved ? target : null;
        }

        private async Task<DirectoryInfo> MoveFromDeviceToLost(FileSystemInfo source, DateTime now)
        {
            string targetName = now.ToString("yyyy-MM-dd");
            string targetPath = Path.Combine(_lostPath, targetName);
            var target = new DirectoryInfo(targetPath);
            bool moved = await MoveFromDevice(source, target);
            return moved ? target : null;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _dataManager.Dispose();
        }

        private void ScrollViewerDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if ((files == null) || (files.Length != 1))
            {
                return;
            }

            AddFolder(files[0]);
        }

        private void ButtonSet_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                _dataManager.FileStatuses.Clear();
                AddFolder(dialog.SelectedPath);
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            _dataManager.FileStatuses.Clear();
            TextBoxPath.Text = "";
            TextBoxName.Text = "";

            UpdateUI();
        }

        private async void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            await Upload();
        }

        private async Task Upload()
        {
            _dataManager.ShouldCancel = false;

            string name = TextBoxName.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<FileInfo> files = _dataManager.FileStatuses.Keys.ToList();
            if (files.Count == 0)
            {
                MessageBox.Show("Выберите файлы для загрузки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _dataManager.StopWatch();
            LockButtons(true, false);

            Status.Content = $"Ищу/cоздаю папку {name}";
            string parentId = await Task.Run(() => _dataManager.GetOrCreateFolder(name));

            while (true)
            {
                if (!_dataManager.ShouldCancel)
                {
                    for (int i = 0; i < files.Count; ++i)
                    {
                        if (_dataManager.ShouldCancel)
                        {
                            break;
                        }

                        await UploadFile(parentId, files, i);
                    }
                }

                Status.Content = "Готов";

                files = _dataManager.GetFailedFiles();
                if (files.Count == 0)
                {
                    if (MessageBox.Show("Все файлы загружены успешно. Удалить их папку с компьютера?", "OK",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _localFolder.Delete(true);
                        Clear();
                    }
                    break;
                }

                if (MessageBox.Show("Некоторые файлы загрузить не удалось. Попытаться загрузить их ещё раз?", "Ошибка",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    break;
                }

                _dataManager.RemoveFiles(_dataManager.FileStatuses.Keys.Except(files).ToList());
                _dataManager.ShouldCancel = false;
                ButtonCancel.IsEnabled = true;
                UpdateUI();
            }

            _dataManager.StartWatch();
            LockButtons(false, true);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _dataManager.ShouldCancel = true;
            Status.Content += ". Отменяю...";
            UpdateUI();
        }

        private void UpdateUI()
        {
            ButtonClear.IsEnabled &= !string.IsNullOrWhiteSpace(TextBoxName.Text) || _dataManager.FileStatuses.Any();
            ButtonUpload.IsEnabled &= !string.IsNullOrWhiteSpace(TextBoxName.Text) && _dataManager.FileStatuses.Any();
            ButtonCancel.IsEnabled &= !_dataManager.ShouldCancel;

            SyncStackPanel();
        }

        private void LockButtons(bool shouldLock, bool shouldLockCancel)
        {
            ButtonSet.IsEnabled = !shouldLock;
            ButtonClear.IsEnabled = !shouldLock;
            ButtonUpload.IsEnabled = !shouldLock;
            ButtonCancel.IsEnabled = !shouldLockCancel;

            UpdateUI();
        }

        private void AddFolder(string path)
        {
            _localFolder = new DirectoryInfo(path);
            AddFolder();
        }

        private DirectoryInfo _localFolder;

        private void AddFolder()
        {
            if (!_localFolder.Exists)
            {
                return;
            }

            List<FileInfo> files = _localFolder.EnumerateFiles().ToList();
            if (!files.Any())
            {
                MessageBox.Show($"Папка {_localFolder.FullName} не содержит файлов!", "Ошибка", MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            TextBoxPath.Text = _localFolder.Parent?.FullName ?? "";
            TextBoxName.Text = _localFolder.Name;
            _dataManager.AddFiles(files);
            UpdateUI();
        }

        private void SyncStackPanel()
        {
            StackPanel.Children.RemoveRange(0, StackPanel.Children.Count);

            foreach (Grid grid in _dataManager.FileStatuses.Keys.Select(f => f.Name).OrderBy(n => n).Select(CreateElement))
            {
                StackPanel.Children.Add(grid);
            }
        }

        private async Task UploadFile(string parentId, IReadOnlyList<FileInfo> files, int index)
        {
            FileInfo file = files[index];
            Status.Content = $"Загружаю файлы ({index + 1}/{files.Count})";
            _currentProgressBar = GetProgressBar(file);
            long? size = await Task.Run(() => _dataManager.UploadFile(file, parentId, 10, UpdateBarInvoker));
            bool success = size.HasValue && (size.Value == file.Length);
            _dataManager.FileStatuses[file] = success;
            _currentProgressBar.Value = success ? 100 : 0;
        }

        private void UpdateBarInvoker(float val)
        {
            Dispatcher.Invoke(() => UpdateBar(val));
        }

        private void UpdateBar(float val)
        {
            _currentProgressBar.Value = (int)Math.Round(val * 100);
        }

        private static Grid CreateElement(string name)
        {
            var grid = new Grid
            {
                Width = double.NaN
            };

            var progressBar = new ProgressBar();

            var textBlock = new TextBlock
            {
                Text = name,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2.0)
            };

            grid.Children.Add(progressBar);
            grid.Children.Add(textBlock);
            return grid;
        }

        private ProgressBar GetProgressBar(FileSystemInfo file)
        {
            return
                StackPanel.Children.Cast<Grid>().Where(grid => ((TextBlock) grid.Children[1]).Text == file.Name)
                                                .Select(grid => (ProgressBar) grid.Children[0])
                                                .FirstOrDefault();
        }

        private ProgressBar _currentProgressBar;
        private readonly DataManager _dataManager;
        private readonly string _downloadPath;
        private readonly string _lostPath;
        private readonly TimeSpan _timepadLookup;
        private readonly TimeSpan _deviceDateWarningMonths;
        private readonly int _organizationId;

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            OnDriveConnectedInvoker("F:");
        }
    }
}

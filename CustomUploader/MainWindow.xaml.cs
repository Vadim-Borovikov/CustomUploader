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
            _timepadLookupTime = TimeSpan.FromHours(timepadHours);

            int deviceDateWarningDays = int.Parse(ConfigurationManager.AppSettings.Get("deviceDateWarningDays"));
            _deviceDateWarningTime = TimeSpan.FromDays(deviceDateWarningDays);

            _deviceFolders =
                ConfigurationManager.AppSettings.Get("deviceFolders").Split(';').ToList();

            _firstDeviceLetter =
                ConfigurationManager.AppSettings.Get("firstDeviceLetter").Single();

            _organizationId = int.Parse(ConfigurationManager.AppSettings.Get("organizationId"));

            _dataManager.StartWatch();

            LockButtons(false, true);
            Status.Content = "Готов";
        }

        private async Task<bool> MoveFromDevice(FileSystemInfo source, DirectoryInfo target)
        {
            MessageBoxResult res =
                MessageBox.Show($"Перенести всё из {source.FullName} в {target.FullName}?",
                                "Обнаружено устройство",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
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
            await ProcessDrive(driveName, true);
        }

        private async Task<bool> ProcessDrive(string driveName, bool foldersNotFoundReport)
        {
            bool processed = false;

            _dataManager.StopWatch();
            LockButtons(true, true);

            DirectoryInfo source = await GetSource(driveName, foldersNotFoundReport);
            if (source != null)
            {
                processed = true;

                DirectoryInfo target = await DetectTarget(source);
                if (target != null)
                {
                    bool moved = await MoveFromDevice(source, target);
                    _localFolder = moved ? target : null;

                    Status.Content = "Готов";
                    if (_localFolder != null)
                    {
                        AddFolder();
                        MessageBoxResult res =
                            MessageBox.Show("Всё готово к загрузке на Google Диск. Приступить?",
                                            "OK",
                                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            await Upload();
                        }
                    }
                }
            }

            _dataManager.StartWatch();
            LockButtons(false, true);
            Status.Content = "Готов";
            return processed;
        }

        private async Task<DirectoryInfo> GetSource(string driveName, bool foldersNotFoundReport)
        {
            Status.Content = "Обнаружено устройство. Анализ папок";

            string path =
                await Task.Run(() => _deviceFolders.Select(p => Path.Combine(driveName, p))
                                                  .FirstOrDefault(Directory.Exists));
            if (path != null)
            {
                return new DirectoryInfo(path);
            }

            if (foldersNotFoundReport)
            {
                MessageBox.Show("На устройстве не обнаружено ожидаемых папок",
                                "Обнаружено устройство",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return null;
        }

        private async Task<DateTime?> GetMinLastWriteTime(DirectoryInfo source)
        {
            Status.Content = "Обнаружено устройство. Анализ файлов";

            DateTime? earliest = await Task.Run(() => DataManager.GetMinLastWriteTime(source));

            if (!earliest.HasValue)
            {
                MessageBox.Show("На устройстве не обнаружено файлов", "Обнаружено устройство",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return earliest;
        }

        private async Task<Event> GetTimepadEvent(DateTime earliest)
        {
            Status.Content = "Обнаружено устройство. Анализ событий Timepad";

            List<Event> events = await Task.Run(() =>
                DataManager.GetTimepadEvents(_organizationId, earliest - _timepadLookupTime, earliest));
            switch (events.Count)
            {
                case 0:
                    Status.Content = "Обнаружено устройство. Подходящие события не найдены";
                    return null;
                case 1:
                    return events.Single();
                default:
                    var selectionWindow = new SelectionWindow(events);
                    selectionWindow.ShowDialog();
                    return events.FirstOrDefault(x => x.Id == selectionWindow.SelectedId);
            }
        }

        private async Task<DirectoryInfo> DetectTarget(DirectoryInfo source)
        {
            DateTime? earliest = await GetMinLastWriteTime(source);
            if (!earliest.HasValue)
            {
                return null;
            }

            DateTime now = DateTime.Now;
            TimeSpan passed = now - earliest.Value;
            string parentPath = _lostPath;
            string targetName = now.ToString("yyyy-MM-dd");
            if (passed <= _timepadLookupTime)
            {
                Event e = await GetTimepadEvent(earliest.Value);
                parentPath = _downloadPath;
                targetName = $"{e.StartsAt:yyyy-MM-dd} {e.Name.Replace(":", " -")}";
            }

            if (passed >= _deviceDateWarningTime)
            {
                MessageBox.Show(
                    $"На устройстве обнаружен файл с датой изменения {earliest:dd.MM.yyyy}. Проверьте настройки устройства.",
                    "Обнаружено устройство", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            string targetPath = Path.Combine(parentPath, targetName);
            return new DirectoryInfo(targetPath);
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
                MessageBox.Show("Введите название!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<FileInfo> files = _dataManager.FileStatuses.Keys.ToList();
            if (files.Count == 0)
            {
                MessageBox.Show("Выберите файлы для загрузки!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBoxResult res;
                if (files.Count == 0)
                {
                    res = MessageBox.Show("Все файлы загружены успешно. Удалить их папку с компьютера?",
                                          "OK", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        _localFolder.Delete(true);
                        Clear();
                    }
                    break;
                }

                res = MessageBox.Show("Некоторые файлы загрузить не удалось. Попытаться загрузить их ещё раз?",
                                      "Ошибка", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes)
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

        private async void ButtonScan_Click(object sender, RoutedEventArgs e)
        {
            LockButtons(true, true);
            bool found = false;

            for (char c = _firstDeviceLetter; c <= 'Z'; ++c)
            {
                string path = $"{c}:";
                if (!Directory.Exists(path))
                {
                    continue;
                }

                found = await ProcessDrive(path, false);
                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                MessageBox.Show("Не обнаружено устройств с ожидаемыми папками", "Не удалось",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

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
            ButtonScan.IsEnabled = !shouldLock;
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
                MessageBox.Show($"Папка {_localFolder.FullName} не содержит файлов!", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
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
        private readonly TimeSpan _timepadLookupTime;
        private readonly TimeSpan _deviceDateWarningTime;
        private readonly int _organizationId;
        private readonly List<string> _deviceFolders;
        private readonly char _firstDeviceLetter;
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CustomUploader.Logic;

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
            _dataManager = new DataManager(clientSecretPath, parentId);

            LockButtons(false);
            Status.Content = "Готов";
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
            _dataManager.FileStatuses.Clear();
            SyncStackPanel();
            TextBoxPath.Text = "";
            TextBoxName.Text = "";
        }

        private async void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            _dataManager.ShouldCancel = false;

            string name = TextBoxName.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название!");
                return;
            }

            List<FileInfo> files = _dataManager.FileStatuses.Keys.ToList();
            if (files.Count == 0)
            {
                MessageBox.Show("Выберите файлы для загрузки!");
                return;
            }

            LockButtons(true);

            Status.Content = $"Ищу/cоздаю папку {name}";
            string parentId = await _dataManager.GetOrCreateFolder(name);

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
                    MessageBox.Show("Все файлы загружены успешно", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
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
                SyncStackPanel();
            }

            LockButtons(false);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            _dataManager.ShouldCancel = true;
            Status.Content += ". Отменяю...";
            ButtonCancel.IsEnabled = false;
        }

        private void LockButtons(bool shouldLock)
        {
            ButtonSet.IsEnabled = !shouldLock;
            ButtonClear.IsEnabled = !shouldLock;
            ButtonUpload.IsEnabled = !shouldLock;
            ButtonCancel.IsEnabled = shouldLock;
        }

        private void AddFolder(string path)
        {
            var directoryInfo = new DirectoryInfo(path);
            if (!directoryInfo.Exists)
            {
                return;
            }

            List<FileInfo> files = directoryInfo.EnumerateFiles().ToList();
            if (!files.Any())
            {
                MessageBox.Show($"Папка {directoryInfo.FullName} не содержит файлов!");
                return;
            }

            TextBoxPath.Text = directoryInfo.Parent?.FullName ?? "";
            TextBoxName.Text = directoryInfo.Name;
            _dataManager.AddFiles(files);
            SyncStackPanel();
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
            bool success = await _dataManager.UploadFile(file, parentId, 10, UpdateBar);
            _dataManager.FileStatuses[file] = success;
            _currentProgressBar.Value = success ? 100 : 0;
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
    }
}

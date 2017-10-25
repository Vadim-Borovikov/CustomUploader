using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CustomUploader.Logic;
using Microsoft.Win32;

namespace CustomUploader.WPF
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

        private void StackPanelDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void StackPanelDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            AddFiles(openFileDialog.FileNames);
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            _dataManager.FileStatuses.Clear();
            SyncStackPanel();
            TextBox.Text = "";
        }

        private async void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            _dataManager.ShouldCancel = false;

            string name = TextBox.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название!");
                return;
            }

            List<string> fileNames = _dataManager.FileStatuses.Keys.ToList();
            if (fileNames.Count == 0)
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
                    for (int i = 0; i < fileNames.Count; ++i)
                    {
                        if (_dataManager.ShouldCancel)
                        {
                            break;
                        }

                        await UploadFile(parentId, fileNames, i);
                    }
                }

                Status.Content = "Готов";

                fileNames = _dataManager.GetFailedFiles();
                if (fileNames.Count == 0)
                {
                    MessageBox.Show("Все файлы загружены успешно", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }

                if (MessageBox.Show("Некоторые файлы загрузить не удалось. Попытаться загрузить их ещё раз?", "Ошибка",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    break;
                }

                _dataManager.RemoveFiles(_dataManager.FileStatuses.Keys.Except(fileNames).ToList());
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
            ButtonAdd.IsEnabled = !shouldLock;
            ButtonClear.IsEnabled = !shouldLock;
            ButtonUpload.IsEnabled = !shouldLock;
            ButtonCancel.IsEnabled = shouldLock;
        }

        private void AddFiles(IEnumerable<string> files)
        {
            _dataManager.AddFiles(files);
            SyncStackPanel();
        }

        private void SyncStackPanel()
        {
            StackPanel.Children.RemoveRange(0, StackPanel.Children.Count);

            foreach (Grid grid in _dataManager.FileStatuses.Keys.Select(CreateElement))
            {
                StackPanel.Children.Add(grid);
            }
        }

        private async Task UploadFile(string parentId, IReadOnlyList<string> fileNames, int index)
        {
            string file = fileNames[index];
            Status.Content = $"Загружаю файлы ({index + 1}/{fileNames.Count})";
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

        private ProgressBar GetProgressBar(string fileName)
        {
            return
                StackPanel.Children.Cast<Grid>().Where(grid => ((TextBlock) grid.Children[1]).Text == fileName)
                                                .Select(grid => (ProgressBar) grid.Children[0])
                                                .FirstOrDefault();
        }

        private ProgressBar _currentProgressBar;
        private readonly DataManager _dataManager;
    }
}

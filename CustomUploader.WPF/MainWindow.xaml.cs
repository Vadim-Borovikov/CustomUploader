using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        private void ListBoxDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void ListBoxDrop(object sender, DragEventArgs e)
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
            SyncListBox();
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
                SyncListBox();
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
            SyncListBox();
        }

        private void SyncListBox()
        {
            ListBox.Items.Clear();
            foreach (string file in _dataManager.FileStatuses.Keys)
            {
                string prefix = _dataManager.FileStatuses[file] ? SuccessPrefix : DefaultPrefix;
                ListBox.Items.Add($"{prefix}{file}");
            }
        }

        private async Task UploadFile(string parentId, IReadOnlyList<string> fileNames, int index)
        {
            string file = fileNames[index];
            Status.Content = $"Загружаю {Path.GetFileName(file)} ({index + 1}/{fileNames.Count})";

            ProgressBar.Value = 0;

            bool success = await _dataManager.UploadFile(file, parentId, 10, UpdateBar);
            _dataManager.FileStatuses[file] = success;
            SyncListBox();
        }

        private void UpdateBar(float val)
        {
            if (val >= 0)
            {
                ProgressBar.Value = (int)Math.Round(val * 100);
            }
        }

        private readonly DataManager _dataManager;
        private const string SuccessPrefix = "[Загружен] ";
        private const string DefaultPrefix = "[Не загружен] ";
    }
}

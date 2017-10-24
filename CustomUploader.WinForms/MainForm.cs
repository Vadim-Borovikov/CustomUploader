using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CustomUploader.Logic;

namespace CustomUploader
{
    internal partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            toolStripStatusLabel.Text = "Загрузка...";

            string clientSecretPath = ConfigurationManager.AppSettings.Get("clientSecretPath");
            string parentId = ConfigurationManager.AppSettings.Get("parentId");
            _dataManager = new DataManager(clientSecretPath, parentId);

            LockButtons(false);
            toolStripStatusLabel.Text = "Готов";
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            _dataManager.Dispose();
        }

        private void listBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void listBox_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            AddFiles(openFileDialog.FileNames);
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            _dataManager.RemoveFiles(listBox.SelectedItems.Cast<string>());
            SyncListBox();
        }

        private async void buttonUpload_Click(object sender, EventArgs e)
        {
            _dataManager.ShouldCancel = false;

            if (toolStripProgressBar.ProgressBar == null)
            {
                throw new Exception("Progress bar error!");
            }

            string name = textBox.Text;
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

            toolStripStatusLabel.Text = $"Ищу/cоздаю папку {name}";
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

                        string file = fileNames[i];
                        toolStripStatusLabel.Text = $"Загружаю {Path.GetFileName(file)} ({i + 1}/{fileNames.Count})";

                        toolStripProgressBar.ProgressBar.Value = 0;

                        bool success = await _dataManager.UploadFile(file, parentId, 10, UpdateBar);
                        _dataManager.FileStatuses[file] = success;
                        SyncListBox();
                    }
                }

                toolStripStatusLabel.Text = "Готов";

                fileNames = _dataManager.GetFailedFiles();
                if (fileNames.Count == 0)
                {
                    MessageBox.Show("Все файлы загружены успешно", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }

                if (MessageBox.Show("Некоторые файлы загрузить не удалось. Попытаться загрузить их ещё раз?", "Ошибка",
                                    MessageBoxButtons.RetryCancel, MessageBoxIcon.Question) != DialogResult.Retry)
                {
                    break;
                }

                _dataManager.RemoveFiles(_dataManager.FileStatuses.Keys.Except(fileNames).ToList());
                _dataManager.ShouldCancel = false;
                buttonCancel.Enabled = true;
                SyncListBox();
            }

            LockButtons(false);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _dataManager.ShouldCancel = true;
            toolStripStatusLabel.Text += ". Отменяю...";
            buttonCancel.Enabled = false;
        }

        private void AddFiles(IEnumerable<string> files)
        {
            _dataManager.AddFiles(files);
            SyncListBox();
        }

        private void SyncListBox()
        {
            listBox.Items.Clear();
            foreach (string file in _dataManager.FileStatuses.Keys)
            {
                string prefix = _dataManager.FileStatuses[file] ? SuccessPrefix : DefaultPrefix;
                listBox.Items.Add($"{prefix}{file}");
            }
        }

        private void LockButtons(bool shouldLock)
        {
            buttonAdd.Enabled = !shouldLock;
            buttonRemove.Enabled = !shouldLock;
            buttonUpload.Enabled = !shouldLock;
            buttonCancel.Enabled = shouldLock;
        }

        private void UpdateBar(float val)
        {
            if (val >= 0)
            {
                toolStripProgressBar.Value = (int)Math.Round(val * 100);
            }
        }

        private readonly DataManager _dataManager;
        private const string SuccessPrefix = "[Загружен] ";
        private const string DefaultPrefix = "[Не загружен] ";
    }
}

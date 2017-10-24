using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
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

            toolStripStatusLabel.Text = "Готов";
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            _dataManager.Dispose();
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            _dataManager.AddFiles(openFileDialog.FileNames);
            SyncListBox();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            _dataManager.RemoveFiles(openFileDialog.FileNames);
            SyncListBox();
        }

        private async void buttonUpload_Click(object sender, EventArgs e)
        {
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

            List<string> fileNames = _dataManager.FileNames.ToList();
            if (fileNames.Count == 0)
            {
                MessageBox.Show("Выберите файлы для загрузки!");
                return;
            }

            LockButtons(true);

            toolStripStatusLabel.Text = $"Ищу/cоздаю папку {name}...";
            string parentId = await _dataManager.GetOrCreateFolder(name);

            var failedFiles = new SortedSet<string>();

            while (true)
            {
                failedFiles.Clear();
                for (int i = 0; i < fileNames.Count; ++i)
                {
                    string file = fileNames[i];
                    toolStripStatusLabel.Text = $"Загружаю {Path.GetFileName(file)} ({i + 1}/{fileNames.Count})";

                    toolStripProgressBar.ProgressBar.Value = 0;

                    bool success = await _dataManager.UploadFile(file, parentId, 10, UpdateBar);
                    if (!success)
                    {
                        failedFiles.Add(file);
                    }
                }

                toolStripStatusLabel.Text = "Готов";

                if (failedFiles.Count == 0)
                {
                    MessageBox.Show("Все файлы загружены успешно", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Не удалось загрузить файлы:");
                foreach (string file in failedFiles)
                {
                    sb.AppendLine(Path.GetFileName(file));
                }
                sb.AppendLine();
                sb.AppendLine("Попытаться загрузить их ещё раз?");
                if (MessageBox.Show(sb.ToString(), "Ошибка", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question) != DialogResult.Retry)
                {
                    break;
                }
                fileNames = failedFiles.ToList();
            }

            LockButtons(false);
        }

        private void SyncListBox()
        {
            listBox.Items.Clear();
            listBox.Items.AddRange(_dataManager.FileNames.Select(x => x as object).ToArray());
        }

        private void LockButtons(bool shouldLock)
        {
            buttonAdd.Enabled = !shouldLock;
            buttonRemove.Enabled = !shouldLock;
            buttonUpload.Enabled = !shouldLock;
        }

        private void UpdateBar(float val)
        {
            if (val >= 0)
            {
                toolStripProgressBar.Value = (int)Math.Round(val * 100);
            }
        }

        private readonly DataManager _dataManager;
    }
}

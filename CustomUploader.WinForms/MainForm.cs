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
            _provider = new GoogleApisDriveProvider(clientSecretPath);

            _parentId = ConfigurationManager.AppSettings.Get("parentId");
            toolStripStatusLabel.Text = "Готов";
        }

        private void MainForm_Closing(object sender, FormClosingEventArgs e)
        {
            _provider.Dispose();
        }

        private void listBox_DragEnter(object sender, DragEventArgs e)
        {
            MessageBox.Show("listBox_DragEnter!");
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void listBox_DragDrop(object sender, DragEventArgs e)
        {
            MessageBox.Show("listBox_DragDrop!");
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AddFiles(openFileDialog.FileNames);
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            foreach (string file in listBox.SelectedItems)
            {
                _files.Remove(file);
            }
            SyncListBox();
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            UploadFiles(textBox.Text, _files.ToList());
        }

        private void AddFiles(IEnumerable<string> files)
        {
            foreach (string file in files)
            {
                _files.Add(file);
            }
            SyncListBox();
        }

        private void SyncListBox()
        {
            listBox.Items.Clear();
            listBox.Items.AddRange(_files.Select(x => x as object).ToArray());
        }

        private async void UploadFiles(string name, IList<string> files)
        {
            if (toolStripProgressBar.ProgressBar == null)
            {
                throw new Exception("Progress bar error!");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название!");
                return;
            }

            if (files.Count == 0)
            {
                MessageBox.Show("Выберите файлы для загрузки!");
                return;
            }

            LockButtons(true);

            toolStripStatusLabel.Text = $"Создаю папку {name}...";
            string folderId = await DataManager.CreateFolder(_provider, textBox.Text, _parentId);

            while (true)
            {
                var failedFiles = new SortedSet<string>();
                for (int i = 0; i < files.Count; ++i)
                {
                    string file = files[i];
                    toolStripStatusLabel.Text = $"Загружаю {Path.GetFileName(file)} ({i + 1}/{files.Count})";

                    toolStripProgressBar.ProgressBar.Value = 0;

                    var progress = new Progress<float>(UpdateBar);
                    bool success = await _provider.Upload(file, folderId, 10, progress);
                    if (!success)
                    {
                        failedFiles.Add(file);
                    }
                }

                toolStripStatusLabel.Text = "Готов";
                LockButtons(false);

                if (failedFiles.Count == 0)
                {
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Не удалось загрузить файлы:");
                foreach (string file in failedFiles)
                {
                    sb.AppendLine(Path.GetFileName(file));
                }
                sb.AppendLine();
                sb.AppendLine("Попытаться загрузить их ещё раз?");
                if (MessageBox.Show(sb.ToString(), "Ошибка", MessageBoxButtons.RetryCancel) != DialogResult.Retry)
                {
                    return;
                }
                files = failedFiles.ToList();
            }
        }

        private void LockButtons(bool shouldLock)
        {
            buttonAdd.Enabled = !shouldLock;
            buttonRemove.Enabled = !shouldLock;
            buttonUpload.Enabled = !shouldLock;
        }

        private void UpdateBar(float val)
        {
            if (val > 0)
            {
                toolStripProgressBar.Value = (int) Math.Round(val * 100);
            }
        }

        private static string _parentId;
        private readonly SortedSet<string> _files = new SortedSet<string>();
        private readonly GoogleApisDriveProvider _provider;
    }
}

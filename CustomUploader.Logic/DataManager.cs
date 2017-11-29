using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.VisualBasic.FileIO;

namespace CustomUploader.Logic
{
    public class DataManager : IDisposable
    {
        public DataManager(string clientSecretJson, string parentId, Action<string> onDriveConnected)
        {
            FileStatuses = new Dictionary<FileInfo, bool>();
            ShouldCancel = false;

            _parentId = parentId;

            using (var stream = new FileStream(clientSecretJson, FileMode.Open, FileAccess.Read))
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string credentialPath = Path.Combine(folderPath, ".credentials/drive-dotnet-quickstart.json");

                _provider = new GoogleApisDriveProvider(stream, credentialPath, "user", CancellationToken.None);
            }

            _deviceInsertListener = new DeviceInsertListener(onDriveConnected);
        }

        public void StartWatch()
        {
            _deviceInsertListener.StartWatch();
        }

        public void StopWatch()
        {
            _deviceInsertListener.StopWatch();
        }

        public static FileInfo GetMinLastWriteTimeFile(DirectoryInfo source)
        {
            return source.EnumerateFiles().OrderBy(f => f?.LastWriteTime).FirstOrDefault();
        }

        public void Dispose()
        {
            _provider.Dispose();
            _deviceInsertListener.Dispose();
        }

        public static void MoveFolder(FileSystemInfo source, DirectoryInfo target)
        {
            FileSystem.MoveDirectory(source.FullName, target.FullName, UIOption.AllDialogs);
            List<FileInfo> files = target.EnumerateFiles().ToList();
            foreach (FileInfo file in files)
            {
                string newName = $"{file.LastWriteTime:yyyy-MM-dd HH_mm}{file.Extension}";
                string newPath = Path.Combine(file.DirectoryName ?? "", newName);
                file.MoveTo(newPath);
            }
        }

        public void AddFiles(IEnumerable<FileInfo> files)
        {
            foreach (FileInfo file in files.Where(f => !FileStatuses.ContainsKey(f)))
            {
                FileStatuses.Add(file, false);
            }
        }

        public void RemoveFiles(IEnumerable<FileInfo> files)
        {
            foreach (FileInfo file in files)
            {
                FileStatuses.Remove(file);
            }
        }

        public List<FileInfo> GetFailedFiles()
        {
            return FileStatuses.Where(p => !p.Value).Select(p => p.Key).ToList();
        }

        public string GetOrCreateFolder(string name)
        {
            IEnumerable<string> foldersIds = _provider.GetFoldersIds(name, _parentId);
            List<string> foldersIdsList = foldersIds.ToList();

            if (foldersIdsList.Count == 1)
            {
                return foldersIdsList.First();
            }

            return _provider.CreateFolder(name, _parentId);
        }

        public long? UploadFile(FileInfo file, string parentId, int maxTries, Action<float> progressHandler)
        {
            if (!file.Exists)
            {
                return null;
            }

            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
            {
                string mimeType = MimeMapping.GetMimeMapping(file.FullName);

                long size = stream.Length;
                var progress = new Progress<long>(bytesSent => HandleProgress(bytesSent, size, progressHandler));

                Func<int, bool> shouldAbort = currentTry => ShouldAbort(currentTry, maxTries);

                return _provider.UploadFile(file.Name, mimeType, parentId, stream, progress, shouldAbort);
            }
        }

        private bool ShouldAbort(int currentTry, int maxTries)
        {
            return ShouldCancel || (currentTry >= maxTries);
        }

        public bool ShouldCancel;

        private static void HandleProgress(long bytesSent, long size, Action<float> progressHandler)
        {
            float progress = 1.0f;
            if (size > 0)
            {
                progress = 1.0f * bytesSent / size;
            }
            progressHandler(progress);
        }

        private readonly string _parentId;
        public readonly Dictionary<FileInfo, bool> FileStatuses;
        private readonly GoogleApisDriveProvider _provider;
        private readonly DeviceInsertListener _deviceInsertListener;
    }
}

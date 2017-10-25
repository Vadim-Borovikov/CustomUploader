using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CustomUploader.Logic
{
    public class DataManager : IDisposable
    {
        public DataManager(string clientSecretJson, string parentId)
        {
            FileStatuses = new Dictionary<string, bool>();
            ShouldCancel = false;

            _parentId = parentId;

            using (var stream = new FileStream(clientSecretJson, FileMode.Open, FileAccess.Read))
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string credentialPath = Path.Combine(folderPath, ".credentials/drive-dotnet-quickstart.json");

                _provider = new GoogleApisDriveProvider(stream, credentialPath, "user", CancellationToken.None);
            }
        }

        public void Dispose()
        {
            _provider.Dispose();
        }

        public void AddFiles(IEnumerable<string> fileNames)
        {
            foreach (string file in fileNames.Where(file => !FileStatuses.ContainsKey(file)))
            {
                FileStatuses.Add(file, false);
            }
        }

        public void RemoveFiles(IEnumerable<string> fileNames)
        {
            foreach (string file in fileNames)
            {
                FileStatuses.Remove(file);
            }
        }

        public List<string> GetFailedFiles()
        {
            return FileStatuses.Where(p => !p.Value).Select(p => p.Key).ToList();
        }

        public async Task<string> GetOrCreateFolder(string name)
        {
            IEnumerable<string> foldersIds = await _provider.GetFoldersIds(name, _parentId);
            List<string> foldersIdsList = foldersIds.ToList();

            if (foldersIdsList.Count == 1)
            {
                return foldersIdsList.First();
            }

            return await _provider.CreateFolder(name, _parentId);
        }

        public async Task<bool> UploadFile(string path, string parentId, int maxTries, Action<float> progressHandler)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string name = Path.GetFileName(path);
                string mimeType = MimeMapping.GetMimeMapping(path);

                long size = stream.Length;
                var progress = new Progress<long>(bytesSent => HandleProgress(bytesSent, size, progressHandler));

                Func<int, bool> shouldAbort = currentTry => ShouldAbort(currentTry, maxTries);

                return await _provider.UploadFile(name, mimeType, parentId, stream, progress, shouldAbort);
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
        public readonly Dictionary<string, bool> FileStatuses;
        private readonly GoogleApisDriveProvider _provider;
    }
}

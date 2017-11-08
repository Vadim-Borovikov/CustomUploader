using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CustomUploader.Logic.Timepad.Data;
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

        public void Dispose()
        {
            _provider.Dispose();
            _deviceInsertListener.Dispose();
        }

        public static void MoveFolder(DirectoryInfo source, DirectoryInfo target)
        {
            FileSystem.MoveDirectory(source.FullName, target.FullName, UIOption.AllDialogs);
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

        public async Task<bool> UploadFile(FileInfo file, string parentId, int maxTries, Action<float> progressHandler)
        {
            if (!file.Exists)
            {
                return false;
            }

            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read))
            {
                string mimeType = MimeMapping.GetMimeMapping(file.FullName);

                long size = stream.Length;
                var progress = new Progress<long>(bytesSent => HandleProgress(bytesSent, size, progressHandler));

                Func<int, bool> shouldAbort = currentTry => ShouldAbort(currentTry, maxTries);

                return await _provider.UploadFile(file.Name, mimeType, parentId, stream, progress, shouldAbort);
            }
        }

        public static async Task<List<Event>> GetTimepadEvents(DateTime startsAtMin, DateTime startsAtMax, string baseUrl,
                                                               string resource, int organizationId)
        {
            var parameters = new Dictionary<string, object>
            {
                { "organization_ids", new[] { organizationId } },
                // { "limit", 10 },
                { "starts_at_min", startsAtMin },
                { "starts_at_max", startsAtMax }
            };
            Data data = await RestSharpProvider.ExecuteGetTaskAsync<Data>(baseUrl, resource, parameters);
            return data?.Values;
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

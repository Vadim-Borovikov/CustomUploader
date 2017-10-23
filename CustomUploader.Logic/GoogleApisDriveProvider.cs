using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using File = System.IO.File;

namespace CustomUploader.Logic
{
    public class GoogleApisDriveProvider : IDisposable
    {
        private readonly DriveService _driveService;

        private static readonly string[] Scopes = { DriveService.Scope.Drive };
        private const string ApplicationName = "CustomUploader";
        private const string FolderType = "application/vnd.google-apps.folder";

        public GoogleApisDriveProvider(string clientSecretJson)
        {
            UserCredential credential = CreateCredential(clientSecretJson);

            // Create Drive API service.
            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            };
            _driveService = new DriveService(initializer);
        }

        private static UserCredential CreateCredential(string clientSecretJson)
        {
            using (var stream = new FileStream(clientSecretJson, FileMode.Open, FileAccess.Read))
            {
                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string credentialPath = Path.Combine(folderPath, ".credentials/drive-dotnet-quickstart.json");

                GoogleClientSecrets secrets = GoogleClientSecrets.Load(stream);

                var credentialDataStore = new FileDataStore(credentialPath, true);

                Task<UserCredential> task =
                    GoogleWebAuthorizationBroker.AuthorizeAsync(secrets.Secrets, Scopes, "user",
                                                                CancellationToken.None, credentialDataStore);
                return task.Result;
            }
        }

        public List<Google.Apis.Drive.v3.Data.File> GetFolderIdsByName(string name, string parentId = null)
        {
            FilesResource.ListRequest request = _driveService.Files.List();
            request.Q = $"mimeType='{FolderType}' and name contains '{name}' and trashed = false";
            if (parentId != null)
            {
                request.Q += $" and '{parentId}' in parents";
            }
            request.PageSize = 10;
            request.Fields = "nextPageToken, files(id, name)";
            FileList result = request.Execute();
            return result.Files.Where(f => f.Name == name).ToList();
        }

        public async Task<Google.Apis.Drive.v3.Data.File> CreateFolder(string name, string parentId)
        {
            List<Google.Apis.Drive.v3.Data.File> files = GetFolderIdsByName(name, parentId);
            if (files.Count == 1)
            {
                return files[0];
            }

            var body = new Google.Apis.Drive.v3.Data.File
            {
                Name = name,
                MimeType = FolderType,
                Parents = new List<string> { parentId }
            };

            return await _driveService.Files.Create(body).ExecuteAsync();
        }

        public async Task<bool> Upload(string path, string parentId, int maxTries, IProgress<float> progress)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = Path.GetFileName(path),
                MimeType = MimeMapping.GetMimeMapping(path),
                Parents = new List<string> { parentId }
            };

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                float size = stream.Length;
                FilesResource.CreateMediaUpload request =
                    _driveService.Files.Create(fileMetadata, stream, MimeMapping.GetMimeMapping(path));
                request.Fields = "id";

                if (progress != null)
                {
                    request.ProgressChanged += p => progress.Report(p.BytesSent / size);
                }
                IUploadProgress uploadProgress = await request.UploadAsync();
                int tries = 0;
                while (uploadProgress.Status != UploadStatus.Completed)
                {
                    ++tries;
                    if (tries >= maxTries)
                    {
                        return false;
                    }
                    await request.ResumeAsync();
                }
                return true;
            }
        }

        public void Dispose()
        {
            _driveService.Dispose();
        }
    }
}

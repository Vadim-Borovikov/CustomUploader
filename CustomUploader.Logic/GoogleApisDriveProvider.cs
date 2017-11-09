using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using File = Google.Apis.Drive.v3.Data.File;

namespace CustomUploader.Logic
{
    internal class GoogleApisDriveProvider : IDisposable
    {
        private readonly DriveService _driveService;

        private static readonly string[] Scopes = { DriveService.Scope.Drive };
        private const string ApplicationName = "CustomUploader";
        private const string FolderType = "application/vnd.google-apps.folder";

        internal GoogleApisDriveProvider(Stream clientSecretStream, string credentialPath, string user,
                                         CancellationToken taskCancellationToken)
        {
            GoogleClientSecrets secrets = GoogleClientSecrets.Load(clientSecretStream);

            var credentialDataStore = new FileDataStore(credentialPath, true);

            Task<UserCredential> credentialTask =
                GoogleWebAuthorizationBroker.AuthorizeAsync(secrets.Secrets, Scopes, user, taskCancellationToken,
                                                            credentialDataStore);

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credentialTask.Result,
                ApplicationName = ApplicationName
            };

            _driveService = new DriveService(initializer);
        }

        public void Dispose()
        {
            _driveService.Dispose();
        }

        internal IEnumerable<string> GetFoldersIds(string name, string parentId)
        {
            return GetFilesIds(name, FolderType, parentId);
        }

        internal string CreateFolder(string name, string parentId)
        {
            var body = new File
            {
                Name = name,
                MimeType = FolderType,
                Parents = new List<string> { parentId }
            };
            FilesResource.CreateRequest createRequest = _driveService.Files.Create(body);

            File result = createRequest.Execute();
            return result.Id;
        }

        internal long? UploadFile(string name, string mimeType, string parentId, Stream fileStream,
                                  IProgress<long> progress, Func<int, bool> shouldAbort)
        {
            var fileMetadata = new File
            {
                Name = name,
                MimeType = mimeType,
                Parents = new List<string> { parentId }
            };
            FilesResource.CreateMediaUpload uploadRequest =
                _driveService.Files.Create(fileMetadata, fileStream, fileMetadata.MimeType);
            uploadRequest.Fields = "id";
            if (progress != null)
            {
                uploadRequest.ProgressChanged += u => progress.Report(u.BytesSent);
            }

            IUploadProgress uploadProgress = uploadRequest.Upload();

            int currentTry = 0;
            while (uploadProgress.Status != UploadStatus.Completed)
            {
                if (!shouldAbort(currentTry))
                {
                    return null;
                }
                ++currentTry;
                uploadRequest.Resume();
            }

            FilesResource.GetRequest fileRequest = _driveService.Files.Get(uploadRequest.ResponseBody.Id);
            fileRequest.Fields = "size";
            File file = fileRequest.Execute();

            return file.Size;
        }

        private IEnumerable<string> GetFilesIds(string name, string mimeType, string parentId)
        {
            FilesResource.ListRequest request = _driveService.Files.List();
            request.Q = $"name contains '{name}' and mimeType='{mimeType}' and trashed = false";
            if (parentId != null)
            {
                request.Q += $" and '{parentId}' in parents";
            }
            request.PageSize = 10;
            request.Fields = "nextPageToken, files(id, name)";

            FileList result = request.Execute();

            return result.Files.Where(f => f.Name == name).Select(f => f.Id);
        }
    }
}

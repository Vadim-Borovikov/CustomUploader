using System.Threading.Tasks;
using Google.Apis.Drive.v3.Data;

namespace CustomUploader.Logic
{
    public static class DataManager
    {
        public static async Task<string> CreateFolder(this GoogleApisDriveProvider provider, string name,
                                                      string parentId)
        {
            File folder = await provider.CreateFolder(name, parentId);
            return folder.Id;
        }
    }
}

using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace CustomUploader
{
    public class ConfigurationProvider
    {
        [JsonProperty]
        public readonly FileInfo ClientSecret;
        [JsonProperty]
        public readonly string ParentId;
        [JsonProperty]
        public readonly DirectoryInfo Download;
        [JsonProperty]
        public readonly TimeSpan DeviceDateWarningTime;
        [JsonProperty]
        public readonly string[] DeviceFolders;
        [JsonProperty]
        public readonly char FirstDeviceLetter;

        public ConfigurationProvider(Action<string> onError = null)
        {
            _onError = onError;

            ClientSecret = CheckFileInfoSetting("clientSecretPath", true);
            if (ClientSecret == null)
            {
                return;
            }

            ParentId = GetSetting("parentId");
            if (ParentId == null)
            {
                return;
            }

            Download = CheckDirectoryInfoSetting("downloadPath", false);
            if (Download == null)
            {
                return;
            }

            int? deviceDateWarningDays = CheckIntSetting("deviceDateWarningDays");
            if (!deviceDateWarningDays.HasValue)
            {
                return;
            }
            DeviceDateWarningTime = TimeSpan.FromDays(deviceDateWarningDays.Value);

            string deviceFoldersSetting = GetSetting("deviceFolders");
            if (deviceFoldersSetting == null)
            {
                return;
            }
            DeviceFolders = deviceFoldersSetting.Split(';');

            char? firstDeviceLetterSetting = CheckCharSetting("firstDeviceLetter");
            if (!firstDeviceLetterSetting.HasValue)
            {
                return;
            }
            FirstDeviceLetter = firstDeviceLetterSetting.Value;
        }

        private string GetSetting(string key)
        {
            string setting = ConfigurationManager.AppSettings.Get(key);
            if (setting == null)
            {
                _onError?.Invoke($"{key} не задан в настройках");
            }
            return setting;
        }

        private FileInfo CheckFileInfoSetting(string key, bool checkExistence)
        {
            string setting = GetSetting(key);
            if (setting == null)
            {
                return null;
            }

            var info = new FileInfo(setting);
            if (!checkExistence || info.Exists)
            {
                return info;
            }

            _onError?.Invoke($"Не найден файл {info.FullName}");
            return null;
        }

        private DirectoryInfo CheckDirectoryInfoSetting(string key, bool checkExistence)
        {
            string setting = GetSetting(key);
            if (setting == null)
            {
                return null;
            }

            var info = new DirectoryInfo(setting);
            if (!checkExistence || info.Exists)
            {
                return info;
            }

            _onError?.Invoke($"Не найдена папка {info.FullName}");
            return null;
        }

        private int? CheckIntSetting(string key)
        {
            string setting = GetSetting(key);
            if (setting == null)
            {
                return null;
            }

            bool success = int.TryParse(setting, out int result);
            if (success)
            {
                return result;
            }

            _onError?.Invoke($"Некорректное значение настройки {key}. Ожидается целое число");
            return null;
        }

        private char? CheckCharSetting(string key)
        {
            string setting = GetSetting(key);
            if (setting == null)
            {
                return null;
            }

            if (setting.Length != 1)
            {
                _onError?.Invoke($"Некорректное значение настройки {key}. Ожидается символ");
            }

            return setting.Single();
        }

        private readonly Action<string> _onError;
    }
}

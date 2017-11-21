using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace CustomUploader
{
    internal class ConfigurationProvider
    {
        internal readonly FileInfo ClientSecret;
        internal readonly string ParentId;
        internal readonly DirectoryInfo Download;
        internal readonly DirectoryInfo Lost;
        internal readonly TimeSpan TimepadLookupTime;
        internal readonly TimeSpan DeviceDateWarningTime;
        internal readonly string[] DeviceFolders;
        internal readonly char FirstDeviceLetter;
        internal readonly int OrganizationId;

        internal ConfigurationProvider(Action<string> onError)
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

            Lost = CheckDirectoryInfoSetting("lostPath", false);
            if (Lost == null)
            {
                return;
            }

            int? timepadHours = CheckIntSetting("timepadHours");
            if (!timepadHours.HasValue)
            {
                return;
            }
            TimepadLookupTime = TimeSpan.FromHours(timepadHours.Value);

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

            int? organizationIdSetting = CheckIntSetting("organizationId");
            if (!organizationIdSetting.HasValue)
            {
                return;
            }
            OrganizationId = organizationIdSetting.Value;
        }

        private string GetSetting(string key)
        {
            string setting = ConfigurationManager.AppSettings.Get(key);
            if (setting == null)
            {
                _onError($"{key} не задан в настройках");
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

            _onError($"Не найден файл {info.FullName}");
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

            _onError($"Не найдена папка {info.FullName}");
            return null;
        }

        private int? CheckIntSetting(string key)
        {
            string setting = GetSetting(key);
            if (setting == null)
            {
                return null;
            }

            int result;
            bool success = int.TryParse(setting, out result);
            if (success)
            {
                return result;
            }

            _onError($"Некорректное значение настройки {key}. Ожидается целое число");
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
                _onError($"Некорректное значение настройки {key}. Ожидается символ");
            }

            return setting.Single();
        }

        private readonly Action<string> _onError;
    }
}

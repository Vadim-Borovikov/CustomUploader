using System;
using System.Linq;
using System.Management;

namespace CustomUploader.Logic
{
    internal class DeviceInsertListener : IDisposable
    {
        public DeviceInsertListener(Action<string> onDriveConnected)
        {
            if (onDriveConnected == null)
            {
                return;
            }

            _watcher = new ManagementEventWatcher
            {
                Query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2")
            };
            _watcher.EventArrived += WatcherOnEventArrived;

            _onDriveConnected = onDriveConnected;
        }

        public void StartWatch()
        {
            _watcher.Start();
        }

        public void StopWatch()
        {
            _watcher.Stop();
        }

        private void WatcherOnEventArrived(object sender, EventArrivedEventArgs e)
        {
            PropertyData driveNameProperty =
                e.NewEvent.Properties.Cast<PropertyData>().FirstOrDefault(p => p.Name == "DriveName");
            string driveName = (string)driveNameProperty?.Value;
            if (!string.IsNullOrWhiteSpace(driveName))
            {
                _onDriveConnected(driveName);
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        private readonly ManagementEventWatcher _watcher;
        private readonly Action<string> _onDriveConnected;
    }
}

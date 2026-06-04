using System.IO;

namespace FMVideoManagerApp.Services
{
    public sealed class LocalDeviceService
    {
        private readonly string _deviceIdFilePath;

        public LocalDeviceService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string directory = Path.Combine(appData, "FMVideoManager");

            Directory.CreateDirectory(directory);

            _deviceIdFilePath = Path.Combine(directory, "device_id.txt");
        }

        public string GetDeviceId()
        {
            if (File.Exists(_deviceIdFilePath))
            {
                string existing = File.ReadAllText(_deviceIdFilePath).Trim();

                if (!string.IsNullOrWhiteSpace(existing))
                    return existing;
            }

            string deviceId = Guid.NewGuid().ToString("N");

            File.WriteAllText(_deviceIdFilePath, deviceId);

            return deviceId;
        }
    }
}
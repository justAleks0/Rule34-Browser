using System.IO;

namespace Rule34Gallery.Core.CloudSync;

public static class SyncDeviceStore
{
    private const string FileName = "sync-device-id.txt";

    public static string GetOrCreateDeviceId(string appDataFolder)
    {
        if (string.IsNullOrWhiteSpace(appDataFolder))
        {
            return Guid.NewGuid().ToString("N");
        }

        Directory.CreateDirectory(appDataFolder);
        var path = Path.Combine(appDataFolder, FileName);
        try
        {
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    return existing;
                }
            }

            var id = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, id);
            return id;
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public static SyncDeviceInfo CreateCurrentDevice(string appDataFolder, string platform, string appVersion)
    {
        var id = GetOrCreateDeviceId(appDataFolder);
        return new SyncDeviceInfo
        {
            DeviceId = id,
            DisplayName = Environment.MachineName,
            Platform = platform,
            AppVersion = appVersion,
            LastSeenAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
    }
}

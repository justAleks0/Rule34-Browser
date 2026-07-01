namespace Rule34GalleryApp.Services;

/// <summary>
/// Returns a safe audio output label for playback UI.
/// Avoids direct CoreAudio COM interop because some systems trigger native AVs in this path.
/// </summary>
internal static class AudioOutputDeviceHelper
{
    private const string FallbackLabel = "System default";

    public static string GetDefaultPlaybackDeviceLabel() => FallbackLabel;
}

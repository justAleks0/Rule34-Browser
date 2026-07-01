namespace Rule34Gallery.Core.Remote;

public static class RemotePairingPin
{
    public static string Generate() => Random.Shared.Next(0, 1_000_000).ToString("D6");

    public static bool IsValidFormat(string? pin) =>
        !string.IsNullOrWhiteSpace(pin) && pin.Trim().Length == 6 && pin.Trim().All(char.IsDigit);
}

using Rule34Gallery.Core.Abstractions;
using Rule34GalleryApp.Firebase;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfGoogleSignInService : IGoogleSignInService
{
    public Task<string> SignInAsync(string clientId, string? clientSecret, CancellationToken cancellationToken = default)
        => GoogleSignInService.SignInAsync(clientId, clientSecret, cancellationToken);

    public string? BuildClientSecretHelp() => GoogleSignInService.BuildClientSecretHelp();
}

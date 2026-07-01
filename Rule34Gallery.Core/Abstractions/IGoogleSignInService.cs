namespace Rule34Gallery.Core.Abstractions;

public interface IGoogleSignInService
{
    Task<string> SignInAsync(string clientId, string? clientSecret, CancellationToken cancellationToken = default);

    string? BuildClientSecretHelp();
}

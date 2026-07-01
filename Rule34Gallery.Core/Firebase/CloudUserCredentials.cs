namespace Rule34Gallery.Core.Firebase;

public sealed class CloudUserCredentials
{
    public string Rule34UserId { get; set; } = string.Empty;

    public string Rule34ApiKey { get; set; } = string.Empty;

    public string DanbooruLogin { get; set; } = string.Empty;

    public string DanbooruApiKey { get; set; } = string.Empty;

    public string E621Username { get; set; } = string.Empty;

    public string E621ApiKey { get; set; } = string.Empty;

    public string OpenAiApiKey { get; set; } = string.Empty;

    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    public bool UseOpenAiForDescribeSearch { get; set; } = true;

    public bool HasRule34Credentials =>
        !string.IsNullOrWhiteSpace(Rule34UserId) && !string.IsNullOrWhiteSpace(Rule34ApiKey);

    public bool HasDanbooruCredentials =>
        !string.IsNullOrWhiteSpace(DanbooruLogin) && !string.IsNullOrWhiteSpace(DanbooruApiKey);

    public bool HasE621Credentials =>
        !string.IsNullOrWhiteSpace(E621Username) && !string.IsNullOrWhiteSpace(E621ApiKey);

    public bool HasOpenAiCredentials => !string.IsNullOrWhiteSpace(OpenAiApiKey);

    public bool HasAny =>
        HasRule34Credentials || HasDanbooruCredentials || HasE621Credentials || HasOpenAiCredentials;

    public static CloudUserCredentials FromSettings(UserSettings settings) => new()
    {
        Rule34UserId = settings.UserId?.Trim() ?? string.Empty,
        Rule34ApiKey = settings.ApiKey ?? string.Empty,
        DanbooruLogin = settings.DanbooruLogin?.Trim() ?? string.Empty,
        DanbooruApiKey = settings.DanbooruApiKey ?? string.Empty,
        E621Username = settings.E621Username?.Trim() ?? string.Empty,
        E621ApiKey = settings.E621ApiKey ?? string.Empty,
        OpenAiApiKey = settings.OpenAiApiKey ?? string.Empty,
        OpenAiModel = string.IsNullOrWhiteSpace(settings.OpenAiModel) ? "gpt-4o-mini" : settings.OpenAiModel.Trim(),
        UseOpenAiForDescribeSearch = settings.UseOpenAiForDescribeSearch,
    };

    public void ApplyTo(UserSettings settings)
    {
        settings.UserId = Rule34UserId.Trim();
        settings.ApiKey = Rule34ApiKey;

        if (!string.IsNullOrWhiteSpace(DanbooruLogin))
        {
            settings.DanbooruLogin = DanbooruLogin.Trim();
        }

        if (!string.IsNullOrWhiteSpace(DanbooruApiKey))
        {
            settings.DanbooruApiKey = DanbooruApiKey;
        }

        if (!string.IsNullOrWhiteSpace(E621Username))
        {
            settings.E621Username = E621Username.Trim();
        }

        if (!string.IsNullOrWhiteSpace(E621ApiKey))
        {
            settings.E621ApiKey = E621ApiKey;
        }

        if (!string.IsNullOrWhiteSpace(OpenAiApiKey))
        {
            settings.OpenAiApiKey = OpenAiApiKey;
        }

        if (!string.IsNullOrWhiteSpace(OpenAiModel))
        {
            settings.OpenAiModel = OpenAiModel.Trim();
        }

        settings.UseOpenAiForDescribeSearch = UseOpenAiForDescribeSearch;
    }

    /// <summary>
    /// Combines credentials from both devices. User ID and API key stay paired per site —
    /// never mix one side from local with the other from cloud.
    /// </summary>
    public static CloudUserCredentials MergeCombine(CloudUserCredentials local, CloudUserCredentials cloud) => new()
    {
        Rule34UserId = MergeCredentialPair(
            local.HasRule34Credentials,
            local.Rule34UserId,
            local.Rule34ApiKey,
            cloud.HasRule34Credentials,
            cloud.Rule34UserId,
            cloud.Rule34ApiKey).Id,
        Rule34ApiKey = MergeCredentialPair(
            local.HasRule34Credentials,
            local.Rule34UserId,
            local.Rule34ApiKey,
            cloud.HasRule34Credentials,
            cloud.Rule34UserId,
            cloud.Rule34ApiKey).Secret,
        DanbooruLogin = MergeCredentialPair(
            local.HasDanbooruCredentials,
            local.DanbooruLogin,
            local.DanbooruApiKey,
            cloud.HasDanbooruCredentials,
            cloud.DanbooruLogin,
            cloud.DanbooruApiKey).Id,
        DanbooruApiKey = MergeCredentialPair(
            local.HasDanbooruCredentials,
            local.DanbooruLogin,
            local.DanbooruApiKey,
            cloud.HasDanbooruCredentials,
            cloud.DanbooruLogin,
            cloud.DanbooruApiKey).Secret,
        E621Username = MergeCredentialPair(
            local.HasE621Credentials,
            local.E621Username,
            local.E621ApiKey,
            cloud.HasE621Credentials,
            cloud.E621Username,
            cloud.E621ApiKey).Id,
        E621ApiKey = MergeCredentialPair(
            local.HasE621Credentials,
            local.E621Username,
            local.E621ApiKey,
            cloud.HasE621Credentials,
            cloud.E621Username,
            cloud.E621ApiKey).Secret,
        OpenAiApiKey = PreferNonEmpty(local.OpenAiApiKey, cloud.OpenAiApiKey),
        OpenAiModel = PreferNonEmpty(local.OpenAiModel, cloud.OpenAiModel),
        UseOpenAiForDescribeSearch = local.UseOpenAiForDescribeSearch || cloud.UseOpenAiForDescribeSearch,
    };

    private static (string Id, string Secret) MergeCredentialPair(
        bool localHasPair,
        string localId,
        string localSecret,
        bool cloudHasPair,
        string cloudId,
        string cloudSecret)
    {
        if (localHasPair)
        {
            return (localId.Trim(), localSecret);
        }

        if (cloudHasPair)
        {
            return (cloudId.Trim(), cloudSecret);
        }

        return (PreferNonEmpty(localId, cloudId), PreferNonEmpty(localSecret, cloudSecret));
    }

    public static CloudUserCredentials MergePreferLocal(CloudUserCredentials local, CloudUserCredentials cloud)
        => MergeCombine(local, cloud);

    /// <summary>Push local Rule34 credentials as-is (including cleared). Other fields still merge.</summary>
    public static CloudUserCredentials MergeForCloudUpload(CloudUserCredentials local, CloudUserCredentials? cloud)
    {
        var merged = cloud is null ? local : MergeCombine(local, cloud);
        merged.Rule34UserId = local.Rule34UserId;
        merged.Rule34ApiKey = local.Rule34ApiKey;
        return merged;
    }

    private static string PreferNonEmpty(string preferred, string fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : fallback;
}

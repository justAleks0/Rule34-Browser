using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rule34Gallery.Core.CloudSync;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.Firebase;

public sealed class FirestoreService
{
    private static readonly JsonSerializerOptions ForYouCloudJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FirebaseConfig _config;
    private readonly FirebaseAuthService _auth;
    private readonly HttpClient _http = new();

    public FirestoreService(FirebaseConfig config, FirebaseAuthService auth)
    {
        _config = config;
        _auth = auth;
    }

    public async Task<IReadOnlyList<PostItem>> GetFavoritesAsync()
    {
        var path = $"users/{UserId()}/favorites";
        var docs = await ListDocumentsAsync(path).ConfigureAwait(false);
        return docs.Select(PostFromDocument).Where(p => p.Id > 0).ToList();
    }

    public async Task SetFavoriteAsync(PostItem post, bool isFavorite)
    {
        var path = $"users/{UserId()}/favorites/{post.Id}";
        if (isFavorite)
        {
            await PatchDocumentAsync(path, PostToFields(post)).ConfigureAwait(false);
        }
        else
        {
            await DeleteDocumentAsync(path).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<SavedList>> GetListsAsync()
    {
        var path = $"users/{UserId()}/lists";
        var docs = await ListDocumentsAsync(path).ConfigureAwait(false);
        return docs.Select(ListFromDocument).Where(l => !string.IsNullOrWhiteSpace(l.Id)).ToList();
    }

    public async Task<SavedList> EnsureWatchLaterListAsync()
    {
        var existing = (await GetListsAsync().ConfigureAwait(false))
            .FirstOrDefault(l => l.Id == SavedList.WatchLaterId);
        if (existing is not null)
        {
            return existing;
        }

        var path = $"users/{UserId()}/lists/{SavedList.WatchLaterId}";
        var fields = new Dictionary<string, object?>
        {
            ["name"] = SavedList.WatchLaterName,
            ["isSystem"] = true,
            ["createdAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        await PatchDocumentAsync(path, fields).ConfigureAwait(false);
        return SavedList.WatchLater();
    }

    public async Task<SavedList> CreateListAsync(string name)
    {
        if (string.Equals(name.Trim(), SavedList.WatchLaterName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"\"{SavedList.WatchLaterName}\" is reserved.");
        }

        var listId = Guid.NewGuid().ToString("N");
        var path = $"users/{UserId()}/lists/{listId}";
        var fields = new Dictionary<string, object?>
        {
            ["name"] = name.Trim(),
            ["createdAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        await PatchDocumentAsync(path, fields).ConfigureAwait(false);
        return new SavedList { Id = listId, Name = name.Trim() };
    }

    public async Task DeleteListAsync(string listId)
    {
        if (string.Equals(listId, SavedList.WatchLaterId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"\"{SavedList.WatchLaterName}\" cannot be deleted.");
        }

        var posts = await GetListPostsAsync(listId).ConfigureAwait(false);
        foreach (var post in posts)
        {
            await RemovePostFromListAsync(listId, post.Id).ConfigureAwait(false);
        }

        await DeleteDocumentAsync($"users/{UserId()}/lists/{listId}").ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PostItem>> GetListPostsAsync(string listId)
    {
        var path = $"users/{UserId()}/lists/{listId}/posts";
        var docs = await ListDocumentsAsync(path).ConfigureAwait(false);
        return docs.Select(PostFromDocument).Where(p => p.Id > 0).ToList();
    }

    public async Task AddPostToListAsync(string listId, PostItem post)
    {
        var path = $"users/{UserId()}/lists/{listId}/posts/{post.Id}";
        await PatchDocumentAsync(path, PostToFields(post)).ConfigureAwait(false);
    }

    public async Task RemovePostFromListAsync(string listId, int postId)
    {
        await DeleteDocumentAsync($"users/{UserId()}/lists/{listId}/posts/{postId}").ConfigureAwait(false);
    }

    public async Task<CloudUserCredentials?> GetCredentialsAsync()
    {
        var document = await GetDocumentAsync($"users/{UserId()}/private/credentials").ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        if (!document.TryGetPropertyValue("fields", out var fieldsNode) || fieldsNode is not JsonObject fields)
        {
            return null;
        }

        return new CloudUserCredentials
        {
            Rule34UserId = ReadString(fields, "rule34UserId"),
            Rule34ApiKey = ReadString(fields, "rule34ApiKey"),
            DanbooruLogin = ReadString(fields, "danbooruLogin"),
            DanbooruApiKey = ReadString(fields, "danbooruApiKey"),
            E621Username = ReadString(fields, "e621Username"),
            E621ApiKey = ReadString(fields, "e621ApiKey"),
            OpenAiApiKey = ReadString(fields, "openAiApiKey"),
            OpenAiModel = ReadString(fields, "openAiModel"),
            UseOpenAiForDescribeSearch = ReadBool(fields, "useOpenAiForDescribeSearch", defaultValue: true),
        };
    }

    public async Task<IReadOnlyList<SavedTagPreset>> GetSavedTagPresetsAsync()
    {
        var path = $"users/{UserId()}/savedTagPresets";
        var docs = await ListDocumentsAsync(path).ConfigureAwait(false);
        return docs.Select(SavedTagPresetFromDocument).Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToList();
    }

    public async Task SyncSavedTagPresetsAsync(IReadOnlyList<SavedTagPreset> presets)
    {
        var collectionPath = $"users/{UserId()}/savedTagPresets";
        var existing = await ListDocumentsAsync(collectionPath).ConfigureAwait(false);
        var desiredIds = presets
            .Select(p => p.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var document in existing)
        {
            var documentName = document["name"]?.GetValue<string>() ?? string.Empty;
            var documentId = documentName.Split('/').LastOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(documentId) && !desiredIds.Contains(documentId))
            {
                await DeleteDocumentAsync($"{collectionPath}/{documentId}").ConfigureAwait(false);
            }
        }

        foreach (var preset in presets)
        {
            if (string.IsNullOrWhiteSpace(preset.Id))
            {
                continue;
            }

            var fields = new Dictionary<string, object?>
            {
                ["name"] = preset.Name.Trim(),
                ["tagsJson"] = SavedTagPresetSync.SerializeTags(preset.Tags),
                ["updatedAt"] = preset.UpdatedAtUnix > 0
                    ? preset.UpdatedAtUnix
                    : SavedTagPresetSync.NowUnix(),
            };

            await PatchDocumentAsync($"{collectionPath}/{preset.Id}", fields).ConfigureAwait(false);
        }
    }

    public async Task<ForYouCloudProfile?> GetForYouProfileAsync()
    {
        var document = await GetDocumentAsync($"users/{UserId()}/private/forYouProfile").ConfigureAwait(false);
        if (document is null)
        {
            return null;
        }

        if (!document.TryGetPropertyValue("fields", out var fieldsNode) || fieldsNode is not JsonObject fields)
        {
            return null;
        }

        var json = ReadString(fields, "profileJson");
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ForYouCloudProfile>(json, ForYouCloudJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetForYouProfileAsync(ForYouCloudProfile profile)
    {
        var fields = new Dictionary<string, object?>
        {
            ["profileJson"] = JsonSerializer.Serialize(profile, ForYouCloudJsonOptions),
            ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        await PatchDocumentAsync($"users/{UserId()}/private/forYouProfile", fields).ConfigureAwait(false);
    }

    public Task DeleteForYouProfileAsync()
        => DeleteDocumentAsync($"users/{UserId()}/private/forYouProfile");

    public async Task<FirestoreSyncMeta?> GetSyncMetaAsync()
    {
        var document = await GetDocumentAsync($"users/{UserId()}/private/syncMeta").ConfigureAwait(false);
        if (document is null ||
            !document.TryGetPropertyValue("fields", out var fieldsNode) ||
            fieldsNode is not JsonObject fields)
        {
            return null;
        }

        return new FirestoreSyncMeta
        {
            LastFullSyncAtUnix = ReadLong(fields, "lastFullSyncAt"),
            LastSyncDeviceId = ReadString(fields, "lastSyncDeviceId"),
            LastSyncDirection = ReadString(fields, "lastSyncDirection"),
            LastSyncStatus = ReadString(fields, "lastSyncStatus"),
        };
    }

    public Task SetSyncMetaAsync(FirestoreSyncMeta meta)
    {
        var fields = new Dictionary<string, object?>
        {
            ["lastFullSyncAt"] = meta.LastFullSyncAtUnix ?? 0L,
            ["lastSyncDeviceId"] = meta.LastSyncDeviceId,
            ["lastSyncDirection"] = meta.LastSyncDirection,
            ["lastSyncStatus"] = meta.LastSyncStatus,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        return PatchDocumentAsync($"users/{UserId()}/private/syncMeta", fields);
    }

    public async Task<SyncDeviceInfo?> GetDeviceAsync(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        var document = await GetDocumentAsync($"users/{UserId()}/devices/{deviceId}").ConfigureAwait(false);
        if (document is null ||
            !document.TryGetPropertyValue("fields", out var fieldsNode) ||
            fieldsNode is not JsonObject fields)
        {
            return null;
        }

        return DeviceFromFields(deviceId, fields);
    }

    public Task SetDeviceAsync(SyncDeviceInfo device)
    {
        var fields = new Dictionary<string, object?>
        {
            ["displayName"] = device.DisplayName,
            ["platform"] = device.Platform,
            ["appVersion"] = device.AppVersion,
            ["lastSeenAt"] = device.LastSeenAtUnix,
            ["lastSyncAt"] = device.LastSyncAtUnix,
        };
        return PatchDocumentAsync($"users/{UserId()}/devices/{device.DeviceId}", fields);
    }

    public async Task DeleteSavedTagPresetAsync(string presetId)
    {
        if (string.IsNullOrWhiteSpace(presetId))
        {
            return;
        }

        await DeleteDocumentAsync($"users/{UserId()}/savedTagPresets/{presetId}").ConfigureAwait(false);
    }

    private static SyncDeviceInfo DeviceFromFields(string deviceId, JsonObject fields) => new()
    {
        DeviceId = deviceId,
        DisplayName = ReadString(fields, "displayName"),
        Platform = ReadString(fields, "platform"),
        AppVersion = ReadString(fields, "appVersion"),
        LastSeenAtUnix = ReadLong(fields, "lastSeenAt"),
        LastSyncAtUnix = ReadLong(fields, "lastSyncAt"),
    };

    public async Task SetCredentialsAsync(CloudUserCredentials credentials)
    {
        var fields = new Dictionary<string, object?>
        {
            ["rule34UserId"] = credentials.Rule34UserId.Trim(),
            ["rule34ApiKey"] = credentials.Rule34ApiKey,
            ["danbooruLogin"] = credentials.DanbooruLogin.Trim(),
            ["danbooruApiKey"] = credentials.DanbooruApiKey,
            ["e621Username"] = credentials.E621Username.Trim(),
            ["e621ApiKey"] = credentials.E621ApiKey,
            ["openAiApiKey"] = credentials.OpenAiApiKey,
            ["openAiModel"] = string.IsNullOrWhiteSpace(credentials.OpenAiModel)
                ? "gpt-4o-mini"
                : credentials.OpenAiModel.Trim(),
            ["useOpenAiForDescribeSearch"] = credentials.UseOpenAiForDescribeSearch,
            ["updatedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        await PatchDocumentAsync($"users/{UserId()}/private/credentials", fields).ConfigureAwait(false);
    }

    private string UserId() =>
        _auth.CurrentUser?.UserId ?? throw new InvalidOperationException("Not signed in.");

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string documentPath, HttpContent? content = null)
    {
        var token = await _auth.GetIdTokenAsync().ConfigureAwait(false);
        var url =
            $"https://firestore.googleapis.com/v1/projects/{_config.ProjectId}/databases/(default)/documents/{documentPath}";
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }

    private async Task PatchDocumentAsync(string documentPath, Dictionary<string, object?> fields)
    {
        var mask = string.Join(
            "&",
            fields.Keys.Select(key => $"updateMask.fieldPaths={Uri.EscapeDataString(key)}"));
        var path = string.IsNullOrEmpty(mask) ? documentPath : $"{documentPath}?{mask}";

        var body = new JsonObject
        {
            ["fields"] = FieldsToJson(fields),
        };

        var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var request = await CreateRequestAsync(HttpMethod.Patch, path, content).ConfigureAwait(false);
        var response = await _http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteDocumentAsync(string documentPath)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, documentPath).ConfigureAwait(false);
        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonObject?> GetDocumentAsync(string documentPath)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, documentPath).ConfigureAwait(false);
        var response = await _http.SendAsync(request).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<JsonObject>> ListDocumentsAsync(string collectionPath)
    {
        var url =
            $"https://firestore.googleapis.com/v1/projects/{_config.ProjectId}/databases/(default)/documents/{collectionPath}";
        var token = await _auth.GetIdTokenAsync().ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonObject>().ConfigureAwait(false);
        if (json is null || !json.TryGetPropertyValue("documents", out var documentsNode) || documentsNode is not JsonArray documents)
        {
            return [];
        }

        return documents.OfType<JsonObject>().ToList();
    }

    private static Dictionary<string, object?> PostToFields(PostItem post) => new()
    {
        ["id"] = post.Id,
        ["previewUrl"] = post.PreviewUrl,
        ["sampleUrl"] = post.SampleUrl,
        ["fileUrl"] = post.FileUrl,
        ["rating"] = post.Rating,
        ["tags"] = post.Tags,
        ["tagInfoJson"] = SerializeTagInfo(post.TagInfo),
        ["artist"] = JoinTagsByCategory(post, TagCategory.Artist),
        ["copyright"] = JoinTagsByCategory(post, TagCategory.Copyright),
        ["character"] = JoinTagsByCategory(post, TagCategory.Character),
        ["score"] = post.Score,
        ["width"] = post.Width,
        ["height"] = post.Height,
        ["owner"] = post.Owner,
        ["savedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    };

    private static PostItem PostFromDocument(JsonObject document)
    {
        if (!document.TryGetPropertyValue("fields", out var fieldsNode) || fieldsNode is not JsonObject fields)
        {
            return new PostItem();
        }

        var tagInfo = DeserializeTagInfo(ReadString(fields, "tagInfoJson"));
        tagInfo ??= RebuildTagInfoFromFields(fields);

        return new PostItem
        {
            Id = ReadInt(fields, "id"),
            PreviewUrl = ReadString(fields, "previewUrl"),
            SampleUrl = ReadString(fields, "sampleUrl"),
            FileUrl = ReadString(fields, "fileUrl"),
            Rating = ReadString(fields, "rating"),
            Tags = ReadString(fields, "tags"),
            TagInfo = tagInfo,
            Score = ReadInt(fields, "score"),
            Width = ReadInt(fields, "width"),
            Height = ReadInt(fields, "height"),
            Owner = ReadString(fields, "owner"),
        };
    }

    private static List<PostTagInfo>? RebuildTagInfoFromFields(JsonObject fields)
    {
        var list = new List<PostTagInfo>();
        AddTypedTags(list, ReadString(fields, "artist"), "artist");
        AddTypedTags(list, ReadString(fields, "copyright"), "copyright");
        AddTypedTags(list, ReadString(fields, "character"), "character");
        return list.Count == 0 ? null : list;
    }

    private static void AddTypedTags(List<PostTagInfo> list, string csv, string type)
    {
        foreach (var tag in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            list.Add(new PostTagInfo { Tag = tag, Type = type });
        }
    }

    private static string JoinTagsByCategory(PostItem post, TagCategory category)
    {
        var map = post.GetTagCategoryMap();
        return string.Join(
            ", ",
            post.GetTagList().Where(tag => map.TryGetValue(tag, out var cat) && cat == category));
    }

    private static string SerializeTagInfo(IReadOnlyList<PostTagInfo>? tagInfo) =>
        tagInfo is not { Count: > 0 }
            ? string.Empty
            : JsonSerializer.Serialize(tagInfo);

    private static List<PostTagInfo>? DeserializeTagInfo(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<PostTagInfo>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static SavedTagPreset SavedTagPresetFromDocument(JsonObject document)
    {
        var documentName = document["name"]?.GetValue<string>() ?? string.Empty;
        var id = documentName.Split('/').LastOrDefault() ?? string.Empty;
        if (!document.TryGetPropertyValue("fields", out var fieldsNode) || fieldsNode is not JsonObject fields)
        {
            return new SavedTagPreset { Id = id };
        }

        var tags = SavedTagPresetSync.DeserializeTags(ReadString(fields, "tagsJson"));
        return new SavedTagPreset
        {
            Id = id,
            Name = ReadString(fields, "name"),
            Tags = tags,
            UpdatedAtUnix = ReadLong(fields, "updatedAt"),
        };
    }

    private static SavedList ListFromDocument(JsonObject document)
    {
        var name = document["name"]?.GetValue<string>() ?? string.Empty;
        var id = name.Split('/').LastOrDefault() ?? string.Empty;
        var fields = document["fields"] as JsonObject;
        var listName = fields is null ? id : ReadString(fields, "name");
        if (string.IsNullOrWhiteSpace(listName) && id == SavedList.WatchLaterId)
        {
            listName = SavedList.WatchLaterName;
        }

        var isSystem = id == SavedList.WatchLaterId ||
            (fields is not null && ReadBool(fields, "isSystem"));
        return new SavedList
        {
            Id = id,
            Name = listName,
            IsSystem = isSystem,
        };
    }

    private static JsonObject FieldsToJson(Dictionary<string, object?> fields)
    {
        var result = new JsonObject();
        foreach (var (key, value) in fields)
        {
            result[key] = ValueToJson(value);
        }

        return result;
    }

    private static JsonNode? ValueToJson(object? value) => value switch
    {
        null => null,
        string s => new JsonObject { ["stringValue"] = s },
        int i => new JsonObject { ["integerValue"] = i.ToString() },
        long l => new JsonObject { ["integerValue"] = l.ToString() },
        bool b => new JsonObject { ["booleanValue"] = b },
        _ => new JsonObject { ["stringValue"] = value.ToString() },
    };

    private static string ReadString(JsonObject fields, string name)
    {
        if (!fields.TryGetPropertyValue(name, out var node) || node is not JsonObject wrapper)
        {
            return string.Empty;
        }

        return wrapper["stringValue"]?.GetValue<string>() ?? string.Empty;
    }

    private static long ReadLong(JsonObject fields, string name)
    {
        if (!fields.TryGetPropertyValue(name, out var node) || node is not JsonObject wrapper)
        {
            return 0;
        }

        var text = wrapper["integerValue"]?.GetValue<string>();
        return long.TryParse(text, out var value) ? value : 0;
    }

    private static int ReadInt(JsonObject fields, string name)
    {
        if (!fields.TryGetPropertyValue(name, out var node) || node is not JsonObject wrapper)
        {
            return 0;
        }

        var text = wrapper["integerValue"]?.GetValue<string>();
        return int.TryParse(text, out var value) ? value : 0;
    }

    private static bool ReadBool(JsonObject fields, string name, bool defaultValue = false)
    {
        if (!fields.TryGetPropertyValue(name, out var node) || node is not JsonObject wrapper)
        {
            return defaultValue;
        }

        return wrapper["booleanValue"]?.GetValue<bool>() ?? defaultValue;
    }
}

public sealed class SavedList
{
    public const string WatchLaterId = "watch_later";

    public const string WatchLaterName = "Watch Later";

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsSystem { get; set; }

    public static SavedList WatchLater() => new()
    {
        Id = WatchLaterId,
        Name = WatchLaterName,
        IsSystem = true,
    };

    public override string ToString() => Name;
}

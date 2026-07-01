namespace Rule34Gallery.Core.Abstractions;

public interface ISettingsStore
{
    UserSettings? Load();

    void Save(UserSettings settings);
}

namespace Rule34Gallery.Core.Services;

public static class ForYouTopicOrigin
{
    public static bool IsManual(ForYouTopicProfile topic) => topic.IsManuallyCurated;
}

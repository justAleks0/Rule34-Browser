namespace Rule34Gallery.Core.Services;

public static partial class SearchPresetCatalog
{
    private static readonly SearchPreset[] _basePresets =
    [
        new()
        {
            Id = "solo_female",
            Name = "Solo female",
            Description = "Single female character",
            Tags = ["1girl", "solo"],
        },
        new()
        {
            Id = "solo_male",
            Name = "Solo male",
            Description = "Single male character",
            Tags = ["1boy", "solo"],
        },
        new()
        {
            Id = "lesbian",
            Name = "Lesbian / yuri",
            Description = "Two or more girls together",
            Tags = ["yuri", "2girls"],
        },
        new()
        {
            Id = "hetero",
            Name = "Straight pair",
            Description = "One girl and one boy",
            Tags = ["1girl", "1boy", "hetero"],
        },
        new()
        {
            Id = "threesome_ffm",
            Name = "FFM threesome",
            Description = "One male, two females",
            Tags = ["1boy", "2girls", "threesome"],
        },
        new()
        {
            Id = "group",
            Name = "Group",
            Description = "Three or more participants",
            Tags = ["group", "3girls"],
        },
        new()
        {
            Id = "animated",
            Name = "Animated",
            Description = "Animated posts (incl. GIF-style)",
            Tags = ["animated"],
        },
        new()
        {
            Id = "gif",
            Name = "GIF",
            Description = "GIF format",
            Tags = ["gif"],
        },
        new()
        {
            Id = "video",
            Name = "Video",
            Description = "Video posts",
            Tags = ["video"],
        },
        new()
        {
            Id = "comic",
            Name = "Comic",
            Description = "Multi-page or comic layout",
            Tags = ["comic"],
        },
        new()
        {
            Id = "3d",
            Name = "3D render",
            Description = "3D artwork",
            Tags = ["3d"],
        },
        new()
        {
            Id = "highres",
            Name = "High resolution",
            Description = "High-res images",
            Tags = ["highres"],
        },
        new()
        {
            Id = "english",
            Name = "English",
            Description = "English or translated text",
            Tags = ["english", "translated"],
        },
        new()
        {
            Id = "cosplay",
            Name = "Cosplay",
            Description = "Cosplay content",
            Tags = ["cosplay"],
        },
        new()
        {
            Id = "huge_breasts",
            Name = "Huge breasts",
            Description = "Large chest focus",
            Tags = ["huge_breasts"],
        },
        new()
        {
            Id = "flat_chest",
            Name = "Flat chest",
            Description = "Small / flat chest",
            Tags = ["flat_chest"],
        },
        new()
        {
            Id = "tentacles",
            Name = "Tentacles",
            Description = "Tentacle content (include)",
            Tags = ["tentacles"],
        },
        new()
        {
            Id = "outdoors",
            Name = "Outdoors",
            Description = "Outdoor setting",
            Tags = ["outdoors"],
        },
        new()
        {
            Id = "monster_girl",
            Name = "Monster girl",
            Description = "Monstergirl characters",
            Tags = ["monstergirl"],
        },
        new()
        {
            Id = "pokemon",
            Name = "Pokémon",
            Description = "Pokémon franchise",
            Tags = ["pokemon"],
        },
        new()
        {
            Id = "genshin",
            Name = "Genshin Impact",
            Description = "Genshin Impact",
            Tags = ["genshin_impact"],
        },
        new()
        {
            Id = "overwatch",
            Name = "Overwatch",
            Description = "Overwatch franchise",
            Tags = ["overwatch"],
        },
        new()
        {
            Id = "marvel",
            Name = "Marvel",
            Description = "Marvel characters",
            Tags = ["marvel"],
        },
        new()
        {
            Id = "nintendo",
            Name = "Nintendo",
            Description = "Nintendo games / characters",
            Tags = ["nintendo"],
        },
        new()
        {
            Id = "zelda",
            Name = "The Legend of Zelda",
            Description = "Zelda series",
            Tags = ["the_legend_of_zelda"],
        },
        new()
        {
            Id = "league",
            Name = "League of Legends",
            Description = "LoL champions",
            Tags = ["league_of_legends"],
        },
        new()
        {
            Id = "fortnite",
            Name = "Fortnite",
            Description = "Fortnite",
            Tags = ["fortnite"],
        },
        new()
        {
            Id = "minecraft",
            Name = "Minecraft",
            Description = "Minecraft",
            Tags = ["minecraft"],
        },
        new()
        {
            Id = "disney",
            Name = "Disney",
            Description = "Disney characters",
            Tags = ["disney"],
        },
        new()
        {
            Id = "star_wars",
            Name = "Star Wars",
            Description = "Star Wars",
            Tags = ["star_wars"],
        },
        new()
        {
            Id = "harry_potter",
            Name = "Harry Potter",
            Description = "Harry Potter",
            Tags = ["harry_potter"],
        },
        new()
        {
            Id = "hololive",
            Name = "Hololive / VTuber",
            Description = "VTubers",
            Tags = ["hololive", "vtuber"],
        },
        new()
        {
            Id = "fate",
            Name = "Fate series",
            Description = "Fate/stay night and related",
            Tags = ["fate_(series)"],
        },
        new()
        {
            Id = "touhou",
            Name = "Touhou",
            Description = "Touhou Project",
            Tags = ["touhou"],
        },
        new()
        {
            Id = "kantai",
            Name = "Kantai Collection",
            Description = "KanColle",
            Tags = ["kantai_collection"],
        },
        new()
        {
            Id = "azur_lane",
            Name = "Azur Lane",
            Description = "Azur Lane",
            Tags = ["azur_lane"],
        },
        new()
        {
            Id = "fire_emblem",
            Name = "Fire Emblem",
            Description = "Fire Emblem",
            Tags = ["fire_emblem"],
        },
        new()
        {
            Id = "street_fighter",
            Name = "Street Fighter",
            Description = "Street Fighter",
            Tags = ["street_fighter"],
        },
        new()
        {
            Id = "tekken",
            Name = "Tekken",
            Description = "Tekken",
            Tags = ["tekken"],
        },
        new()
        {
            Id = "final_fantasy",
            Name = "Final Fantasy",
            Description = "Final Fantasy",
            Tags = ["final_fantasy"],
        },
        new()
        {
            Id = "resident_evil",
            Name = "Resident Evil",
            Description = "Resident Evil / Biohazard",
            Tags = ["resident_evil"],
        },
        new()
        {
            Id = "sonic",
            Name = "Sonic",
            Description = "Sonic the Hedgehog",
            Tags = ["sonic_(series)"],
        },
        new()
        {
            Id = "undertale",
            Name = "Undertale / Deltarune",
            Description = "Toby Fox games",
            Tags = ["undertale"],
        },
        new()
        {
            Id = "bikini",
            Name = "Bikini",
            Description = "Bikini / swimsuit",
            Tags = ["bikini", "swimsuit"],
        },
        new()
        {
            Id = "school_uniform",
            Name = "School uniform",
            Description = "School outfit",
            Tags = ["school_uniform"],
        },
        new()
        {
            Id = "maid",
            Name = "Maid",
            Description = "Maid outfit",
            Tags = ["maid"],
        },
        new()
        {
            Id = "nurse",
            Name = "Nurse",
            Description = "Nurse outfit",
            Tags = ["nurse"],
        },
        new()
        {
            Id = "catgirl",
            Name = "Catgirl",
            Description = "Cat ears / nekomimi",
            Tags = ["catgirl", "cat_ears"],
        },
        new()
        {
            Id = "foxgirl",
            Name = "Foxgirl",
            Description = "Fox ears / kitsune",
            Tags = ["foxgirl", "kitsune"],
        },
        new()
        {
            Id = "demon_girl",
            Name = "Demon girl",
            Description = "Female demon characters",
            Tags = ["demon_girl"],
        },
        new()
        {
            Id = "elf",
            Name = "Elf",
            Description = "Elf characters",
            Tags = ["elf"],
        },
        new()
        {
            Id = "thighhighs",
            Name = "Thighhighs",
            Description = "Thigh-high stockings",
            Tags = ["thighhighs"],
        },
        new()
        {
            Id = "glasses",
            Name = "Glasses",
            Description = "Glasses / megane",
            Tags = ["glasses"],
        },
        new()
        {
            Id = "dark_skin",
            Name = "Dark skin",
            Description = "Dark-skinned characters",
            Tags = ["dark_skin"],
        },
        new()
        {
            Id = "muscular_female",
            Name = "Muscular female",
            Description = "Fit / muscular women",
            Tags = ["muscular_female", "abs"],
        },
        new()
        {
            Id = "blowjob",
            Name = "Blowjob",
            Description = "Oral on male",
            Tags = ["blowjob", "fellatio"],
        },
        new()
        {
            Id = "paizuri",
            Name = "Paizuri",
            Description = "Breast sex",
            Tags = ["paizuri"],
        },
        new()
        {
            Id = "anal",
            Name = "Anal",
            Description = "Anal sex",
            Tags = ["anal"],
        },
        new()
        {
            Id = "kiss",
            Name = "Kissing",
            Description = "Kiss-focused",
            Tags = ["kiss", "kissing"],
        },
        new()
        {
            Id = "uncensored",
            Name = "Uncensored",
            Description = "No mosaic censorship",
            Tags = ["uncensored"],
        },
        new()
        {
            Id = "western_art",
            Name = "Western art",
            Description = "Western-style art",
            Tags = ["western_art"],
        },
        new()
        {
            Id = "pixel_art",
            Name = "Pixel art",
            Description = "Pixel / retro style",
            Tags = ["pixel_art"],
        },
        new()
        {
            Id = "sketch",
            Name = "Sketch",
            Description = "Sketch / lineart",
            Tags = ["sketch"],
        },
        new()
        {
            Id = "mmf",
            Name = "MMF threesome",
            Description = "Two males, one female",
            Tags = ["2boys", "1girl", "threesome"],
        },
        new()
        {
            Id = "fff",
            Name = "FFF / yuri group",
            Description = "Three or more girls",
            Tags = ["yuri", "3girls"],
        },
    ];

    public static IReadOnlyList<SearchPreset> All { get; }

    static SearchPresetCatalog()
    {
        All = [.._basePresets, ..ExtendedPresets];
    }

    public static SearchPreset? Find(string id)
        => All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> GetTagsForPresets(IEnumerable<string> presetIds, UserSettings? settings = null)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in presetIds)
        {
            var preset = Find(id);
            if (preset is null && settings?.FindSavedSearchPreset(id) is { } saved)
            {
                preset = UserSettings.ToSearchPreset(saved);
            }
            if (preset is null)
            {
                continue;
            }

            foreach (var tag in preset.Tags)
            {
                var normalized = UserSettings.NormalizeTagToken(tag);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    seen.Add(normalized);
                }
            }
        }

        return seen;
    }
}

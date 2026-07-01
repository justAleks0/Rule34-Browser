namespace Rule34Gallery.Core.Services;

public static partial class BlacklistPresetCatalog
{
    private static readonly BlacklistPreset[] _basePresets =
    [
        new()
        {
            Id = "furry",
            Name = "Furry / anthro",
            Description = "Anthropomorphic and animal-human content",
            Tags =
            [
                "furry",
                "anthro",
                "anthrofied",
                "humanoid",
                "scalie",
                "mammal",
                "canid",
                "feline",
                "avian",
                "reptile",
                "equine",
                "rodent",
            ],
        },
        new()
        {
            Id = "ai",
            Name = "AI art",
            Description = "AI-generated or AI-assisted posts",
            Tags =
            [
                "ai_generated",
                "ai_assisted",
                "artificial_intelligence",
                "stable_diffusion",
                "midjourney",
                "novelai",
            ],
        },
        new()
        {
            Id = "futa",
            Name = "Futanari",
            Description = "Futa / herm content",
            Tags =
            [
                "futanari",
                "futa",
                "herm",
                "dickgirl",
                "full-package_futa",
            ],
        },
        new()
        {
            Id = "male",
            Name = "Male / yaoi",
            Description = "Male-only and yaoi-focused posts",
            Tags =
            [
                "yaoi",
                "males_only",
                "male_only",
                "male_focus",
                "male_penetrating_male",
                "bara",
            ],
        },
        new()
        {
            Id = "hyper",
            Name = "Hyper",
            Description = "Extreme size / hyper body features",
            Tags =
            [
                "hyper",
                "hyper_penis",
                "hyper_breasts",
                "absurdly_large_penis",
                "absurdly_large_breasts",
                "size_difference",
            ],
        },
        new()
        {
            Id = "scat",
            Name = "Scat / gross",
            Description = "Scat, urine, and related fetishes",
            Tags =
            [
                "scat",
                "feces",
                "urine",
                "watersports",
                "pee",
                "fart",
                "vomit",
                "diaper",
            ],
        },
        new()
        {
            Id = "gore",
            Name = "Gore",
            Description = "Blood, guro, and extreme violence",
            Tags =
            [
                "gore",
                "blood",
                "guro",
                "death",
                "snuff",
                "amputee",
            ],
        },
        new()
        {
            Id = "vore",
            Name = "Vore",
            Description = "Vore and unbirth content",
            Tags =
            [
                "vore",
                "unbirth",
                "cock_vore",
                "oral_vore",
                "anal_vore",
            ],
        },
        new()
        {
            Id = "incest",
            Name = "Incest",
            Description = "Incest-related tags",
            Tags = ["incest"],
        },
        new()
        {
            Id = "tf",
            Name = "Transformation",
            Description = "TF and species/gender transformation",
            Tags =
            [
                "transformation",
                "tf",
                "gender_transformation",
                "species_transformation",
                "morph",
            ],
        },
        new()
        {
            Id = "feet",
            Name = "Feet",
            Description = "Foot fetish focused tags",
            Tags =
            [
                "feet",
                "foot_fetish",
                "footjob",
                "smelly_feet",
                "socks",
            ],
        },
        new()
        {
            Id = "pregnant",
            Name = "Pregnant",
            Description = "Pregnancy and birth-related tags",
            Tags =
            [
                "pregnant",
                "impregnation",
                "birth",
                "egg_laying",
                "oviposition",
            ],
        },
        new()
        {
            Id = "loli_shota",
            Name = "Loli / shota",
            Description = "Young-looking or underage-coded characters",
            Tags =
            [
                "loli",
                "shota",
                "lolicon",
                "shotacon",
                "young",
                "underage",
                "cub",
            ],
        },
        new()
        {
            Id = "noncon",
            Name = "Non-con / rape",
            Description = "Non-consensual and forced content",
            Tags =
            [
                "rape",
                "non-con",
                "forced",
                "molestation",
                "gangbang_rape",
            ],
        },
        new()
        {
            Id = "bestiality",
            Name = "Bestiality",
            Description = "Human–animal sexual content",
            Tags =
            [
                "bestiality",
                "zoophilia",
                "animal_penetration",
                "animal_on_human",
                "human_on_animal",
            ],
        },
        new()
        {
            Id = "tentacles",
            Name = "Tentacles",
            Description = "Tentacle and appendage-focused content",
            Tags =
            [
                "tentacles",
                "tentacle_sex",
                "consensual_tentacles",
                "tentacle_penetration",
                "tentacle_gagged",
            ],
        },
        new()
        {
            Id = "bondage",
            Name = "Bondage / BDSM",
            Description = "Restraint, pain play, and domination tags",
            Tags =
            [
                "bondage",
                "bdsm",
                "gag",
                "gagged",
                "shibari",
                "spanking",
                "whip",
                "collar",
                "leash",
            ],
        },
        new()
        {
            Id = "femboy",
            Name = "Femboy / trap",
            Description = "Feminine male and crossdressing tags",
            Tags =
            [
                "femboy",
                "trap",
                "crossdressing",
                "otoko_no_ko",
                "josou",
            ],
        },
        new()
        {
            Id = "inflation",
            Name = "Inflation",
            Description = "Body inflation and expansion",
            Tags =
            [
                "inflation",
                "belly_inflation",
                "cum_inflation",
                "body_expansion",
                "expansion",
            ],
        },
        new()
        {
            Id = "macro_micro",
            Name = "Macro / micro",
            Description = "Giant and tiny size-play tags",
            Tags =
            [
                "macro",
                "micro",
                "giant",
                "giantess",
                "miniguy",
                "minigirl",
                "size_play",
            ],
        },
        new()
        {
            Id = "bbw",
            Name = "BBW / weight gain",
            Description = "Large body and weight-gain tags",
            Tags =
            [
                "bbw",
                "ssbbw",
                "weight_gain",
                "fat",
                "obese",
                "chubby",
            ],
        },
        new()
        {
            Id = "monster",
            Name = "Monsters",
            Description = "Monsters, aliens, and creature partners",
            Tags =
            [
                "monster",
                "monstergirl",
                "alien",
                "creature",
                "xenophilia",
                "demon",
            ],
        },
        new()
        {
            Id = "mind_control",
            Name = "Mind control",
            Description = "Hypnosis, corruption, and mental domination",
            Tags =
            [
                "mind_control",
                "hypnosis",
                "brainwashing",
                "corruption",
                "petrification",
            ],
        },
        new()
        {
            Id = "ntr",
            Name = "NTR / cheating",
            Description = "Netorare and infidelity themes",
            Tags =
            [
                "netorare",
                "ntr",
                "cheating",
                "cuckolding",
                "affair",
            ],
        },
        new()
        {
            Id = "latex",
            Name = "Latex / rubber",
            Description = "Latex suits and rubber fetish gear",
            Tags =
            [
                "latex",
                "rubber",
                "bodysuit",
                "gimp_suit",
                "bondage_suit",
            ],
        },
        new()
        {
            Id = "furry_cub",
            Name = "Furry cub",
            Description = "Young-coded furry characters",
            Tags =
            [
                "cub",
                "young",
                "underage",
                "young_anthro",
            ],
        },
        new()
        {
            Id = "watersports",
            Name = "Watersports",
            Description = "Urine-focused fetish tags",
            Tags =
            [
                "watersports",
                "urine",
                "pee",
                "peeing",
                "golden_shower",
            ],
        },
        new()
        {
            Id = "diaper",
            Name = "Diaper / ABDL",
            Description = "Diapers and age-play adjacent tags",
            Tags =
            [
                "diaper",
                "abdl",
                "adult_baby",
                "diaper_fetish",
            ],
        },
        new()
        {
            Id = "skinsuit",
            Name = "Skinsuit / possession",
            Description = "Body theft, skinsuits, and possession",
            Tags =
            [
                "skinsuit",
                "bodysuit",
                "possession",
                "body_swap",
                "identity_death",
            ],
        },
        new()
        {
            Id = "amputee",
            Name = "Amputee",
            Description = "Amputation and stump-focused tags",
            Tags =
            [
                "amputee",
                "quadruple_amputee",
                "stump",
                "missing_limb",
            ],
        },
        new()
        {
            Id = "insect",
            Name = "Insects / arachnids",
            Description = "Bug and spider partners",
            Tags =
            [
                "insect",
                "spider",
                "arachnid",
                "bug",
                "bee",
            ],
        },
        new()
        {
            Id = "loli_shota_extra",
            Name = "Loli / shota (extra)",
            Description = "Additional young-coded tags",
            Tags =
            [
                "oppai_loli",
                "toddlercon",
            ],
        },
        new()
        {
            Id = "necro",
            Name = "Necrophilia",
            Description = "Dead / corpse content",
            Tags =
            [
                "necrophilia",
                "corpse",
                "death",
            ],
        },
        new()
        {
            Id = "cannibalism",
            Name = "Cannibalism",
            Description = "Cannibalism themes",
            Tags =
            [
                "cannibalism",
                "vore",
            ],
        },
        new()
        {
            Id = "tickling",
            Name = "Tickling",
            Description = "Tickle fetish",
            Tags =
            [
                "tickling",
                "tickle_torture",
            ],
        },
        new()
        {
            Id = "armpit",
            Name = "Armpit",
            Description = "Armpit fetish",
            Tags =
            [
                "armpit",
                "armpit_hair",
                "armpit_sex",
            ],
        },
        new()
        {
            Id = "fart",
            Name = "Fart",
            Description = "Fart fetish",
            Tags =
            [
                "fart",
                "farting",
            ],
        },
        new()
        {
            Id = "smell",
            Name = "Smell / musk",
            Description = "Odor-focused tags",
            Tags =
            [
                "smell",
                "musk",
                "stink",
            ],
        },
        new()
        {
            Id = "drugs",
            Name = "Drugs",
            Description = "Drug use depictions",
            Tags =
            [
                "drugs",
                "drug_use",
                "marijuana",
            ],
        },
        new()
        {
            Id = "smoking",
            Name = "Smoking",
            Description = "Cigarettes and smoking",
            Tags =
            [
                "smoking",
                "cigarette",
            ],
        },
        new()
        {
            Id = "slavery",
            Name = "Slavery",
            Description = "Slavery themes",
            Tags =
            [
                "slavery",
                "slave",
            ],
        },
        new()
        {
            Id = "pegging",
            Name = "Pegging",
            Description = "Female penetrating male",
            Tags =
            [
                "pegging",
                "strap-on",
                "strapon",
            ],
        },
        new()
        {
            Id = "femdom",
            Name = "Femdom",
            Description = "Female domination",
            Tags =
            [
                "femdom",
                "dominatrix",
                "female_domination",
            ],
        },
        new()
        {
            Id = "maledom",
            Name = "Maledom",
            Description = "Male domination",
            Tags =
            [
                "maledom",
                "male_domination",
            ],
        },
        new()
        {
            Id = "cuckold",
            Name = "Cuckold",
            Description = "Cuckolding (extra tags)",
            Tags =
            [
                "cuckold",
                "cuckolding",
            ],
        },
        new()
        {
            Id = "slime",
            Name = "Slime",
            Description = "Slime girls / creatures",
            Tags =
            [
                "slime",
                "slime_girl",
            ],
        },
        new()
        {
            Id = "robot",
            Name = "Robot / android",
            Description = "Mechanical partners",
            Tags =
            [
                "robot",
                "android",
                "mecha",
            ],
        },
        new()
        {
            Id = "furry_avian",
            Name = "Furry birds",
            Description = "Avian furry tags",
            Tags =
            [
                "avian",
                "bird",
                "feathers",
            ],
        },
        new()
        {
            Id = "hyper_pregnancy",
            Name = "Hyper pregnancy",
            Description = "Extreme pregnancy",
            Tags =
            [
                "hyper_pregnancy",
                "hyper_belly",
            ],
        },
        new()
        {
            Id = "lactation",
            Name = "Lactation",
            Description = "Breast milk / nursing",
            Tags =
            [
                "lactation",
                "breastfeeding",
                "milking",
            ],
        },
        new()
        {
            Id = "urine_drinking",
            Name = "Urine drinking",
            Description = "Consuming urine",
            Tags =
            [
                "urine_drinking",
                "pee_drinking",
            ],
        },
        new()
        {
            Id = "public_use",
            Name = "Public use",
            Description = "Public degradation",
            Tags =
            [
                "public_use",
                "glory_hole",
            ],
        },
        new()
        {
            Id = "bestiality_alt",
            Name = "Bestiality (alt)",
            Description = "Additional zoophilia tags",
            Tags =
            [
                "bestiality",
                "zoophilia",
                "animal_penis",
            ],
        },
        new()
        {
            Id = "guro_extreme",
            Name = "Extreme guro",
            Description = "Extreme guro variants",
            Tags =
            [
                "guro",
                "intestines",
                "disembodied",
            ],
        },
        new()
        {
            Id = "parasite",
            Name = "Parasite",
            Description = "Parasitic infestation",
            Tags =
            [
                "parasite",
                "parasitic",
                "infestation",
            ],
        },
        new()
        {
            Id = "body_writing",
            Name = "Body writing",
            Description = "Degrading text on body",
            Tags =
            [
                "body_writing",
                "degradation",
            ],
        },
        new()
        {
            Id = "humiliation",
            Name = "Humiliation",
            Description = "Humiliation themes",
            Tags =
            [
                "humiliation",
                "degradation",
                "embarrassed",
            ],
        },
        new()
        {
            Id = "yuri_exclude",
            Name = "Yuri / girls only",
            Description = "Female-only pairings",
            Tags =
            [
                "yuri",
                "girls_only",
                "2girls",
            ],
        },
        new()
        {
            Id = "bara_exclude",
            Name = "Bara / muscular men",
            Description = "Muscular male focus",
            Tags =
            [
                "bara",
                "muscular_male",
                "large_pectorals",
            ],
        },
    ];

    public static IReadOnlyList<BlacklistPreset> All { get; }

    static BlacklistPresetCatalog()
    {
        All = [.._basePresets, ..ExtendedPresets];
    }

    public static BlacklistPreset? Find(string id)
        => All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<string> GetTagsForPresets(IEnumerable<string> presetIds)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in presetIds)
        {
            var preset = Find(id);
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

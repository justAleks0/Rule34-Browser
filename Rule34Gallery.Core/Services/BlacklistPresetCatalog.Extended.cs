namespace Rule34Gallery.Core.Services;

public static partial class BlacklistPresetCatalog
{
    private static readonly BlacklistPreset[] ExtendedPresets =
    [
        new()
        {
            Id = "guro_light",
            Name = "Light guro",
            Description = "Blood and injury",
            Tags = ["blood", "injury", "bruise", "wound"],
        },
        new()
        {
            Id = "amputation",
            Name = "Amputation",
            Description = "Missing limbs",
            Tags = ["amputation", "stump", "missing_limb"],
        },
        new()
        {
            Id = "asphyxiation",
            Name = "Asphyxiation",
            Description = "Choking/strangling",
            Tags = ["asphyxiation", "strangling", "choking"],
        },
        new()
        {
            Id = "electrocution",
            Name = "Electrocution",
            Description = "Electric torture",
            Tags = ["electrocution", "electric_torture"],
        },
        new()
        {
            Id = "fire_play",
            Name = "Fire play",
            Description = "Burns",
            Tags = ["fire", "burn", "burning"],
        },
        new()
        {
            Id = "knife_play",
            Name = "Knife play",
            Description = "Blades",
            Tags = ["knife", "blade", "cutting"],
        },
        new()
        {
            Id = "gunplay",
            Name = "Gunplay",
            Description = "Firearms in scene",
            Tags = ["gun", "firearm", "gunplay"],
        },
        new()
        {
            Id = "snuff",
            Name = "Snuff",
            Description = "Death themes",
            Tags = ["snuff", "death", "dying"],
        },
        new()
        {
            Id = "ryona",
            Name = "Ryona",
            Description = "Beat-up fetish",
            Tags = ["ryona", "beaten", "bruised"],
        },
        new()
        {
            Id = "unbirth",
            Name = "Unbirth",
            Description = "Reverse birth",
            Tags = ["unbirth"],
        },
        new()
        {
            Id = "cock_vore",
            Name = "Cock vore",
            Description = "Genital vore",
            Tags = ["cock_vore"],
        },
        new()
        {
            Id = "anal_vore",
            Name = "Anal vore",
            Description = "Anal vore",
            Tags = ["anal_vore"],
        },
        new()
        {
            Id = "soft_vore",
            Name = "Soft vore",
            Description = "Non-fatal vore",
            Tags = ["soft_vore"],
        },
        new()
        {
            Id = "hard_vore",
            Name = "Hard vore",
            Description = "Fatal vore",
            Tags = ["hard_vore"],
        },
        new()
        {
            Id = "oral_vore",
            Name = "Oral vore",
            Description = "Mouth vore",
            Tags = ["oral_vore"],
        },
        new()
        {
            Id = "full_tour",
            Name = "Full tour vore",
            Description = "Digestion path",
            Tags = ["full_tour"],
        },
        new()
        {
            Id = "digestion",
            Name = "Digestion",
            Description = "Stomach acids",
            Tags = ["digestion", "stomach_acids"],
        },
        new()
        {
            Id = "weight_gain",
            Name = "Weight gain",
            Description = "Fattening",
            Tags = ["weight_gain", "fattening", "getting_fatter"],
        },
        new()
        {
            Id = "feederism",
            Name = "Feederism",
            Description = "Feeding kink",
            Tags = ["feederism", "feeding"],
        },
        new()
        {
            Id = "vore_alt",
            Name = "Vore (extra tags)",
            Description = "More vore tags",
            Tags = ["vore", "pred", "prey"],
        },
        new()
        {
            Id = "macro",
            Name = "Macro",
            Description = "Giant size",
            Tags = ["macro", "giant", "giantess"],
        },
        new()
        {
            Id = "micro",
            Name = "Micro",
            Description = "Tiny size",
            Tags = ["micro", "shrunken", "minigirl", "miniboy"],
        },
        new()
        {
            Id = "size_difference",
            Name = "Extreme size difference",
            Description = "Huge gap",
            Tags = ["size_difference"],
        },
        new()
        {
            Id = "unbirth_alt",
            Name = "Unbirth alt",
            Description = "More unbirth",
            Tags = ["unbirthing"],
        },
        new()
        {
            Id = "furry_insects",
            Name = "Furry insects",
            Description = "Bug anthro",
            Tags = ["insect", "arthropod", "bug"],
        },
        new()
        {
            Id = "furry_aquatic",
            Name = "Furry aquatic",
            Description = "Fish/sea anthro",
            Tags = ["aquatic", "fish", "shark", "dolphin"],
        },
        new()
        {
            Id = "furry_plant",
            Name = "Plant furry",
            Description = "Plant anthro",
            Tags = ["plant", "plantification"],
        },
        new()
        {
            Id = "furry_robot",
            Name = "Robot furry",
            Description = "Mecha anthro",
            Tags = ["robot", "mecha", "cyborg"],
        },
        new()
        {
            Id = "feral",
            Name = "Feral",
            Description = "Non-anthro animals",
            Tags = ["feral", "feral_intercourse", "animal_genitalia"],
        },
        new()
        {
            Id = "zoophilia_extra",
            Name = "Zoophilia extra",
            Description = "More bestiality tags",
            Tags = ["zoophilia", "animal_penetration"],
        },
        new()
        {
            Id = "human_on_feral",
            Name = "Human on feral",
            Description = "Human/animal",
            Tags = ["human_on_feral"],
        },
        new()
        {
            Id = "animal_on_human",
            Name = "Animal on human",
            Description = "Animal/human",
            Tags = ["animal_on_human"],
        },
        new()
        {
            Id = "dog",
            Name = "Dogs",
            Description = "Canine content",
            Tags = ["dog", "canine", "puppy"],
        },
        new()
        {
            Id = "horse",
            Name = "Horses",
            Description = "Equine content",
            Tags = ["horse", "equine"],
        },
        new()
        {
            Id = "pig",
            Name = "Pigs",
            Description = "Swine content",
            Tags = ["pig", "swine"],
        },
        new()
        {
            Id = "cow",
            Name = "Cows",
            Description = "Bovine content",
            Tags = ["cow", "bovine"],
        },
        new()
        {
            Id = "sheep",
            Name = "Sheep",
            Description = "Ovine content",
            Tags = ["sheep", "ovine"],
        },
        new()
        {
            Id = "reptile_furry",
            Name = "Reptile furry",
            Description = "Scales",
            Tags = ["reptile", "scalie", "snake", "lizard"],
        },
        new()
        {
            Id = "avian_furry_extra",
            Name = "Avian furry extra",
            Description = "Birds",
            Tags = ["bird", "avian", "feathered_wings"],
        },
        new()
        {
            Id = "prolapse",
            Name = "Prolapse",
            Description = "Medical extreme",
            Tags = ["prolapse"],
        },
        new()
        {
            Id = "gaping",
            Name = "Gaping",
            Description = "Extreme stretch",
            Tags = ["gaping", "gaping_anus", "gaping_pussy"],
        },
        new()
        {
            Id = "fisting",
            Name = "Fisting",
            Description = "Hand insertion",
            Tags = ["fisting", "anal_fisting", "vaginal_fisting"],
        },
        new()
        {
            Id = "enema",
            Name = "Enema",
            Description = "Enema play",
            Tags = ["enema"],
        },
        new()
        {
            Id = "enema_alt",
            Name = "Enema alt",
            Description = "Colon play",
            Tags = ["colon_play"],
        },
        new()
        {
            Id = "scat_extra",
            Name = "Scat extra",
            Description = "More scat tags",
            Tags = ["scat", "feces", "defecation", "coprophagia"],
        },
        new()
        {
            Id = "fart_extra",
            Name = "Fart extra",
            Description = "More gas tags",
            Tags = ["fart", "flatulence", "farting"],
        },
        new()
        {
            Id = "burping",
            Name = "Burping",
            Description = "Belching",
            Tags = ["burp", "belching"],
        },
        new()
        {
            Id = "vomit",
            Name = "Vomit",
            Description = "Emetophilia",
            Tags = ["vomit", "vomiting", "puke"],
        },
        new()
        {
            Id = "spit",
            Name = "Spit",
            Description = "Spitting",
            Tags = ["spit", "spitting"],
        },
        new()
        {
            Id = "saliva",
            Name = "Saliva excess",
            Description = "Drool focus",
            Tags = ["saliva", "drooling", "slobber"],
        },
        new()
        {
            Id = "nosebleed",
            Name = "Nosebleed",
            Description = "Blood from nose",
            Tags = ["nosebleed"],
        },
        new()
        {
            Id = "menstruation",
            Name = "Menstruation",
            Description = "Period",
            Tags = ["menstruation", "period"],
        },
        new()
        {
            Id = "urination_extra",
            Name = "Urination extra",
            Description = "More pee tags",
            Tags = ["urination", "peeing", "pee", "golden_shower"],
        },
        new()
        {
            Id = "diaper_extra",
            Name = "Diaper extra",
            Description = "More ABDL",
            Tags = ["diaper", "abdl", "adult_baby"],
        },
        new()
        {
            Id = "age_regression",
            Name = "Age regression",
            Description = "Mental/physical age",
            Tags = ["age_regression", "age_play"],
        },
        new()
        {
            Id = "dollification",
            Name = "Dollification",
            Description = "Doll TF",
            Tags = ["dollification", "doll"],
        },
        new()
        {
            Id = "statue",
            Name = "Statue TF",
            Description = "Stone/frozen",
            Tags = ["statue", "petrification", "frozen"],
        },
        new()
        {
            Id = "latex_extra",
            Name = "Latex extra",
            Description = "Rubber gear",
            Tags = ["latex", "rubber", "rubber_suit"],
        },
        new()
        {
            Id = "vacbed",
            Name = "Vacbed",
            Description = "Vacuum bed",
            Tags = ["vacbed", "vacuum_bed"],
        },
        new()
        {
            Id = "bondage_extra",
            Name = "Bondage extra",
            Description = "More restraint",
            Tags = ["shibari", "rope_bondage", "cuffs", "chains"],
        },
        new()
        {
            Id = "gag",
            Name = "Gags",
            Description = "Mouth gags",
            Tags = ["gag", "ball_gag", "ring_gag"],
        },
        new()
        {
            Id = "blindfold",
            Name = "Blindfold",
            Description = "Eyes covered",
            Tags = ["blindfold"],
        },
        new()
        {
            Id = "chastity",
            Name = "Chastity",
            Description = "Locked genitals",
            Tags = ["chastity", "chastity_cage", "chastity_belt"],
        },
        new()
        {
            Id = "cbt",
            Name = "CBT",
            Description = "Genital torture",
            Tags = ["cbt", "ball_busting", "testicle_torture"],
        },
        new()
        {
            Id = "nipple_torture",
            Name = "Nipple torture",
            Description = "Pain play",
            Tags = ["nipple_torture", "nipple_clamps"],
        },
        new()
        {
            Id = "wax_play",
            Name = "Wax play",
            Description = "Hot wax",
            Tags = ["wax_play", "candle_wax"],
        },
        new()
        {
            Id = "needle_play",
            Name = "Needle play",
            Description = "Piercing pain",
            Tags = ["needle_play", "piercing_play"],
        },
        new()
        {
            Id = "breath_play",
            Name = "Breath play",
            Description = "Breath control",
            Tags = ["breath_play"],
        },
        new()
        {
            Id = "pet_play",
            Name = "Pet play",
            Description = "Petplay",
            Tags = ["pet_play", "petplay", "leash", "collar"],
        },
        new()
        {
            Id = "pony_play",
            Name = "Pony play",
            Description = "Harness cart",
            Tags = ["pony_play"],
        },
        new()
        {
            Id = "humiliation_extra",
            Name = "Humiliation extra",
            Description = "Degradation",
            Tags = ["degradation", "humiliation", "insult"],
        },
        new()
        {
            Id = "public_humiliation",
            Name = "Public humiliation",
            Description = "Exposed shame",
            Tags = ["public_humiliation", "embarrassed_public"],
        },
        new()
        {
            Id = "filming",
            Name = "Filming",
            Description = "Camera recording",
            Tags = ["filming", "camera", "recording"],
        },
        new()
        {
            Id = "prostitution",
            Name = "Prostitution",
            Description = "Sex work",
            Tags = ["prostitution", "hooker"],
        },
        new()
        {
            Id = "cheating",
            Name = "Cheating",
            Description = "Affair",
            Tags = ["cheating", "affair"],
        },
        new()
        {
            Id = "netorare_extra",
            Name = "NTR extra",
            Description = "Cuckolding tags",
            Tags = ["netorare", "cuckolding", "cuckold"],
        },
        new()
        {
            Id = "rape_extra",
            Name = "Non-con extra",
            Description = "More force tags",
            Tags = ["rape", "forced", "non-consensual"],
        },
        new()
        {
            Id = "drugged",
            Name = "Drugged",
            Description = "Intoxicated sex",
            Tags = ["drugged", "intoxicated"],
        },
        new()
        {
            Id = "hypnosis_extra",
            Name = "Hypnosis extra",
            Description = "Mind control tags",
            Tags = ["hypnosis", "hypnotized", "mind_break"],
        },
        new()
        {
            Id = "brainwashing",
            Name = "Brainwashing",
            Description = "Conditioning",
            Tags = ["brainwashing", "conditioning"],
        },
        new()
        {
            Id = "corruption",
            Name = "Corruption",
            Description = "Fallen hero",
            Tags = ["corruption", "corrupted"],
        },
        new()
        {
            Id = "monster_girl_exclude",
            Name = "Monster girl",
            Description = "Monstergirls",
            Tags = ["monster_girl", "monstergirl"],
        },
        new()
        {
            Id = "slime_girl",
            Name = "Slime girl",
            Description = "Slime characters",
            Tags = ["slime_girl"],
        },
        new()
        {
            Id = "robot_girl",
            Name = "Robot girl",
            Description = "Android girls",
            Tags = ["robot_girl", "android"],
        },
        new()
        {
            Id = "undead",
            Name = "Undead",
            Description = "Zombie/ghost",
            Tags = ["zombie", "ghost", "undead"],
        },
        new()
        {
            Id = "necrophilia_extra",
            Name = "Necrophilia extra",
            Description = "Dead body",
            Tags = ["necrophilia", "corpse"],
        },
        new()
        {
            Id = "parasite_extra",
            Name = "Parasite extra",
            Description = "Parasites",
            Tags = ["parasite", "parasitic", "infestation"],
        },
        new()
        {
            Id = "insect_sex",
            Name = "Insect sex",
            Description = "Bug sex",
            Tags = ["insect_sex", "bee", "wasp", "spider"],
        },
        new()
        {
            Id = "tentacle_extra",
            Name = "Tentacle extra",
            Description = "More tentacle",
            Tags = ["tentacle_sex", "tentacle_penetration"],
        },
        new()
        {
            Id = "oviposition",
            Name = "Oviposition",
            Description = "Egg laying",
            Tags = ["oviposition", "egg_laying"],
        },
        new()
        {
            Id = "egg_implant",
            Name = "Egg implant",
            Description = "Internal eggs",
            Tags = ["egg_implant"],
        },
        new()
        {
            Id = "birth",
            Name = "Birth",
            Description = "Labor/birth",
            Tags = ["birth", "labor"],
        },
        new()
        {
            Id = "impregnation",
            Name = "Impregnation",
            Description = "Bred",
            Tags = ["impregnation", "breeding"],
        },
        new()
        {
            Id = "pregnant_extra",
            Name = "Pregnant extra",
            Description = "More pregnancy",
            Tags = ["pregnant", "pregnancy", "belly"],
        },
        new()
        {
            Id = "lactation_extra",
            Name = "Lactation extra",
            Description = "Milk",
            Tags = ["lactation", "breast_milk", "milking"],
        },
        new()
        {
            Id = "hyper_breasts",
            Name = "Hyper breasts",
            Description = "Extreme chest",
            Tags = ["hyper_breasts", "hyper_boobs"],
        },
        new()
        {
            Id = "hyper_ass",
            Name = "Hyper ass",
            Description = "Extreme rear",
            Tags = ["hyper_ass", "hyper_butt"],
        },
        new()
        {
            Id = "hyper_belly",
            Name = "Hyper belly",
            Description = "Extreme stomach",
            Tags = ["hyper_belly"],
        },
        new()
        {
            Id = "hyper_muscles",
            Name = "Hyper muscles",
            Description = "Extreme muscle",
            Tags = ["hyper_muscles", "hyper_muscle"],
        },
        new()
        {
            Id = "muscular_male_extra",
            Name = "Muscular male extra",
            Description = "Muscle men",
            Tags = ["muscular_male", "bodybuilder"],
        },
        new()
        {
            Id = "obese",
            Name = "Obese",
            Description = "Very fat",
            Tags = ["obese", "fat", "morbidly_obese"],
        },
        new()
        {
            Id = "skinny",
            Name = "Extreme thin",
            Description = "Very thin",
            Tags = ["skinny", "anorexic"],
        },
        new()
        {
            Id = "ugly",
            Name = "Ugly",
            Description = "Unattractive focus",
            Tags = ["ugly", "unattractive"],
        },
        new()
        {
            Id = "old",
            Name = "Elderly",
            Description = "Old characters",
            Tags = ["old", "elderly", "grandmother", "grandfather"],
        },
        new()
        {
            Id = "baby",
            Name = "Infants",
            Description = "Babies",
            Tags = ["baby", "infant"],
        },
        new()
        {
            Id = "toddler",
            Name = "Toddlers",
            Description = "Young children",
            Tags = ["toddler"],
        },
        new()
        {
            Id = "child",
            Name = "Children",
            Description = "Child characters",
            Tags = ["child"],
        },
        new()
        {
            Id = "young",
            Name = "Young-looking",
            Description = "Youth-coded",
            Tags = ["young", "young-looking"],
        },
        new()
        {
            Id = "student_loli",
            Name = "Schoolchild coded",
            Description = "Young uniform",
            Tags = ["elementary_school_uniform"],
        },
        new()
        {
            Id = "teacher_student",
            Name = "Teacher/student",
            Description = "Power imbalance",
            Tags = ["teacher_and_student"],
        },
        new()
        {
            Id = "family",
            Name = "Family taboo",
            Description = "Relatives",
            Tags = ["family", "relative", "sister", "brother", "mother", "father"],
        },
        new()
        {
            Id = "step_family",
            Name = "Step-family",
            Description = "Step relations",
            Tags = ["step-sister", "step-brother", "step-mother", "step-father"],
        },
        new()
        {
            Id = "cousin",
            Name = "Cousin",
            Description = "Cousin incest",
            Tags = ["cousin"],
        },
        new()
        {
            Id = "aunt_uncle",
            Name = "Aunt/uncle",
            Description = "Extended family",
            Tags = ["aunt", "uncle"],
        },
        new()
        {
            Id = "twincest",
            Name = "Twincest",
            Description = "Twins",
            Tags = ["twincest", "twins"],
        },
        new()
        {
            Id = "yuri_exclude_extra",
            Name = "Yuri extra",
            Description = "Girl-girl",
            Tags = ["yuri", "girls_only", "2girls", "tribadism"],
        },
        new()
        {
            Id = "yaoi_extra",
            Name = "Yaoi extra",
            Description = "Boy-boy",
            Tags = ["yaoi", "males_only", "2boys"],
        },
        new()
        {
            Id = "futa_extra",
            Name = "Futa extra",
            Description = "More futa tags",
            Tags = ["futanari", "futa", "dickgirl", "herm"],
        },
        new()
        {
            Id = "male_focus_extra",
            Name = "Male focus extra",
            Description = "Male-centric",
            Tags = ["male_focus", "1boy"],
        },
        new()
        {
            Id = "solo_male_exclude",
            Name = "Solo male",
            Description = "Male solo",
            Tags = ["solo_male", "1boy", "solo"],
        },
        new()
        {
            Id = "solo_female_exclude",
            Name = "Solo female only",
            Description = "No solo female",
            Tags = ["solo_female"],
        },
        new()
        {
            Id = "group_male",
            Name = "Male group",
            Description = "Multiple males",
            Tags = ["multiple_boys", "group_of_men"],
        },
        new()
        {
            Id = "trans",
            Name = "Trans themes",
            Description = "Trans characters",
            Tags = ["transgender", "trans", "trans_woman", "trans_man"],
        },
        new()
        {
            Id = "crossdressing",
            Name = "Crossdressing",
            Description = "Opposite gender clothes",
            Tags = ["crossdressing", "trap"],
        },
        new()
        {
            Id = "sissy",
            Name = "Sissy",
            Description = "Feminization",
            Tags = ["sissy", "feminization"],
        },
        new()
        {
            Id = "forced_feminization",
            Name = "Forced feminization",
            Description = "Forced dress",
            Tags = ["forced_feminization"],
        },
        new()
        {
            Id = "gender_swap",
            Name = "Gender swap",
            Description = "TG",
            Tags = ["gender_swap", "gender_bender"],
        },
        new()
        {
            Id = "mtf",
            Name = "MTF",
            Description = "Male to female",
            Tags = ["mtf"],
        },
        new()
        {
            Id = "ftm",
            Name = "FTM",
            Description = "Female to male",
            Tags = ["ftm"],
        },
        new()
        {
            Id = "herm_extra",
            Name = "Herm extra",
            Description = "Intersex",
            Tags = ["herm", "intersex"],
        },
        new()
        {
            Id = "cuntboy",
            Name = "Cuntboy",
            Description = "Male with vagina",
            Tags = ["cuntboy"],
        },
        new()
        {
            Id = "gynomorph",
            Name = "Gynomorph",
            Description = "Feminine male body",
            Tags = ["gynomorph"],
        },
        new()
        {
            Id = "andromorph",
            Name = "Andromorph",
            Description = "Masculine female body",
            Tags = ["andromorph"],
        },
        new()
        {
            Id = "ai_extra",
            Name = "AI extra",
            Description = "More AI tags",
            Tags = ["ai_generated", "stable_diffusion", "novelai", "midjourney"],
        },
        new()
        {
            Id = "3d_exclude",
            Name = "3D render",
            Description = "CGI",
            Tags = ["3d", "3d_(artwork)", "render"],
        },
        new()
        {
            Id = "western_cartoon",
            Name = "Western cartoon",
            Description = "Western style",
            Tags = ["western_cartoon", "western_animation"],
        },
        new()
        {
            Id = "live_action",
            Name = "Live action",
            Description = "Real people",
            Tags = ["live_action", "real_person"],
        },
        new()
        {
            Id = "photo_real",
            Name = "Photoreal",
            Description = "Photos",
            Tags = ["photo_(medium)", "photograph"],
        },
        new()
        {
            Id = "low_quality",
            Name = "Low quality",
            Description = "Bad art",
            Tags = ["low_quality", "bad_anatomy", "bad_proportions"],
        },
        new()
        {
            Id = "watermark",
            Name = "Watermark",
            Description = "Marked images",
            Tags = ["watermark", "sample_watermark"],
        },
        new()
        {
            Id = "comic_exclude",
            Name = "Comics",
            Description = "Multi-page",
            Tags = ["comic", "comic_page"],
        },
        new()
        {
            Id = "text_heavy",
            Name = "Text heavy",
            Description = "Lots of text",
            Tags = ["text_heavy", "speech_bubble"],
        },
        new()
        {
            Id = "meme_exclude",
            Name = "Memes",
            Description = "Meme posts",
            Tags = ["meme"],
        },
        new()
        {
            Id = "gore_extra",
            Name = "Gore extra",
            Description = "Graphic violence",
            Tags = ["gore", "guro", "dismemberment", "entrails"],
        },
        new()
        {
            Id = "blood_extra",
            Name = "Blood extra",
            Description = "Blood focus",
            Tags = ["blood", "bloody"],
        },
        new()
        {
            Id = "weapon",
            Name = "Weapons",
            Description = "Guns and blades",
            Tags = ["weapon", "sword", "gun"],
        },
        new()
        {
            Id = "military_violence",
            Name = "Military violence",
            Description = "War themes",
            Tags = ["military", "war", "soldier"],
        },
        new()
        {
            Id = "political",
            Name = "Political",
            Description = "Political themes",
            Tags = ["political", "nazi", "confederate"],
        },
        new()
        {
            Id = "religious",
            Name = "Religious",
            Description = "Religious themes",
            Tags = ["religious", "crucifixion"],
        },
        new()
        {
            Id = "racist",
            Name = "Racist themes",
            Description = "Hate imagery",
            Tags = ["racist", "racism"],
        },
        new()
        {
            Id = "diaper_mess",
            Name = "Diaper mess",
            Description = "Soiled diaper",
            Tags = ["diaper_mess", "messy_diaper"],
        },
        new()
        {
            Id = "toilet",
            Name = "Toilet",
            Description = "Bathroom filth",
            Tags = ["toilet", "bathroom"],
        },
        new()
        {
            Id = "mucus",
            Name = "Mucus",
            Description = "Snot/slime bodily",
            Tags = ["mucus", "snot"],
        },
        new()
        {
            Id = "ear_play",
            Name = "Ear play",
            Description = "Ear fetish",
            Tags = ["ear_play", "ear_fuck"],
        },
        new()
        {
            Id = "navel_play",
            Name = "Navel play",
            Description = "Belly button",
            Tags = ["navel_fuck", "navel_play"],
        },
        new()
        {
            Id = "armpit_extra",
            Name = "Armpit extra",
            Description = "More armpit",
            Tags = ["armpit", "armpit_hair", "armpit_licking"],
        },
        new()
        {
            Id = "belly_button",
            Name = "Belly",
            Description = "Stomach focus",
            Tags = ["belly", "stomach"],
        },
        new()
        {
            Id = "feet_extra",
            Name = "Feet extra",
            Description = "More foot tags",
            Tags = ["feet", "foot_focus", "toes", "soles", "footjob"],
        },
        new()
        {
            Id = "tickling_extra",
            Name = "Tickling extra",
            Description = "More tickle",
            Tags = ["tickling", "tickle_torture"],
        },
        new()
        {
            Id = "smoking_extra",
            Name = "Smoking extra",
            Description = "Tobacco",
            Tags = ["smoking", "cigarette", "cigar"],
        },
        new()
        {
            Id = "alcohol",
            Name = "Alcohol",
            Description = "Drunk themes",
            Tags = ["alcohol", "drunk", "beer", "wine"],
        },
        new()
        {
            Id = "drugs_extra",
            Name = "Drugs extra",
            Description = "Substances",
            Tags = ["drugs", "cocaine", "heroin", "meth"],
        },
        new()
        {
            Id = "piercing_extreme",
            Name = "Extreme piercing",
            Description = "Heavy mods",
            Tags = ["piercing", "body_modification"],
        },
        new()
        {
            Id = "tattoo_heavy",
            Name = "Heavy tattoos",
            Description = "Full ink",
            Tags = ["tattoo", "full_body_tattoo"],
        },
        new()
        {
            Id = "scar",
            Name = "Scars",
            Description = "Scar focus",
            Tags = ["scar", "scarring"],
        },
        new()
        {
            Id = "amputee_extra",
            Name = "Amputee extra",
            Description = "More amputee",
            Tags = ["amputee", "amputation"],
        },
        new()
        {
            Id = "wheelchair",
            Name = "Wheelchair",
            Description = "Disability",
            Tags = ["wheelchair"],
        },
        new()
        {
            Id = "blind",
            Name = "Blind",
            Description = "Vision disability",
            Tags = ["blind"],
        },
        new()
        {
            Id = "deaf",
            Name = "Deaf",
            Description = "Hearing disability",
            Tags = ["deaf"],
        },
        new()
        {
            Id = "paralysis",
            Name = "Paralysis",
            Description = "Immobilized",
            Tags = ["paralysis", "paralyzed"],
        },
        new()
        {
            Id = "medical",
            Name = "Medical play",
            Description = "Hospital extreme",
            Tags = ["medical", "hospital", "syringe"],
        },
        new()
        {
            Id = "surgery",
            Name = "Surgery",
            Description = "Operation",
            Tags = ["surgery", "operation"],
        },
        new()
        {
            Id = "castration",
            Name = "Castration",
            Description = "Genital removal",
            Tags = ["castration"],
        },
        new()
        {
            Id = "circumcision",
            Name = "Circumcision",
            Description = "Cutting theme",
            Tags = ["circumcision"],
        },
        new()
        {
            Id = "circumcision_play",
            Name = "Genital mod",
            Description = "Body mod",
            Tags = ["genital_modification"],
        },
        new()
        {
            Id = "urethral",
            Name = "Urethral",
            Description = "Sounding",
            Tags = ["urethral", "sounding"],
        },
        new()
        {
            Id = "anal_prolapse",
            Name = "Anal prolapse",
            Description = "Rear prolapse",
            Tags = ["anal_prolapse"],
        },
        new()
        {
            Id = "cervix",
            Name = "Cervix",
            Description = "Deep penetration",
            Tags = ["cervix", "cervix_penetration"],
        },
        new()
        {
            Id = "x_ray",
            Name = "X-ray view",
            Description = "Internal view",
            Tags = ["x-ray"],
        },
        new()
        {
            Id = "internal",
            Name = "Internal view",
            Description = "Cross-section",
            Tags = ["internal", "cross-section"],
        },
        new()
        {
            Id = "stomach_bulge",
            Name = "Stomach bulge",
            Description = "Visible outline",
            Tags = ["stomach_bulge"],
        },
        new()
        {
            Id = "inflation_extra",
            Name = "Inflation extra",
            Description = "More inflation",
            Tags = ["inflation", "inflated", "belly_inflation"],
        },
        new()
        {
            Id = "cum_inflation",
            Name = "Cum inflation",
            Description = "Fluid swell",
            Tags = ["cum_inflation"],
        },
        new()
        {
            Id = "breast_expansion",
            Name = "Breast expansion",
            Description = "Growing chest",
            Tags = ["breast_expansion"],
        },
        new()
        {
            Id = "ass_expansion",
            Name = "Ass expansion",
            Description = "Growing rear",
            Tags = ["ass_expansion"],
        },
        new()
        {
            Id = "muscle_growth",
            Name = "Muscle growth",
            Description = "Growing muscle",
            Tags = ["muscle_growth"],
        },
        new()
        {
            Id = "shrinking",
            Name = "Shrinking",
            Description = "Size down",
            Tags = ["shrinking"],
        },
        new()
        {
            Id = "growth",
            Name = "Growth",
            Description = "Size up",
            Tags = ["growth"],
        },
        new()
        {
            Id = "transformation_extra",
            Name = "TF extra",
            Description = "More transformation",
            Tags = ["transformation", "tf", "morph"],
        },
        new()
        {
            Id = "species_transformation",
            Name = "Species TF",
            Description = "Species change",
            Tags = ["species_transformation"],
        },
        new()
        {
            Id = "gender_transformation",
            Name = "Gender TF",
            Description = "Sex change",
            Tags = ["gender_transformation"],
        },
        new()
        {
            Id = "inanimate",
            Name = "Inanimate TF",
            Description = "Object TF",
            Tags = ["inanimate_transformation"],
        },
        new()
        {
            Id = "food_tf",
            Name = "Food TF",
            Description = "Edible TF",
            Tags = ["food_transformation"],
        },
        new()
        {
            Id = "clothing_tf",
            Name = "Clothing TF",
            Description = "Living clothes",
            Tags = ["living_clothes"],
        },
        new()
        {
            Id = "possession",
            Name = "Possession",
            Description = "Body takeover",
            Tags = ["possession", "body_takeover"],
        },
        new()
        {
            Id = "ghost",
            Name = "Ghost",
            Description = "Spectral",
            Tags = ["ghost"],
        },
        new()
        {
            Id = "demon",
            Name = "Demon",
            Description = "Hell themes",
            Tags = ["demon", "hell"],
        },
        new()
        {
            Id = "religion_dark",
            Name = "Dark religion",
            Description = "Occult extreme",
            Tags = ["occult", "satanic"],
        },
        new()
        {
            Id = "public_nudity",
            Name = "Public nudity",
            Description = "Exhibitionism",
            Tags = ["public_nudity", "exhibitionism"],
        },
        new()
        {
            Id = "voyeurism",
            Name = "Voyeurism",
            Description = "Watching",
            Tags = ["voyeurism", "peeping"],
        },
        new()
        {
            Id = "hidden_sex",
            Name = "Hidden sex",
            Description = "Secret act",
            Tags = ["hidden_sex", "stealth_sex"],
        },
        new()
        {
            Id = "prostitution_extra",
            Name = "Prostitution extra",
            Description = "Escort",
            Tags = ["escort", "brothel"],
        },
        new()
        {
            Id = "slave_extra",
            Name = "Slavery extra",
            Description = "Ownership",
            Tags = ["slave", "slavery", "owned"],
        },
        new()
        {
            Id = "branding",
            Name = "Branding",
            Description = "Marking slave",
            Tags = ["branding", "brand"],
        },
        new()
        {
            Id = "piercing_slave",
            Name = "Piercing slave",
            Description = "Permanent mods",
            Tags = ["piercing_slave"],
        },
        new()
        {
            Id = "chain",
            Name = "Chains",
            Description = "Restraint",
            Tags = ["chain", "chained"],
        },
        new()
        {
            Id = "cage",
            Name = "Cage",
            Description = "Imprisonment",
            Tags = ["cage", "caged"],
        },
        new()
        {
            Id = "dungeon",
            Name = "Dungeon",
            Description = "BDSM room",
            Tags = ["dungeon"],
        },
        new()
        {
            Id = "whip",
            Name = "Whipping",
            Description = "Flagellation",
            Tags = ["whip", "whipping", "flogging"],
        },
        new()
        {
            Id = "spanking",
            Name = "Spanking",
            Description = "Impact play",
            Tags = ["spanking"],
        },
        new()
        {
            Id = "paddle",
            Name = "Paddle",
            Description = "Impact play",
            Tags = ["paddle"],
        },
        new()
        {
            Id = "crop",
            Name = "Riding crop",
            Description = "Impact play",
            Tags = ["riding_crop"],
        },
        new()
        {
            Id = "hot_wax",
            Name = "Hot wax",
            Description = "Wax torture",
            Tags = ["hot_wax"],
        },
        new()
        {
            Id = "ice_play",
            Name = "Ice play",
            Description = "Temperature",
            Tags = ["ice_play"],
        },
        new()
        {
            Id = "temperature",
            Name = "Temperature play",
            Description = "Hot/cold",
            Tags = ["temperature_play"],
        },
        new()
        {
            Id = "suspension",
            Name = "Suspension",
            Description = "Hanging bondage",
            Tags = ["suspension"],
        },
        new()
        {
            Id = "hogtie",
            Name = "Hogtie",
            Description = "Bound pose",
            Tags = ["hogtie"],
        },
        new()
        {
            Id = "mummification",
            Name = "Mummification",
            Description = "Wrapped bound",
            Tags = ["mummification"],
        },
        new()
        {
            Id = "encasement",
            Name = "Encasement",
            Description = "Trapped in material",
            Tags = ["encasement"],
        },
        new()
        {
            Id = "vacuum",
            Name = "Vacuum",
            Description = "Suction play",
            Tags = ["vacuum"],
        },
        new()
        {
            Id = "machine",
            Name = "Sex machine",
            Description = "Mechanical",
            Tags = ["sex_machine", "machine"],
        },
        new()
        {
            Id = "robot_sex",
            Name = "Robot sex",
            Description = "Mechanical partner",
            Tags = ["robot_sex"],
        },
        new()
        {
            Id = "doll",
            Name = "Sex doll",
            Description = "Doll partner",
            Tags = ["sex_doll"],
        },
        new()
        {
            Id = "inflatable",
            Name = "Inflatable toy",
            Description = "Pool toy etc",
            Tags = ["inflatable"],
        },
        new()
        {
            Id = "object_insertion",
            Name = "Object insertion",
            Description = "Non-toy insert",
            Tags = ["object_insertion"],
        },
        new()
        {
            Id = "large_insertion",
            Name = "Large insertion",
            Description = "Oversized",
            Tags = ["large_insertion"],
        },
        new()
        {
            Id = "anal_beads",
            Name = "Anal beads",
            Description = "Toy",
            Tags = ["anal_beads"],
        },
        new()
        {
            Id = "butt_plug",
            Name = "Butt plug",
            Description = "Toy",
            Tags = ["butt_plug"],
        },
        new()
        {
            Id = "strap_on",
            Name = "Strap-on",
            Description = "Pegging gear",
            Tags = ["strap-on"],
        },
        new()
        {
            Id = "double_dildo",
            Name = "Double dildo",
            Description = "Toy",
            Tags = ["double_dildo"],
        },
        new()
        {
            Id = "sybian",
            Name = "Sybian",
            Description = "Ride machine",
            Tags = ["sybian"],
        },
        new()
        {
            Id = "vibrator_extra",
            Name = "Vibrator extra",
            Description = "More toy tags",
            Tags = ["vibrator", "egg_vibrator", "remote_vibrator"],
        },
        new()
        {
            Id = "denial",
            Name = "Orgasm denial",
            Description = "Edging",
            Tags = ["orgasm_denial", "edging"],
        },
        new()
        {
            Id = "forced_orgasm",
            Name = "Forced orgasm",
            Description = "Overstim",
            Tags = ["forced_orgasm"],
        },
        new()
        {
            Id = "ruined_orgasm",
            Name = "Ruined orgasm",
            Description = "Denied finish",
            Tags = ["ruined_orgasm"],
        },
        new()
        {
            Id = "chastity_play",
            Name = "Chastity play",
            Description = "Locked",
            Tags = ["chastity_play"],
        },
        new()
        {
            Id = "cuckquean",
            Name = "Cuckquean",
            Description = "Female cuck",
            Tags = ["cuckquean"],
        },
        new()
        {
            Id = "hotwife",
            Name = "Hotwife",
            Description = "Sharing wife",
            Tags = ["hotwife"],
        },
        new()
        {
            Id = "swinging",
            Name = "Swinging",
            Description = "Partner swap",
            Tags = ["swinging"],
        },
        new()
        {
            Id = "orgy",
            Name = "Orgy",
            Description = "Large group",
            Tags = ["orgy"],
        },
        new()
        {
            Id = "gangbang_exclude",
            Name = "Gangbang",
            Description = "Many on one",
            Tags = ["gangbang"],
        },
        new()
        {
            Id = "bukkake_exclude",
            Name = "Bukkake",
            Description = "Many finish",
            Tags = ["bukkake"],
        },
        new()
        {
            Id = "cum_play",
            Name = "Cum play",
            Description = "Fluid play",
            Tags = ["cum_play", "cum_bath"],
        },
        new()
        {
            Id = "saliva_swap",
            Name = "Saliva swap",
            Description = "Spit swap",
            Tags = ["saliva_swap"],
        },
        new()
        {
            Id = "kiss_exclude",
            Name = "Kissing",
            Description = "Kiss scenes",
            Tags = ["kiss"],
        },
        new()
        {
            Id = "romance_exclude",
            Name = "Romance",
            Description = "Soft romantic",
            Tags = ["romance", "romantic"],
        },
        new()
        {
            Id = "vanilla_exclude",
            Name = "Vanilla",
            Description = "Soft sex",
            Tags = ["vanilla"],
        },
        new()
        {
            Id = "rough_sex",
            Name = "Rough sex",
            Description = "Aggressive",
            Tags = ["rough_sex", "aggressive"],
        },
        new()
        {
            Id = "praise",
            Name = "Praise kink",
            Description = "Positive dom",
            Tags = ["praise_kink"],
        },
        new()
        {
            Id = "degradation_extra",
            Name = "Degradation extra",
            Description = "Name-calling",
            Tags = ["degradation", "name_calling"],
        },
        new()
        {
            Id = "watersports_extra",
            Name = "Watersports extra",
            Description = "Urine play",
            Tags = ["watersports", "urine"],
        },
        new()
        {
            Id = "scat_play",
            Name = "Scat play",
            Description = "Feces play",
            Tags = ["scat_play"],
        },
        new()
        {
            Id = "filth",
            Name = "Filth",
            Description = "Dirty themes",
            Tags = ["filth", "dirty"],
        },
        new()
        {
            Id = "trash",
            Name = "Trashy",
            Description = "Low class theme",
            Tags = ["trashy"],
        },
        new()
        {
            Id = "homeless",
            Name = "Homeless",
            Description = "Street themes",
            Tags = ["homeless"],
        },
        new()
        {
            Id = "poverty",
            Name = "Poverty",
            Description = "Poor themes",
            Tags = ["poverty"],
        },
        new()
        {
            Id = "fat_shaming",
            Name = "Fat shaming",
            Description = "Body shame",
            Tags = ["fat_shaming"],
        },
        new()
        {
            Id = "racism_play",
            Name = "Race play",
            Description = "Controversial",
            Tags = ["race_play"],
        },
        new()
        {
            Id = "religious_play",
            Name = "Religious play",
            Description = "Sacrilege",
            Tags = ["religious_play"],
        },
    ];
}

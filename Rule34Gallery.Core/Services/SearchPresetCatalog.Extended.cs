namespace Rule34Gallery.Core.Services;

public static partial class SearchPresetCatalog
{
    private static readonly SearchPreset[] ExtendedPresets =
    [
        new()
        {
            Id = "one_piece",
            Name = "One Piece",
            Description = "Pirates adventure",
            Tags = ["one_piece"],
        },
        new()
        {
            Id = "naruto",
            Name = "Naruto",
            Description = "Ninja world",
            Tags = ["naruto"],
        },
        new()
        {
            Id = "bleach",
            Name = "Bleach",
            Description = "Soul reapers",
            Tags = ["bleach"],
        },
        new()
        {
            Id = "dragon_ball",
            Name = "Dragon Ball",
            Description = "Martial arts",
            Tags = ["dragon_ball"],
        },
        new()
        {
            Id = "jojo",
            Name = "JoJo's Bizarre Adventure",
            Description = "Bizarre adventures",
            Tags = ["jojo's_bizarre_adventure"],
        },
        new()
        {
            Id = "mha",
            Name = "My Hero Academia",
            Description = "Hero academy",
            Tags = ["boku_no_hero_academia"],
        },
        new()
        {
            Id = "demon_slayer",
            Name = "Demon Slayer",
            Description = "Kimetsu no Yaiba",
            Tags = ["kimetsu_no_yaiba"],
        },
        new()
        {
            Id = "jjk",
            Name = "Jujutsu Kaisen",
            Description = "Cursed spirits",
            Tags = ["jujutsu_kaisen"],
        },
        new()
        {
            Id = "chainsaw_man",
            Name = "Chainsaw Man",
            Description = "Devils and chainsaws",
            Tags = ["chainsaw_man"],
        },
        new()
        {
            Id = "spy_x_family",
            Name = "Spy x Family",
            Description = "Spy family",
            Tags = ["spy_x_family"],
        },
        new()
        {
            Id = "evangelion",
            Name = "Evangelion",
            Description = "Mecha drama",
            Tags = ["neon_genesis_evangelion"],
        },
        new()
        {
            Id = "sailor_moon",
            Name = "Sailor Moon",
            Description = "Magical girls",
            Tags = ["sailor_moon"],
        },
        new()
        {
            Id = "madoka",
            Name = "Madoka Magica",
            Description = "Magical girls",
            Tags = ["mahou_shoujo_madoka_magica"],
        },
        new()
        {
            Id = "love_live",
            Name = "Love Live",
            Description = "Idol school",
            Tags = ["love_live"],
        },
        new()
        {
            Id = "idolmaster",
            Name = "The Idolmaster",
            Description = "Idol franchise",
            Tags = ["idolmaster"],
        },
        new()
        {
            Id = "blue_archive",
            Name = "Blue Archive",
            Description = "Schale students",
            Tags = ["blue_archive"],
        },
        new()
        {
            Id = "honkai_star_rail",
            Name = "Honkai: Star Rail",
            Description = "Trailblazers",
            Tags = ["honkai:_star_rail"],
        },
        new()
        {
            Id = "honkai_impact",
            Name = "Honkai Impact",
            Description = "Honkai battles",
            Tags = ["honkai_impact_3rd"],
        },
        new()
        {
            Id = "arknights",
            Name = "Arknights",
            Description = "Rhodes Island",
            Tags = ["arknights"],
        },
        new()
        {
            Id = "persona_5",
            Name = "Persona 5",
            Description = "Phantom thieves",
            Tags = ["persona_5"],
        },
        new()
        {
            Id = "persona_4",
            Name = "Persona 4",
            Description = "Investigation team",
            Tags = ["persona_4"],
        },
        new()
        {
            Id = "nier",
            Name = "NieR",
            Description = "YoRHa and androids",
            Tags = ["nier_(series)"],
        },
        new()
        {
            Id = "bayonetta",
            Name = "Bayonetta",
            Description = "Umbra witch",
            Tags = ["bayonetta"],
        },
        new()
        {
            Id = "metroid",
            Name = "Metroid",
            Description = "Samus adventures",
            Tags = ["metroid"],
        },
        new()
        {
            Id = "kirby",
            Name = "Kirby",
            Description = "Dream Land",
            Tags = ["kirby_(series)"],
        },
        new()
        {
            Id = "splatoon",
            Name = "Splatoon",
            Description = "Ink battles",
            Tags = ["splatoon_(series)"],
        },
        new()
        {
            Id = "xenoblade",
            Name = "Xenoblade",
            Description = "Chronicles",
            Tags = ["xenoblade_chronicles_(series)"],
        },
        new()
        {
            Id = "animal_crossing",
            Name = "Animal Crossing",
            Description = "Village life",
            Tags = ["animal_crossing"],
        },
        new()
        {
            Id = "digimon",
            Name = "Digimon",
            Description = "Digital monsters",
            Tags = ["digimon"],
        },
        new()
        {
            Id = "rezero",
            Name = "Re:Zero",
            Description = "Isekai loop",
            Tags = ["re:zero"],
        },
        new()
        {
            Id = "konosuba",
            Name = "Konosuba",
            Description = "Fantasy comedy",
            Tags = ["kono_subarashii_sekai_ni_shukufuku_wo!"],
        },
        new()
        {
            Id = "overlord_anime",
            Name = "Overlord",
            Description = "Nazarick",
            Tags = ["overlord_(maruyama)"],
        },
        new()
        {
            Id = "highschool_dxd",
            Name = "High School DxD",
            Description = "Devils and angels",
            Tags = ["highschool_dxd"],
        },
        new()
        {
            Id = "date_a_live",
            Name = "Date A Live",
            Description = "Spirits",
            Tags = ["date_a_live"],
        },
        new()
        {
            Id = "tolove_ru",
            Name = "To Love-Ru",
            Description = "Alien harem",
            Tags = ["to_love-ru"],
        },
        new()
        {
            Id = "steins_gate",
            Name = "Steins;Gate",
            Description = "Time travel",
            Tags = ["steins;gate"],
        },
        new()
        {
            Id = "monogatari",
            Name = "Monogatari",
            Description = "Oddities",
            Tags = ["monogatari_(series)"],
        },
        new()
        {
            Id = "oshi_no_ko",
            Name = "Oshi no Ko",
            Description = "Idol drama",
            Tags = ["oshi_no_ko"],
        },
        new()
        {
            Id = "frieren",
            Name = "Frieren",
            Description = "After the journey",
            Tags = ["sousou_no_frieren"],
        },
        new()
        {
            Id = "dungeon_meshi",
            Name = "Dungeon Meshi",
            Description = "Dungeon dining",
            Tags = ["dungeon_meshi"],
        },
        new()
        {
            Id = "mob_psycho",
            Name = "Mob Psycho 100",
            Description = "Psychic powers",
            Tags = ["mob_psycho_100"],
        },
        new()
        {
            Id = "opm",
            Name = "One Punch Man",
            Description = "Hero association",
            Tags = ["one-punch_man"],
        },
        new()
        {
            Id = "hxh",
            Name = "Hunter x Hunter",
            Description = "Hunters",
            Tags = ["hunter_x_hunter"],
        },
        new()
        {
            Id = "black_clover",
            Name = "Black Clover",
            Description = "Magic knights",
            Tags = ["black_clover"],
        },
        new()
        {
            Id = "fairy_tail",
            Name = "Fairy Tail",
            Description = "Guild mages",
            Tags = ["fairy_tail"],
        },
        new()
        {
            Id = "rwby",
            Name = "RWBY",
            Description = "Huntresses",
            Tags = ["rwby"],
        },
        new()
        {
            Id = "precure",
            Name = "Precure",
            Description = "Magical warriors",
            Tags = ["precure"],
        },
        new()
        {
            Id = "gundam",
            Name = "Gundam",
            Description = "Mecha wars",
            Tags = ["gundam"],
        },
        new()
        {
            Id = "fullmetal_alchemist",
            Name = "Fullmetal Alchemist",
            Description = "Alchemy",
            Tags = ["fullmetal_alchemist"],
        },
        new()
        {
            Id = "code_geass",
            Name = "Code Geass",
            Description = "Geass",
            Tags = ["code_geass"],
        },
        new()
        {
            Id = "vocaloid",
            Name = "Vocaloid",
            Description = "Virtual singers",
            Tags = ["vocaloid"],
        },
        new()
        {
            Id = "uma_musume",
            Name = "Uma Musume",
            Description = "Horse girls",
            Tags = ["umamusume"],
        },
        new()
        {
            Id = "project_sekai",
            Name = "Project Sekai",
            Description = "Rhythm idols",
            Tags = ["project_sekai"],
        },
        new()
        {
            Id = "bang_dream",
            Name = "BanG Dream",
            Description = "Band girls",
            Tags = ["bang_dream!"],
        },
        new()
        {
            Id = "granblue",
            Name = "Granblue Fantasy",
            Description = "Sky journey",
            Tags = ["granblue_fantasy"],
        },
        new()
        {
            Id = "kingdom_hearts",
            Name = "Kingdom Hearts",
            Description = "Keyblade",
            Tags = ["kingdom_hearts"],
        },
        new()
        {
            Id = "senran_kagura",
            Name = "Senran Kagura",
            Description = "Shinobi",
            Tags = ["senran_kagura"],
        },
        new()
        {
            Id = "dead_or_alive",
            Name = "Dead or Alive",
            Description = "Fighters",
            Tags = ["dead_or_alive"],
        },
        new()
        {
            Id = "soul_calibur",
            Name = "Soulcalibur",
            Description = "Weapons",
            Tags = ["soulcalibur"],
        },
        new()
        {
            Id = "king_of_fighters",
            Name = "King of Fighters",
            Description = "SNK fighters",
            Tags = ["the_king_of_fighters"],
        },
        new()
        {
            Id = "mortal_kombat",
            Name = "Mortal Kombat",
            Description = "Kombat",
            Tags = ["mortal_kombat"],
        },
        new()
        {
            Id = "guilty_gear",
            Name = "Guilty Gear",
            Description = "Rock music fighters",
            Tags = ["guilty_gear"],
        },
        new()
        {
            Id = "blazblue",
            Name = "BlazBlue",
            Description = "Azure",
            Tags = ["blazblue"],
        },
        new()
        {
            Id = "skullgirls",
            Name = "Skullgirls",
            Description = "New Meridian",
            Tags = ["skullgirls"],
        },
        new()
        {
            Id = "dragon_quest",
            Name = "Dragon Quest",
            Description = "Slime heroes",
            Tags = ["dragon_quest"],
        },
        new()
        {
            Id = "ff7",
            Name = "Final Fantasy VII",
            Description = "Midgar",
            Tags = ["final_fantasy_vii"],
        },
        new()
        {
            Id = "ff14",
            Name = "Final Fantasy XIV",
            Description = "Eorzea",
            Tags = ["final_fantasy_xiv"],
        },
        new()
        {
            Id = "yakuza",
            Name = "Yakuza",
            Description = "Kamurocho",
            Tags = ["yakuza"],
        },
        new()
        {
            Id = "danganronpa",
            Name = "Danganronpa",
            Description = "Hope and despair",
            Tags = ["danganronpa_(series)"],
        },
        new()
        {
            Id = "kill_la_kill",
            Name = "Kill la Kill",
            Description = "Life fibers",
            Tags = ["kill_la_kill"],
        },
        new()
        {
            Id = "gurren_lagann",
            Name = "Gurren Lagann",
            Description = "Drill",
            Tags = ["tengen_toppa_gurren-lagann"],
        },
        new()
        {
            Id = "gravity_falls",
            Name = "Gravity Falls",
            Description = "Mystery shack",
            Tags = ["gravity_falls"],
        },
        new()
        {
            Id = "avatar_tla",
            Name = "Avatar: The Last Airbender",
            Description = "Bending",
            Tags = ["avatar:_the_last_airbender"],
        },
        new()
        {
            Id = "ben_10",
            Name = "Ben 10",
            Description = "Omnitrix",
            Tags = ["ben_10"],
        },
        new()
        {
            Id = "totally_spies",
            Name = "Totally Spies",
            Description = "Spies",
            Tags = ["totally_spies"],
        },
        new()
        {
            Id = "adventure_time",
            Name = "Adventure Time",
            Description = "Land of Ooo",
            Tags = ["adventure_time"],
        },
        new()
        {
            Id = "steven_universe",
            Name = "Steven Universe",
            Description = "Gems",
            Tags = ["steven_universe"],
        },
        new()
        {
            Id = "mlp",
            Name = "My Little Pony",
            Description = "Friendship",
            Tags = ["my_little_pony"],
        },
        new()
        {
            Id = "warcraft",
            Name = "Warcraft",
            Description = "Azeroth",
            Tags = ["warcraft"],
        },
        new()
        {
            Id = "wow",
            Name = "World of Warcraft",
            Description = "MMO fantasy",
            Tags = ["world_of_warcraft"],
        },
        new()
        {
            Id = "diablo",
            Name = "Diablo",
            Description = "Sanctuary",
            Tags = ["diablo_(series)"],
        },
        new()
        {
            Id = "elder_scrolls",
            Name = "The Elder Scrolls",
            Description = "Tamriel",
            Tags = ["the_elder_scrolls"],
        },
        new()
        {
            Id = "skyrim",
            Name = "Skyrim",
            Description = "Dragonborn",
            Tags = ["the_elder_scrolls_v:_skyrim"],
        },
        new()
        {
            Id = "fallout",
            Name = "Fallout",
            Description = "Wasteland",
            Tags = ["fallout_(series)"],
        },
        new()
        {
            Id = "mass_effect",
            Name = "Mass Effect",
            Description = "Galaxy",
            Tags = ["mass_effect_(series)"],
        },
        new()
        {
            Id = "halo",
            Name = "Halo",
            Description = "Spartans",
            Tags = ["halo_(series)"],
        },
        new()
        {
            Id = "apex_legends",
            Name = "Apex Legends",
            Description = "Battle royale",
            Tags = ["apex_legends"],
        },
        new()
        {
            Id = "valorant",
            Name = "Valorant",
            Description = "Agents",
            Tags = ["valorant"],
        },
        new()
        {
            Id = "half_life",
            Name = "Half-Life",
            Description = "Combine",
            Tags = ["half-life_(series)"],
        },
        new()
        {
            Id = "portal",
            Name = "Portal",
            Description = "Aperture",
            Tags = ["portal_(series)"],
        },
        new()
        {
            Id = "bioshock",
            Name = "BioShock",
            Description = "Rapture",
            Tags = ["bioshock_(series)"],
        },
        new()
        {
            Id = "assassins_creed",
            Name = "Assassin's Creed",
            Description = "Assassins",
            Tags = ["assassin's_creed_(series)"],
        },
        new()
        {
            Id = "god_of_war",
            Name = "God of War",
            Description = "Norse myth",
            Tags = ["god_of_war"],
        },
        new()
        {
            Id = "cyberpunk_2077",
            Name = "Cyberpunk 2077",
            Description = "Night City",
            Tags = ["cyberpunk_2077"],
        },
        new()
        {
            Id = "witcher",
            Name = "The Witcher",
            Description = "Monster hunter",
            Tags = ["the_witcher_(series)"],
        },
        new()
        {
            Id = "elden_ring",
            Name = "Elden Ring",
            Description = "Lands Between",
            Tags = ["elden_ring"],
        },
        new()
        {
            Id = "dark_souls",
            Name = "Dark Souls",
            Description = "Soulslike",
            Tags = ["dark_souls_(series)"],
        },
        new()
        {
            Id = "bloodborne",
            Name = "Bloodborne",
            Description = "Yharnam",
            Tags = ["bloodborne"],
        },
        new()
        {
            Id = "sekiro",
            Name = "Sekiro",
            Description = "Shinobi",
            Tags = ["sekiro:_shadows_die_twice"],
        },
        new()
        {
            Id = "monster_hunter",
            Name = "Monster Hunter",
            Description = "Hunts",
            Tags = ["monster_hunter_(series)"],
        },
        new()
        {
            Id = "devil_may_cry",
            Name = "Devil May Cry",
            Description = "Demons",
            Tags = ["devil_may_cry_(series)"],
        },
        new()
        {
            Id = "castlevania",
            Name = "Castlevania",
            Description = "Vampires",
            Tags = ["castlevania_(series)"],
        },
        new()
        {
            Id = "hollow_knight",
            Name = "Hollow Knight",
            Description = "Hallownest",
            Tags = ["hollow_knight"],
        },
        new()
        {
            Id = "deltarune",
            Name = "Deltarune",
            Description = "Dark world",
            Tags = ["deltarune"],
        },
        new()
        {
            Id = "cuphead",
            Name = "Cuphead",
            Description = "Inkwell",
            Tags = ["cuphead_(game)"],
        },
        new()
        {
            Id = "fnaf",
            Name = "Five Nights at Freddy's",
            Description = "Animatronics",
            Tags = ["five_nights_at_freddy's"],
        },
        new()
        {
            Id = "terraria",
            Name = "Terraria",
            Description = "Sandbox",
            Tags = ["terraria"],
        },
        new()
        {
            Id = "stardew_valley",
            Name = "Stardew Valley",
            Description = "Farm life",
            Tags = ["stardew_valley"],
        },
        new()
        {
            Id = "roblox",
            Name = "Roblox",
            Description = "Platform",
            Tags = ["roblox"],
        },
        new()
        {
            Id = "among_us",
            Name = "Among Us",
            Description = "Crewmates",
            Tags = ["among_us"],
        },
        new()
        {
            Id = "plants_vs_zombies",
            Name = "Plants vs. Zombies",
            Description = "Tower defense",
            Tags = ["plants_vs._zombies"],
        },
        new()
        {
            Id = "dc_comics",
            Name = "DC Comics",
            Description = "DC heroes",
            Tags = ["dc_comics"],
        },
        new()
        {
            Id = "batman",
            Name = "Batman",
            Description = "Gotham",
            Tags = ["batman_(series)"],
        },
        new()
        {
            Id = "superman",
            Name = "Superman",
            Description = "Kryptonian",
            Tags = ["superman_(series)"],
        },
        new()
        {
            Id = "spider_man",
            Name = "Spider-Man",
            Description = "Web-slinger",
            Tags = ["spider-man_(series)"],
        },
        new()
        {
            Id = "x_men",
            Name = "X-Men",
            Description = "Mutants",
            Tags = ["x-men"],
        },
        new()
        {
            Id = "invincible",
            Name = "Invincible",
            Description = "Image hero",
            Tags = ["invincible_(series)"],
        },
        new()
        {
            Id = "helluva_boss",
            Name = "Helluva Boss",
            Description = "Imp company",
            Tags = ["helluva_boss"],
        },
        new()
        {
            Id = "hazbin_hotel",
            Name = "Hazbin Hotel",
            Description = "Redemption hotel",
            Tags = ["hazbin_hotel"],
        },
        new()
        {
            Id = "arcane",
            Name = "Arcane",
            Description = "League show",
            Tags = ["arcane:_league_of_legends"],
        },
        new()
        {
            Id = "the_last_of_us",
            Name = "The Last of Us",
            Description = "Survival",
            Tags = ["the_last_of_us"],
        },
        new()
        {
            Id = "bioshock_elizabeth",
            Name = "BioShock Infinite",
            Description = "Columbia",
            Tags = ["bioshock_infinite"],
        },
        new()
        {
            Id = "nier_automata",
            Name = "NieR: Automata",
            Description = "YoRHa",
            Tags = ["nier:_automata"],
        },
        new()
        {
            Id = "nier_replicant",
            Name = "NieR Replicant",
            Description = "Replicant",
            Tags = ["nier_replicant"],
        },
        new()
        {
            Id = "dragon_age",
            Name = "Dragon Age",
            Description = "Thedas",
            Tags = ["dragon_age_(series)"],
        },
        new()
        {
            Id = "mass_effect_liara",
            Name = "Mass Effect Liara",
            Description = "Asari",
            Tags = ["mass_effect_(series)", "liara_t'soni"],
        },
        new()
        {
            Id = "borderlands",
            Name = "Borderlands",
            Description = "Vault hunters",
            Tags = ["borderlands_(series)"],
        },
        new()
        {
            Id = "doom",
            Name = "DOOM",
            Description = "Slayer",
            Tags = ["doom_(series)"],
        },
        new()
        {
            Id = "metroid_samus",
            Name = "Samus Aran",
            Description = "Bounty hunter",
            Tags = ["metroid", "samus_aran"],
        },
        new()
        {
            Id = "zelda_link",
            Name = "Link",
            Description = "Hero of Hyrule",
            Tags = ["the_legend_of_zelda", "link"],
        },
        new()
        {
            Id = "mario",
            Name = "Super Mario",
            Description = "Mushroom Kingdom",
            Tags = ["super_mario_bros."],
        },
        new()
        {
            Id = "pokemon_trainer",
            Name = "Pokémon trainer",
            Description = "Trainer art",
            Tags = ["pokemon", "pokemon_trainer"],
        },
        new()
        {
            Id = "genshin_traveler",
            Name = "Genshin Traveler",
            Description = "Traveler",
            Tags = ["genshin_impact", "traveler_(genshin_impact)"],
        },
        new()
        {
            Id = "cowgirl",
            Name = "Cowgirl",
            Description = "Riding position",
            Tags = ["cowgirl_position"],
        },
        new()
        {
            Id = "doggystyle",
            Name = "Doggystyle",
            Description = "From behind",
            Tags = ["doggystyle"],
        },
        new()
        {
            Id = "missionary",
            Name = "Missionary",
            Description = "Face to face",
            Tags = ["missionary"],
        },
        new()
        {
            Id = "reverse_cowgirl",
            Name = "Reverse cowgirl",
            Description = "Facing away",
            Tags = ["reverse_cowgirl"],
        },
        new()
        {
            Id = "mating_press",
            Name = "Mating press",
            Description = "Pinned",
            Tags = ["mating_press"],
        },
        new()
        {
            Id = "full_nelson",
            Name = "Full nelson",
            Description = "Held position",
            Tags = ["full_nelson"],
        },
        new()
        {
            Id = "sixty_nine",
            Name = "69",
            Description = "Mutual oral",
            Tags = ["69"],
        },
        new()
        {
            Id = "handjob",
            Name = "Handjob",
            Description = "Manual",
            Tags = ["handjob"],
        },
        new()
        {
            Id = "footjob",
            Name = "Footjob",
            Description = "Feet",
            Tags = ["footjob"],
        },
        new()
        {
            Id = "cunnilingus",
            Name = "Cunnilingus",
            Description = "Oral on female",
            Tags = ["cunnilingus"],
        },
        new()
        {
            Id = "facesitting",
            Name = "Facesitting",
            Description = "Sitting on face",
            Tags = ["facesitting"],
        },
        new()
        {
            Id = "deepthroat",
            Name = "Deepthroat",
            Description = "Oral depth",
            Tags = ["deepthroat"],
        },
        new()
        {
            Id = "female_masturbation",
            Name = "Female masturbation",
            Description = "Solo play",
            Tags = ["female_masturbation"],
        },
        new()
        {
            Id = "male_masturbation",
            Name = "Male masturbation",
            Description = "Solo play",
            Tags = ["male_masturbation"],
        },
        new()
        {
            Id = "dildo",
            Name = "Dildo",
            Description = "Toy",
            Tags = ["dildo"],
        },
        new()
        {
            Id = "vibrator",
            Name = "Vibrator",
            Description = "Toy",
            Tags = ["vibrator"],
        },
        new()
        {
            Id = "creampie",
            Name = "Creampie",
            Description = "Internal finish",
            Tags = ["creampie"],
        },
        new()
        {
            Id = "facial",
            Name = "Facial",
            Description = "Face finish",
            Tags = ["facial"],
        },
        new()
        {
            Id = "cum_on_body",
            Name = "Cum on body",
            Description = "Body finish",
            Tags = ["cum_on_body"],
        },
        new()
        {
            Id = "bukkake",
            Name = "Bukkake",
            Description = "Multiple finish",
            Tags = ["bukkake"],
        },
        new()
        {
            Id = "gangbang",
            Name = "Gangbang",
            Description = "Many partners",
            Tags = ["gangbang"],
        },
        new()
        {
            Id = "double_penetration",
            Name = "Double penetration",
            Description = "DP",
            Tags = ["double_penetration"],
        },
        new()
        {
            Id = "spitroast",
            Name = "Spitroast",
            Description = "Two on one",
            Tags = ["spitroast"],
        },
        new()
        {
            Id = "public_sex",
            Name = "Public sex",
            Description = "In public",
            Tags = ["public", "sex"],
        },
        new()
        {
            Id = "outdoor_sex",
            Name = "Outdoor sex",
            Description = "Outside",
            Tags = ["outdoors", "sex"],
        },
        new()
        {
            Id = "shower_sex",
            Name = "Shower sex",
            Description = "In shower",
            Tags = ["shower", "sex"],
        },
        new()
        {
            Id = "onsen",
            Name = "Onsen",
            Description = "Hot spring",
            Tags = ["onsen"],
        },
        new()
        {
            Id = "classroom",
            Name = "Classroom",
            Description = "School setting",
            Tags = ["classroom"],
        },
        new()
        {
            Id = "office_lady",
            Name = "Office lady",
            Description = "OL",
            Tags = ["office_lady"],
        },
        new()
        {
            Id = "bunny_girl",
            Name = "Bunny girl",
            Description = "Playboy bunny",
            Tags = ["bunny_girl"],
        },
        new()
        {
            Id = "angel",
            Name = "Angel",
            Description = "Angel character",
            Tags = ["angel"],
        },
        new()
        {
            Id = "witch",
            Name = "Witch",
            Description = "Witch character",
            Tags = ["witch"],
        },
        new()
        {
            Id = "nun",
            Name = "Nun",
            Description = "Nun outfit",
            Tags = ["nun"],
        },
        new()
        {
            Id = "police",
            Name = "Police",
            Description = "Officer",
            Tags = ["policewoman"],
        },
        new()
        {
            Id = "military_uniform",
            Name = "Military",
            Description = "Uniform",
            Tags = ["military"],
        },
        new()
        {
            Id = "kimono",
            Name = "Kimono",
            Description = "Traditional",
            Tags = ["kimono"],
        },
        new()
        {
            Id = "china_dress",
            Name = "China dress",
            Description = "Cheongsam",
            Tags = ["china_dress"],
        },
        new()
        {
            Id = "wedding_dress",
            Name = "Wedding dress",
            Description = "Bridal",
            Tags = ["wedding_dress"],
        },
        new()
        {
            Id = "lingerie",
            Name = "Lingerie",
            Description = "Underwear",
            Tags = ["lingerie"],
        },
        new()
        {
            Id = "panties",
            Name = "Panties",
            Description = "Underwear",
            Tags = ["panties"],
        },
        new()
        {
            Id = "bra",
            Name = "Bra",
            Description = "Underwear",
            Tags = ["bra"],
        },
        new()
        {
            Id = "skirt_lift",
            Name = "Skirt lift",
            Description = "Lifted skirt",
            Tags = ["skirt_lift"],
        },
        new()
        {
            Id = "shirt_lift",
            Name = "Shirt lift",
            Description = "Lifted shirt",
            Tags = ["shirt_lift"],
        },
        new()
        {
            Id = "undressing",
            Name = "Undressing",
            Description = "Stripping",
            Tags = ["undressing"],
        },
        new()
        {
            Id = "nude",
            Name = "Nude",
            Description = "Fully nude",
            Tags = ["nude"],
        },
        new()
        {
            Id = "partially_clothed",
            Name = "Partially clothed",
            Description = "Some clothes",
            Tags = ["partially_clothed"],
        },
        new()
        {
            Id = "garter_belt",
            Name = "Garter belt",
            Description = "Lingerie",
            Tags = ["garter_belt"],
        },
        new()
        {
            Id = "high_heels",
            Name = "High heels",
            Description = "Heels",
            Tags = ["high_heels"],
        },
        new()
        {
            Id = "blonde_hair",
            Name = "Blonde hair",
            Description = "Hair color",
            Tags = ["blonde_hair"],
        },
        new()
        {
            Id = "brown_hair",
            Name = "Brown hair",
            Description = "Hair color",
            Tags = ["brown_hair"],
        },
        new()
        {
            Id = "red_hair",
            Name = "Red hair",
            Description = "Hair color",
            Tags = ["red_hair"],
        },
        new()
        {
            Id = "blue_hair",
            Name = "Blue hair",
            Description = "Hair color",
            Tags = ["blue_hair"],
        },
        new()
        {
            Id = "pink_hair",
            Name = "Pink hair",
            Description = "Hair color",
            Tags = ["pink_hair"],
        },
        new()
        {
            Id = "white_hair",
            Name = "White hair",
            Description = "Hair color",
            Tags = ["white_hair"],
        },
        new()
        {
            Id = "long_hair",
            Name = "Long hair",
            Description = "Hair length",
            Tags = ["long_hair"],
        },
        new()
        {
            Id = "short_hair",
            Name = "Short hair",
            Description = "Hair length",
            Tags = ["short_hair"],
        },
        new()
        {
            Id = "twintails",
            Name = "Twintails",
            Description = "Hairstyle",
            Tags = ["twintails"],
        },
        new()
        {
            Id = "ponytail",
            Name = "Ponytail",
            Description = "Hairstyle",
            Tags = ["ponytail"],
        },
        new()
        {
            Id = "braid",
            Name = "Braid",
            Description = "Hairstyle",
            Tags = ["braid"],
        },
        new()
        {
            Id = "large_breasts",
            Name = "Large breasts",
            Description = "Big chest",
            Tags = ["large_breasts"],
        },
        new()
        {
            Id = "medium_breasts",
            Name = "Medium breasts",
            Description = "Mid chest",
            Tags = ["medium_breasts"],
        },
        new()
        {
            Id = "small_breasts",
            Name = "Small breasts",
            Description = "Small chest",
            Tags = ["small_breasts"],
        },
        new()
        {
            Id = "wide_hips",
            Name = "Wide hips",
            Description = "Curvy hips",
            Tags = ["wide_hips"],
        },
        new()
        {
            Id = "thick_thighs",
            Name = "Thick thighs",
            Description = "Thicc",
            Tags = ["thick_thighs"],
        },
        new()
        {
            Id = "plump",
            Name = "Plump",
            Description = "Soft body",
            Tags = ["plump"],
        },
        new()
        {
            Id = "petite",
            Name = "Petite",
            Description = "Small frame",
            Tags = ["petite"],
        },
        new()
        {
            Id = "mature_female",
            Name = "Mature female",
            Description = "Older woman",
            Tags = ["mature_female"],
        },
        new()
        {
            Id = "milf",
            Name = "MILF",
            Description = "Mature",
            Tags = ["milf"],
        },
        new()
        {
            Id = "shortstack",
            Name = "Shortstack",
            Description = "Short adult",
            Tags = ["shortstack"],
        },
        new()
        {
            Id = "tall_female",
            Name = "Tall female",
            Description = "Tall",
            Tags = ["tall_female"],
        },
        new()
        {
            Id = "toned",
            Name = "Toned body",
            Description = "Fit",
            Tags = ["toned"],
        },
        new()
        {
            Id = "sweat",
            Name = "Sweaty",
            Description = "Sweat",
            Tags = ["sweat"],
        },
        new()
        {
            Id = "wet",
            Name = "Wet",
            Description = "Wet body",
            Tags = ["wet"],
        },
        new()
        {
            Id = "oiled",
            Name = "Oiled",
            Description = "Oil shine",
            Tags = ["oiled"],
        },
        new()
        {
            Id = "tan",
            Name = "Tan",
            Description = "Tanned skin",
            Tags = ["tan"],
        },
        new()
        {
            Id = "freckles",
            Name = "Freckles",
            Description = "Face freckles",
            Tags = ["freckles"],
        },
        new()
        {
            Id = "tattoo",
            Name = "Tattoo",
            Description = "Body ink",
            Tags = ["tattoo"],
        },
        new()
        {
            Id = "piercing",
            Name = "Piercing",
            Description = "Body piercing",
            Tags = ["piercing"],
        },
        new()
        {
            Id = "looking_at_viewer",
            Name = "Looking at viewer",
            Description = "Eye contact",
            Tags = ["looking_at_viewer"],
        },
        new()
        {
            Id = "smile",
            Name = "Smiling",
            Description = "Happy",
            Tags = ["smile"],
        },
        new()
        {
            Id = "blush",
            Name = "Blush",
            Description = "Embarrassed",
            Tags = ["blush"],
        },
        new()
        {
            Id = "ahegao",
            Name = "Ahegao",
            Description = "Face",
            Tags = ["ahegao"],
        },
        new()
        {
            Id = "femdom_search",
            Name = "Femdom",
            Description = "Female dom",
            Tags = ["femdom"],
        },
        new()
        {
            Id = "bondage_search",
            Name = "Bondage",
            Description = "Tied up",
            Tags = ["bondage"],
        },
        new()
        {
            Id = "bdsm_search",
            Name = "BDSM",
            Description = "Kink",
            Tags = ["bdsm"],
        },
        new()
        {
            Id = "frottage",
            Name = "Frottage",
            Description = "Grinding",
            Tags = ["frottage"],
        },
        new()
        {
            Id = "tribadism",
            Name = "Tribadism",
            Description = "Girl grinding",
            Tags = ["tribadism"],
        },
        new()
        {
            Id = "fingering",
            Name = "Fingering",
            Description = "Manual",
            Tags = ["fingering"],
        },
        new()
        {
            Id = "squirting",
            Name = "Squirting",
            Description = "Squirt",
            Tags = ["squirting"],
        },
        new()
        {
            Id = "orgasm",
            Name = "Orgasm",
            Description = "Climax",
            Tags = ["orgasm"],
        },
        new()
        {
            Id = "after_sex",
            Name = "After sex",
            Description = "Post",
            Tags = ["after_sex"],
        },
        new()
        {
            Id = "before_sex",
            Name = "Before sex",
            Description = "Pre",
            Tags = ["before_sex"],
        },
        new()
        {
            Id = "clothed_sex",
            Name = "Clothed sex",
            Description = "With clothes",
            Tags = ["clothed_sex"],
        },
        new()
        {
            Id = "imminent_sex",
            Name = "Imminent sex",
            Description = "About to",
            Tags = ["imminent_sex"],
        },
        new()
        {
            Id = "under_table",
            Name = "Under table",
            Description = "Hidden",
            Tags = ["under_table"],
        },
        new()
        {
            Id = "library",
            Name = "Library",
            Description = "Quiet place",
            Tags = ["library"],
        },
        new()
        {
            Id = "car_interior",
            Name = "Car sex",
            Description = "Vehicle",
            Tags = ["car_interior"],
        },
        new()
        {
            Id = "train_interior",
            Name = "Train",
            Description = "Transit",
            Tags = ["train_interior"],
        },
        new()
        {
            Id = "locker_room",
            Name = "Locker room",
            Description = "Changing",
            Tags = ["locker_room"],
        },
        new()
        {
            Id = "gym",
            Name = "Gym",
            Description = "Workout",
            Tags = ["gym"],
        },
        new()
        {
            Id = "pool",
            Name = "Pool",
            Description = "Swimming",
            Tags = ["pool"],
        },
        new()
        {
            Id = "beach",
            Name = "Beach",
            Description = "Sand and sea",
            Tags = ["beach"],
        },
        new()
        {
            Id = "forest",
            Name = "Forest",
            Description = "Woods",
            Tags = ["forest"],
        },
        new()
        {
            Id = "night",
            Name = "Night",
            Description = "Dark sky",
            Tags = ["night"],
        },
        new()
        {
            Id = "moonlight",
            Name = "Moonlight",
            Description = "Night light",
            Tags = ["moonlight"],
        },
        new()
        {
            Id = "rain",
            Name = "Rain",
            Description = "Wet weather",
            Tags = ["rain"],
        },
        new()
        {
            Id = "snow",
            Name = "Snow",
            Description = "Winter",
            Tags = ["snow"],
        },
        new()
        {
            Id = "christmas",
            Name = "Christmas",
            Description = "Holiday",
            Tags = ["christmas"],
        },
        new()
        {
            Id = "halloween",
            Name = "Halloween",
            Description = "Spooky",
            Tags = ["halloween"],
        },
        new()
        {
            Id = "valentine",
            Name = "Valentine",
            Description = "Romance",
            Tags = ["valentine"],
        },
        new()
        {
            Id = "new_year",
            Name = "New Year",
            Description = "Holiday",
            Tags = ["new_year"],
        },
        new()
        {
            Id = "summer",
            Name = "Summer",
            Description = "Warm season",
            Tags = ["summer"],
        },
        new()
        {
            Id = "winter",
            Name = "Winter",
            Description = "Cold season",
            Tags = ["winter"],
        },
        new()
        {
            Id = "spring",
            Name = "Spring",
            Description = "Season",
            Tags = ["spring"],
        },
        new()
        {
            Id = "autumn",
            Name = "Autumn",
            Description = "Fall",
            Tags = ["autumn"],
        },
        new()
        {
            Id = "monochrome",
            Name = "Monochrome",
            Description = "B&W art",
            Tags = ["monochrome"],
        },
        new()
        {
            Id = "sepia",
            Name = "Sepia",
            Description = "Sepia tone",
            Tags = ["sepia"],
        },
        new()
        {
            Id = "watercolor",
            Name = "Watercolor",
            Description = "Traditional",
            Tags = ["watercolor_(medium)"],
        },
        new()
        {
            Id = "oil_painting",
            Name = "Oil painting",
            Description = "Traditional",
            Tags = ["oil_painting_(medium)"],
        },
        new()
        {
            Id = "chibi_search",
            Name = "Chibi",
            Description = "Super-deformed",
            Tags = ["chibi"],
        },
        new()
        {
            Id = "parody",
            Name = "Parody",
            Description = "Parody tag",
            Tags = ["parody"],
        },
        new()
        {
            Id = "crossover",
            Name = "Crossover",
            Description = "Mixed series",
            Tags = ["crossover"],
        },
        new()
        {
            Id = "meme",
            Name = "Meme",
            Description = "Meme art",
            Tags = ["meme"],
        },
        new()
        {
            Id = "4koma",
            Name = "4koma",
            Description = "Comic strip",
            Tags = ["4koma"],
        },
        new()
        {
            Id = "multiple_views",
            Name = "Multiple views",
            Description = "Angles",
            Tags = ["multiple_views"],
        },
        new()
        {
            Id = "speech_bubble",
            Name = "Speech bubble",
            Description = "Dialogue",
            Tags = ["speech_bubble"],
        },
        new()
        {
            Id = "english_text",
            Name = "English text",
            Description = "Text",
            Tags = ["english_text"],
        },
        new()
        {
            Id = "japanese_text",
            Name = "Japanese text",
            Description = "Text",
            Tags = ["japanese_text"],
        },
        new()
        {
            Id = "sound",
            Name = "With sound",
            Description = "Audio",
            Tags = ["sound"],
        },
        new()
        {
            Id = "webm_search",
            Name = "WebM",
            Description = "WebM format",
            Tags = ["webm"],
        },
        new()
        {
            Id = "absurdres",
            Name = "Absurd resolution",
            Description = "Very high res",
            Tags = ["absurdres"],
        },
        new()
        {
            Id = "game_cg",
            Name = "Game CG",
            Description = "Visual novel",
            Tags = ["game_cg"],
        },
        new()
        {
            Id = "visual_novel",
            Name = "Visual novel",
            Description = "VN art",
            Tags = ["visual_novel"],
        },
        new()
        {
            Id = "official_art",
            Name = "Official art",
            Description = "Official",
            Tags = ["official_art"],
        },
        new()
        {
            Id = "fanart",
            Name = "Fan art",
            Description = "Fanmade",
            Tags = ["fanart"],
        },
        new()
        {
            Id = "cosplay_photo",
            Name = "Cosplay photo",
            Description = "Real cosplay",
            Tags = ["cosplay_photo"],
        },
        new()
        {
            Id = "realistic_search",
            Name = "Realistic",
            Description = "Semi-real",
            Tags = ["realistic"],
        },
        new()
        {
            Id = "photo_medium",
            Name = "Photo",
            Description = "Photograph",
            Tags = ["photo_(medium)"],
        },
        new()
        {
            Id = "full_color",
            Name = "Full color",
            Description = "Colored",
            Tags = ["full_color"],
        },
        new()
        {
            Id = "greyscale",
            Name = "Greyscale",
            Description = "Grayscale",
            Tags = ["greyscale"],
        },
        new()
        {
            Id = "2girls",
            Name = "Two girls",
            Description = "Pair of girls",
            Tags = ["2girls"],
        },
        new()
        {
            Id = "3girls",
            Name = "Three girls",
            Description = "Trio",
            Tags = ["3girls"],
        },
        new()
        {
            Id = "4girls",
            Name = "Four girls",
            Description = "Quartet",
            Tags = ["4girls"],
        },
        new()
        {
            Id = "1girl",
            Name = "One girl",
            Description = "Single girl",
            Tags = ["1girl"],
        },
        new()
        {
            Id = "1boy",
            Name = "One boy",
            Description = "Single boy",
            Tags = ["1boy"],
        },
        new()
        {
            Id = "2boys",
            Name = "Two boys",
            Description = "Pair of boys",
            Tags = ["2boys"],
        },
        new()
        {
            Id = "mixed_bathing",
            Name = "Mixed bathing",
            Description = "Shared bath",
            Tags = ["mixed_bathing"],
        },
        new()
        {
            Id = "bath",
            Name = "Bath",
            Description = "Bathtub",
            Tags = ["bath"],
        },
        new()
        {
            Id = "hot_spring",
            Name = "Hot spring",
            Description = "Onsen scene",
            Tags = ["hot_spring"],
        },
        new()
        {
            Id = "swimsuit",
            Name = "Swimsuit",
            Description = "Swimwear",
            Tags = ["swimsuit"],
        },
        new()
        {
            Id = "one_piece_swimsuit",
            Name = "One-piece swimsuit",
            Description = "Swimwear",
            Tags = ["one-piece_swimsuit"],
        },
        new()
        {
            Id = "bikini_top",
            Name = "Bikini top only",
            Description = "Partial swim",
            Tags = ["bikini_top_only"],
        },
        new()
        {
            Id = "sideboob",
            Name = "Sideboob",
            Description = "Side exposure",
            Tags = ["sideboob"],
        },
        new()
        {
            Id = "underboob",
            Name = "Underboob",
            Description = "Under exposure",
            Tags = ["underboob"],
        },
        new()
        {
            Id = "cleavage",
            Name = "Cleavage",
            Description = "Chest line",
            Tags = ["cleavage"],
        },
        new()
        {
            Id = "nipples",
            Name = "Visible nipples",
            Description = "Exposed",
            Tags = ["nipples"],
        },
        new()
        {
            Id = "pussy",
            Name = "Visible pussy",
            Description = "Exposed",
            Tags = ["pussy"],
        },
        new()
        {
            Id = "penis",
            Name = "Visible penis",
            Description = "Exposed",
            Tags = ["penis"],
        },
        new()
        {
            Id = "spread_legs",
            Name = "Spread legs",
            Description = "Pose",
            Tags = ["spread_legs"],
        },
        new()
        {
            Id = "ass",
            Name = "Ass focus",
            Description = "Rear",
            Tags = ["ass"],
        },
        new()
        {
            Id = "ass_visible",
            Name = "Ass visible",
            Description = "Rear view",
            Tags = ["ass_visible_through_thighs"],
        },
        new()
        {
            Id = "thigh_gap",
            Name = "Thigh gap",
            Description = "Legs",
            Tags = ["thigh_gap"],
        },
        new()
        {
            Id = "pantyshot",
            Name = "Pantyshot",
            Description = "Upskirt",
            Tags = ["pantyshot"],
        },
        new()
        {
            Id = "upskirt",
            Name = "Upskirt",
            Description = "Under skirt",
            Tags = ["upskirt"],
        },
        new()
        {
            Id = "wardrobe_malfunction",
            Name = "Wardrobe malfunction",
            Description = "Accident",
            Tags = ["wardrobe_malfunction"],
        },
        new()
        {
            Id = "see_through",
            Name = "See-through",
            Description = "Sheer clothes",
            Tags = ["see-through"],
        },
        new()
        {
            Id = "wet_clothes",
            Name = "Wet clothes",
            Description = "Clinging fabric",
            Tags = ["wet_clothes"],
        },
        new()
        {
            Id = "torn_clothes",
            Name = "Torn clothes",
            Description = "Ripped",
            Tags = ["torn_clothes"],
        },
        new()
        {
            Id = "open_shirt",
            Name = "Open shirt",
            Description = "Unbuttoned",
            Tags = ["open_shirt"],
        },
        new()
        {
            Id = "off_shoulder",
            Name = "Off shoulder",
            Description = "Bare shoulder",
            Tags = ["off_shoulder"],
        },
        new()
        {
            Id = "bare_shoulders",
            Name = "Bare shoulders",
            Description = "Exposed shoulders",
            Tags = ["bare_shoulders"],
        },
        new()
        {
            Id = "collarbone",
            Name = "Collarbone",
            Description = "Neckline",
            Tags = ["collarbone"],
        },
        new()
        {
            Id = "midriff",
            Name = "Midriff",
            Description = "Belly",
            Tags = ["midriff"],
        },
        new()
        {
            Id = "navel",
            Name = "Navel",
            Description = "Belly button",
            Tags = ["navel"],
        },
        new()
        {
            Id = "back",
            Name = "Back view",
            Description = "From behind",
            Tags = ["back"],
        },
        new()
        {
            Id = "from_behind",
            Name = "From behind",
            Description = "Rear angle",
            Tags = ["from_behind"],
        },
        new()
        {
            Id = "from_above",
            Name = "From above",
            Description = "High angle",
            Tags = ["from_above"],
        },
        new()
        {
            Id = "from_below",
            Name = "From below",
            Description = "Low angle",
            Tags = ["from_below"],
        },
        new()
        {
            Id = "dutch_angle",
            Name = "Dutch angle",
            Description = "Tilted",
            Tags = ["dutch_angle"],
        },
        new()
        {
            Id = "close_up",
            Name = "Close-up",
            Description = "Face focus",
            Tags = ["close-up"],
        },
        new()
        {
            Id = "portrait",
            Name = "Portrait",
            Description = "Upper body",
            Tags = ["portrait"],
        },
        new()
        {
            Id = "full_body",
            Name = "Full body",
            Description = "Whole figure",
            Tags = ["full_body"],
        },
        new()
        {
            Id = "lying",
            Name = "Lying down",
            Description = "Reclined",
            Tags = ["lying"],
        },
        new()
        {
            Id = "sitting",
            Name = "Sitting",
            Description = "Seated",
            Tags = ["sitting"],
        },
        new()
        {
            Id = "standing",
            Name = "Standing",
            Description = "Upright",
            Tags = ["standing"],
        },
        new()
        {
            Id = "kneeling",
            Name = "Kneeling",
            Description = "On knees",
            Tags = ["kneeling"],
        },
        new()
        {
            Id = "on_bed",
            Name = "On bed",
            Description = "Bedroom",
            Tags = ["on_bed"],
        },
        new()
        {
            Id = "bedroom",
            Name = "Bedroom",
            Description = "Indoor",
            Tags = ["bedroom"],
        },
        new()
        {
            Id = "couch",
            Name = "On couch",
            Description = "Living room",
            Tags = ["couch"],
        },
        new()
        {
            Id = "chair",
            Name = "On chair",
            Description = "Seated furniture",
            Tags = ["chair"],
        },
        new()
        {
            Id = "table",
            Name = "On table",
            Description = "Furniture",
            Tags = ["table"],
        },
        new()
        {
            Id = "window",
            Name = "By window",
            Description = "Natural light",
            Tags = ["window"],
        },
        new()
        {
            Id = "cityscape",
            Name = "Cityscape",
            Description = "Urban",
            Tags = ["cityscape"],
        },
        new()
        {
            Id = "sky",
            Name = "Sky background",
            Description = "Outdoors",
            Tags = ["sky"],
        },
        new()
        {
            Id = "cloud",
            Name = "Clouds",
            Description = "Sky",
            Tags = ["cloud"],
        },
        new()
        {
            Id = "sunset",
            Name = "Sunset",
            Description = "Golden hour",
            Tags = ["sunset"],
        },
        new()
        {
            Id = "sunrise",
            Name = "Sunrise",
            Description = "Morning light",
            Tags = ["sunrise"],
        },
        new()
        {
            Id = "star",
            Name = "Starry sky",
            Description = "Night sky",
            Tags = ["starry_sky"],
        },
        new()
        {
            Id = "flower",
            Name = "Flowers",
            Description = "Floral",
            Tags = ["flower"],
        },
        new()
        {
            Id = "rose",
            Name = "Roses",
            Description = "Romantic",
            Tags = ["rose"],
        },
        new()
        {
            Id = "cherry_blossoms",
            Name = "Cherry blossoms",
            Description = "Sakura",
            Tags = ["cherry_blossoms"],
        },
        new()
        {
            Id = "japanese_clothes",
            Name = "Japanese clothes",
            Description = "Traditional",
            Tags = ["japanese_clothes"],
        },
        new()
        {
            Id = "chinese_clothes",
            Name = "Chinese clothes",
            Description = "Traditional",
            Tags = ["chinese_clothes"],
        },
        new()
        {
            Id = "armor",
            Name = "Armor",
            Description = "Armored",
            Tags = ["armor"],
        },
        new()
        {
            Id = "bodysuit",
            Name = "Bodysuit",
            Description = "Tight suit",
            Tags = ["bodysuit"],
        },
        new()
        {
            Id = "leotard",
            Name = "Leotard",
            Description = "Dancewear",
            Tags = ["leotard"],
        },
        new()
        {
            Id = "gym_uniform",
            Name = "Gym uniform",
            Description = "Sports",
            Tags = ["gym_uniform"],
        },
        new()
        {
            Id = "track_suit",
            Name = "Tracksuit",
            Description = "Athletic",
            Tags = ["track_suit"],
        },
        new()
        {
            Id = "sports_bra",
            Name = "Sports bra",
            Description = "Athletic",
            Tags = ["sports_bra"],
        },
        new()
        {
            Id = "yoga_pants",
            Name = "Yoga pants",
            Description = "Athletic",
            Tags = ["yoga_pants"],
        },
        new()
        {
            Id = "jeans",
            Name = "Jeans",
            Description = "Denim",
            Tags = ["jeans"],
        },
        new()
        {
            Id = "shorts",
            Name = "Shorts",
            Description = "Short pants",
            Tags = ["shorts"],
        },
        new()
        {
            Id = "miniskirt",
            Name = "Miniskirt",
            Description = "Short skirt",
            Tags = ["miniskirt"],
        },
        new()
        {
            Id = "pleated_skirt",
            Name = "Pleated skirt",
            Description = "School style",
            Tags = ["pleated_skirt"],
        },
        new()
        {
            Id = "sweater",
            Name = "Sweater",
            Description = "Knit top",
            Tags = ["sweater"],
        },
        new()
        {
            Id = "hoodie",
            Name = "Hoodie",
            Description = "Casual",
            Tags = ["hoodie"],
        },
        new()
        {
            Id = "jacket",
            Name = "Jacket",
            Description = "Outerwear",
            Tags = ["jacket"],
        },
        new()
        {
            Id = "coat",
            Name = "Coat",
            Description = "Outerwear",
            Tags = ["coat"],
        },
        new()
        {
            Id = "cape",
            Name = "Cape",
            Description = "Fantasy wear",
            Tags = ["cape"],
        },
        new()
        {
            Id = "hat",
            Name = "Hat",
            Description = "Headwear",
            Tags = ["hat"],
        },
        new()
        {
            Id = "beret",
            Name = "Beret",
            Description = "Headwear",
            Tags = ["beret"],
        },
        new()
        {
            Id = "headphones",
            Name = "Headphones",
            Description = "Accessories",
            Tags = ["headphones"],
        },
        new()
        {
            Id = "ribbon",
            Name = "Ribbon",
            Description = "Hair accessory",
            Tags = ["ribbon"],
        },
        new()
        {
            Id = "hair_ornament",
            Name = "Hair ornament",
            Description = "Accessory",
            Tags = ["hair_ornament"],
        },
        new()
        {
            Id = "hairband",
            Name = "Hairband",
            Description = "Accessory",
            Tags = ["hairband"],
        },
        new()
        {
            Id = "earrings",
            Name = "Earrings",
            Description = "Jewelry",
            Tags = ["earrings"],
        },
        new()
        {
            Id = "necklace",
            Name = "Necklace",
            Description = "Jewelry",
            Tags = ["necklace"],
        },
        new()
        {
            Id = "choker",
            Name = "Choker",
            Description = "Neck accessory",
            Tags = ["choker"],
        },
        new()
        {
            Id = "collar",
            Name = "Collar",
            Description = "Neck accessory",
            Tags = ["collar"],
        },
        new()
        {
            Id = "gloves",
            Name = "Gloves",
            Description = "Hands",
            Tags = ["gloves"],
        },
        new()
        {
            Id = "fingerless_gloves",
            Name = "Fingerless gloves",
            Description = "Hands",
            Tags = ["fingerless_gloves"],
        },
        new()
        {
            Id = "stockings",
            Name = "Stockings",
            Description = "Legwear",
            Tags = ["stockings"],
        },
        new()
        {
            Id = "pantyhose",
            Name = "Pantyhose",
            Description = "Legwear",
            Tags = ["pantyhose"],
        },
        new()
        {
            Id = "fishnets",
            Name = "Fishnets",
            Description = "Legwear",
            Tags = ["fishnets"],
        },
        new()
        {
            Id = "kneehighs",
            Name = "Knee highs",
            Description = "Socks",
            Tags = ["kneehighs"],
        },
        new()
        {
            Id = "barefoot",
            Name = "Barefoot",
            Description = "No shoes",
            Tags = ["barefoot"],
        },
        new()
        {
            Id = "shoes",
            Name = "Shoes",
            Description = "Footwear",
            Tags = ["shoes"],
        },
        new()
        {
            Id = "boots",
            Name = "Boots",
            Description = "Footwear",
            Tags = ["boots"],
        },
        new()
        {
            Id = "sandals",
            Name = "Sandals",
            Description = "Footwear",
            Tags = ["sandals"],
        },
        new()
        {
            Id = "tail",
            Name = "Tail",
            Description = "Character tail",
            Tags = ["tail"],
        },
        new()
        {
            Id = "horns",
            Name = "Horns",
            Description = "Demon etc",
            Tags = ["horns"],
        },
        new()
        {
            Id = "wings",
            Name = "Wings",
            Description = "Angel/demon",
            Tags = ["wings"],
        },
        new()
        {
            Id = "pointy_ears",
            Name = "Pointy ears",
            Description = "Elf etc",
            Tags = ["pointy_ears"],
        },
        new()
        {
            Id = "fangs",
            Name = "Fangs",
            Description = "Vampire etc",
            Tags = ["fangs"],
        },
        new()
        {
            Id = "heterochromia",
            Name = "Heterochromia",
            Description = "Different eye colors",
            Tags = ["heterochromia"],
        },
        new()
        {
            Id = "red_eyes",
            Name = "Red eyes",
            Description = "Eye color",
            Tags = ["red_eyes"],
        },
        new()
        {
            Id = "blue_eyes",
            Name = "Blue eyes",
            Description = "Eye color",
            Tags = ["blue_eyes"],
        },
        new()
        {
            Id = "green_eyes",
            Name = "Green eyes",
            Description = "Eye color",
            Tags = ["green_eyes"],
        },
        new()
        {
            Id = "purple_eyes",
            Name = "Purple eyes",
            Description = "Eye color",
            Tags = ["purple_eyes"],
        },
        new()
        {
            Id = "yellow_eyes",
            Name = "Yellow eyes",
            Description = "Eye color",
            Tags = ["yellow_eyes"],
        },
        new()
        {
            Id = "hetero_kiss",
            Name = "Hetero kiss",
            Description = "Couple kiss",
            Tags = ["1girl", "1boy", "kiss"],
        },
        new()
        {
            Id = "yuri_kiss",
            Name = "Yuri kiss",
            Description = "Girls kissing",
            Tags = ["yuri", "2girls", "kiss"],
        },
        new()
        {
            Id = "cuddling",
            Name = "Cuddling",
            Description = "Intimate",
            Tags = ["cuddling"],
        },
        new()
        {
            Id = "hug",
            Name = "Hug",
            Description = "Embrace",
            Tags = ["hug"],
        },
        new()
        {
            Id = "holding_hands",
            Name = "Holding hands",
            Description = "Romantic",
            Tags = ["holding_hands"],
        },
        new()
        {
            Id = "lap_pillow",
            Name = "Lap pillow",
            Description = "Resting",
            Tags = ["lap_pillow"],
        },
        new()
        {
            Id = "sleeping",
            Name = "Sleeping",
            Description = "Asleep",
            Tags = ["sleeping"],
        },
        new()
        {
            Id = "waking_up",
            Name = "Waking up",
            Description = "Morning",
            Tags = ["waking_up"],
        },
        new()
        {
            Id = "bathing",
            Name = "Bathing",
            Description = "Wash scene",
            Tags = ["bathing"],
        },
        new()
        {
            Id = "towel",
            Name = "Towel",
            Description = "After bath",
            Tags = ["towel"],
        },
        new()
        {
            Id = "soap",
            Name = "Soap bubbles",
            Description = "Bath",
            Tags = ["soap_bubbles"],
        },
        new()
        {
            Id = "steam",
            Name = "Steam",
            Description = "Hot scene",
            Tags = ["steam"],
        },
        new()
        {
            Id = "censored",
            Name = "Censored",
            Description = "Mosaic etc",
            Tags = ["censored"],
        },
        new()
        {
            Id = "mosaic_censoring",
            Name = "Mosaic",
            Description = "Pixel censor",
            Tags = ["mosaic_censoring"],
        },
        new()
        {
            Id = "bar_censor",
            Name = "Bar censor",
            Description = "Black bar",
            Tags = ["bar_censor"],
        },
        new()
        {
            Id = "convenient_censoring",
            Name = "Convenient censor",
            Description = "Strategic cover",
            Tags = ["convenient_censoring"],
        },
        new()
        {
            Id = "no_humans",
            Name = "No humans",
            Description = "Landscape/object",
            Tags = ["no_humans"],
        },
        new()
        {
            Id = "multiple_girls",
            Name = "Multiple girls",
            Description = "Several girls",
            Tags = ["multiple_girls"],
        },
        new()
        {
            Id = "solo_focus",
            Name = "Solo focus",
            Description = "One character focus",
            Tags = ["solo_focus"],
        },
        new()
        {
            Id = "character_name",
            Name = "Named character",
            Description = "Has character tag",
            Tags = ["character_name"],
        },
    ];
}

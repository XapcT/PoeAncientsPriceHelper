namespace PoeAncientsPriceHelper;

internal static class LocalizedNameResolver
{
    private static readonly IReadOnlyDictionary<string, string> RussianAliases = CreateRussianAliases();

    public static string Resolve(string normalizedName)
    {
        if (TryResolveRussianUncutGemKey(normalizedName, out var gemKey))
            return gemKey;
        if (TryResolveRussianThaumaturgicFluxKey(normalizedName, out var fluxKey))
            return fluxKey;
        if (TryResolveRussianFenumusRuneKey(normalizedName, out var fenumusKey))
            return fenumusKey;
        if (TryResolveRussianAncientRuneKey(normalizedName, out var ancientRuneKey))
            return ancientRuneKey;

        if (RussianAliases.TryGetValue(normalizedName, out var key))
            return key;

        if (normalizedName.Length >= 10)
        {
            var suffixMatch = RussianAliases
                .Where(a => a.Key.EndsWith(normalizedName, StringComparison.Ordinal))
                .OrderBy(a => a.Key.Length)
                .FirstOrDefault();
            if (suffixMatch.Key is not null)
                return suffixMatch.Value;
        }

        if (normalizedName.Length >= 8 &&
            BestRussianAlias(normalizedName) is { } fuzzyKey)
            return fuzzyKey;

        if (TryResolveRussianRuneOfKey(normalizedName, out var runeOfKey))
            return runeOfKey;

        return normalizedName;
    }

    private static bool TryResolveRussianUncutGemKey(string normalizedName, out string key)
    {
        key = "";
        if (!normalizedName.Contains("камень"))
            return false;

        var type =
            normalizedName.Contains("духа") ? "spirit" :
            normalizedName.Contains("поддерж") ? "support" :
            normalizedName.Contains("умени") ? "skill" :
            null;
        if (type is null)
            return false;

        var level = Regex.Match(normalizedName, @"\bуров(?:ень|ня)?\s+(\d{1,2})\b");
        if (!level.Success)
            return false;

        key = $"uncut {type} gem level {level.Groups[1].Value}";
        return true;
    }

    private static bool TryResolveRussianThaumaturgicFluxKey(string normalizedName, out string key)
    {
        key = "";
        if (!normalizedName.Contains("чародейский") || !normalizedName.Contains("расплав"))
            return false;

        var level = Regex.Match(normalizedName, @"\bуров(?:ень|ня)?\s+(\d{1,2})\b");
        if (!level.Success)
            return false;

        key = $"thaumaturgic flux level {level.Groups[1].Value}";
        return true;
    }

    private static bool TryResolveRussianFenumusRuneKey(string normalizedName, out string key)
    {
        key = "";
        if (!normalizedName.Contains("фенум") && !normalizedName.Contains("енум"))
            return false;

        key =
            normalizedName.Contains("агонии") ? "fenumus rune of agony" :
            normalizedName.Contains("высушивания") ? "fenumus rune of draining" :
            normalizedName.Contains("плетения") ? "fenumus rune of spinning" :
            "";

        return key.Length > 0;
    }

    private static bool TryResolveRussianAncientRuneKey(string normalizedName, out string key)
    {
        key = "";
        if (!normalizedName.Contains("руна") ||
            (!normalizedName.Contains("древняя") && !normalizedName.Contains("ревняя")))
            return false;

        var suffix = ResolveByContainedWord(normalizedName, AncientRuneSuffixes);
        if (suffix is null)
            return false;

        key = $"ancient rune of {suffix}";
        return true;
    }

    private static bool TryResolveRussianRuneOfKey(string normalizedName, out string key)
    {
        key = "";
        if (!normalizedName.Contains("руна"))
            return false;

        var suffix = ResolveByContainedWord(normalizedName, RuneOfSuffixes);
        if (suffix is null)
            return false;

        key = suffix.StartsWith("the ", StringComparison.Ordinal)
            ? $"rune of {suffix}"
            : $"rune of {suffix}";
        return true;
    }

    private static string? ResolveByContainedWord(string normalizedName, IReadOnlyDictionary<string, string> suffixes)
    {
        foreach (var (russian, english) in suffixes.OrderByDescending(x => x.Key.Length))
        {
            if (Regex.IsMatch(normalizedName, $@"\b{Regex.Escape(russian)}\b"))
                return english;
        }
        return null;
    }

    private static string? BestRussianAlias(string normalizedName)
    {
        string? best = null;
        var bestScore = 0.88;
        foreach (var (alias, key) in RussianAliases)
        {
            if (Math.Abs(alias.Length - normalizedName.Length) > 4)
                continue;

            var score = Similarity(normalizedName, alias);
            if (score > bestScore)
            {
                bestScore = score;
                best = key;
            }
        }
        return best;
    }

    private static double Similarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0)
            return 1;

        var dist = Levenshtein(a, b);
        return 1.0 - (double)dist / Math.Max(a.Length, b.Length);
    }

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++) prev[j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return prev[b.Length];
    }

    private static Dictionary<string, string> CreateRussianAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string russian, string english)
            => aliases[OcrScanner.NormalizeName(russian)] = PriceRepository.NormalizeName(english);

        void AddRuneFamily(string russianSuffix, string englishPrefix)
        {
            Add($"Руна {russianSuffix}", $"{englishPrefix} Rune");
            Add($"Малая руна {russianSuffix}", $"Lesser {englishPrefix} Rune");
            Add($"Большая руна {russianSuffix}", $"Greater {englishPrefix} Rune");
            Add($"Безупречная руна {russianSuffix}", $"Perfect {englishPrefix} Rune");
        }

        Add("Точильный камень", "Blacksmith's Whetstone");
        Add("Резец чародея", "Arcanist's Etcher");
        Add("Свиток мудрости", "Scroll of Wisdom");
        Add("Сфера хаоса", "Chaos Orb");
        Add("Большая сфера хаоса", "Greater Chaos Orb");
        Add("Совершенная сфера хаоса", "Perfect Chaos Orb");
        Add("Деталь доспеха", "Armourer's Scrap");
        Add("Божественная сфера", "Divine Orb");
        Add("Зеркало Каландры", "Mirror of Kalandra");
        Add("Прядь Хинекоры", "Hinekora's Lock");
        Add("Сфера алхимии", "Orb of Alchemy");
        Add("Сфера удачи", "Orb of Chance");
        Add("Сфера превращения", "Orb of Transmutation");
        Add("Большая сфера превращения", "Greater Orb of Transmutation");
        Add("Совершенная сфера превращения", "Perfect Orb of Transmutation");
        Add("Сфера возвышения", "Exalted Orb");
        Add("Большая сфера возвышения", "Greater Exalted Orb");
        Add("Совершенная сфера возвышения", "Perfect Exalted Orb");
        Add("Сфера царей", "Regal Orb");
        Add("Большая сфера царей", "Greater Regal Orb");
        Add("Совершенная сфера царей", "Perfect Regal Orb");
        Add("Сфера усиления", "Orb of Augmentation");
        Add("Большая сфера усиления", "Greater Orb of Augmentation");
        Add("Совершенная сфера усиления", "Perfect Orb of Augmentation");
        Add("Стекольная масса", "Glassblower's Bauble");
        Add("Призма камнереза", "Gemcutter's Prism");
        Add("Малая сфера златокузнеца", "Lesser Jeweller's Orb");
        Add("Большая сфера златокузнеца", "Greater Jeweller's Orb");
        Add("Совершенная сфера златокузнеца", "Perfect Jeweller's Orb");
        Add("Сфера астромантии", "Artificer's Orb");
        Add("Осколок астромантии", "Artificer's Shard");
        Add("Осколок превращения", "Transmutation Shard");
        Add("Осколок удачи", "Chance Shard");
        Add("Сфера ваал", "Vaal Orb");
        Add("Сфера отмены", "Orb of Annulment");
        Add("Раскалывающая сфера", "Fracturing Orb");
        Add("Осколок царей", "Regal Shard");
        Add("Древний усилитель", "Ancient Infuser");
        Add("Древний инфузер", "Ancient Infuser");
        Add("Дестабилизатор ядра", "Core Destabiliser");
        Add("Кристаллизованная порча", "Crystallised Corruption");
        Add("Криптический ключ", "Cryptic Key");
        Add("Тайный ключ", "Cryptic Key");
        Add("Сфера извлечения", "Orb of Extraction");
        Add("Культивирующая сфера ваал", "Vaal Cultivation Orb");
        Add("Уникальная бижутерия", "Unique Jewellery");
        Add("Сфера архитектора", "Architect's Orb");
        Add("Нагнетатель чародея ваал", "Vaal Arcanist's Infuser");
        Add("Нагнетатель бронника ваал", "Vaal Armourer's Infuser");
        Add("Нагнетатель кузнеца ваал", "Vaal Blacksmith's Infuser");
        Add("Катализирующий нагнетатель ваал", "Vaal Catalysing Infuser");
        Add("Поглотитель ваал", "Vaal Siphoner");
        Add("Веризий", "Verisium");
        Add("Исключительный веризий", "Exceptional Verisium");
        Add("Рунный сплав", "Runic Alloy");
        Add("Экспансивный сплав", "Expansive Alloy");
        Add("Лёгкий сплав", "Swift Alloy");
        Add("Мистический сплав", "Mystic Alloy");
        Add("Небесный сплав", "Celestial Alloy");
        Add("Вихревой сплав", "Cyclonic Alloy");
        Add("Радужный сплав", "Prismatic Alloy");
        Add("Защитный сплав", "Protective Alloy");
        Add("Державный сплав", "Sovereign Alloy");
        Add("Сплав Повелителя рун", "The Runebinder's Alloy");
        Add("Сплав Рунного отца", "The Runefather's Alloy");
        Add("Возвышенный сплав", "Transcendent Alloy");
        Add("Благоговейная подзвёздная руда", "Revered Starlit Ore");
        Add("Почитаемая подзвёздная руда", "Venerable Starlit Ore");
        Add("Истинная подзвёздная руда", "Veridical Starlit Ore");
        Add("Оберегающая подзвёздная руда", "Warding Starlit Ore");
        Add("Знак Круга Медведя", "Medved's Crest of the Circle");
        Add("Знак Солнца Олрота", "Olroth's Crest of the Sun");
        Add("Знак Чаши Утреда", "Uhtred's Crest of the Chalice");
        Add("Знак Косы Вораны", "Vorana's Crest of the Scythe");
        Add("Сага Альдура", "Aldur's Saga");
        Add("Сага Медведя", "Medved's Saga");
        Add("Сага Олрота", "Olroth's Saga");
        Add("Сага Утреда", "Uhtred's Saga");
        Add("Сага Вораны", "Vorana's Saga");
        Add("Журнал экспедиции", "Expedition Logbook");
        Add("Жгучий расплав", "Blazing Flux");
        Add("Студёный расплав", "Chilling Flux");
        Add("Искрящий расплав", "Crackling Flux");
        Add("Пустотный расплав", "Void Flux");
        Add("Сфера Узазы", "Perfect Flux");
        Add("Наследие Альдура", "Aldur's Legacy");
        Add("Изобретательность Астрид", "Astrid's Creativity");
        Add("Предательство Альдура", "Betrayal of Aldur");
        Add("Дыхание Альдура", "Breath of Aldur");
        Add("Озарение Кадигана", "Cadigan's Epiphany");
        Add("Гнев Альдура", "Ire of Aldur");
        Add("Мрачность Катлы", "Katla's Gloom");
        Add("Охота Колра", "Kolr's Hunt");
        Add("Присмотр Медведя", "Medved's Tending");
        Add("Страсть Альдура", "Passion of Aldur");
        Add("Триумф Серли", "Serle's Triumph");
        Add("Сила Трада", "Thrud's Might");
        Add("Сидерий Утреда", "Uhtred's Sidereus");
        Add("Резня Вораны", "Vorana's Carnage");
        Add("Руна мудрости лесной ведьмы Ассандры", "Hedgewitch Assandra's Rune of Wisdom");
        Add("Руна дикости тана Гирта", "Thane Girt's Rune of Wildness");
        Add("Руна мастерства тана Граннеля", "Thane Grannell's Rune of Mastery");
        Add("Руна весны тана Лельда", "Thane Leld's Rune of Spring");
        Add("Руна лета тана Мирка", "Thane Myrk's Rune of Summer");
        Add("Руна меткости графини Сеске", "Countess Seske's Rune of Archery");
        Add("Руна жестокости куртизанки Маннан", "Courtesan Mannan's Rune of Cruelty");
        Add("Руна восстановления Краценна", "Craiceann's Rune of Recovery");
        Add("Руна барьера Краценна", "Craiceann's Rune of Warding");
        Add("Руна грации Фаррул", "Farrul's Rune of Grace");
        Add("Руна погони Фаррул", "Farrul's Rune of the Chase");
        Add("Руна охоты Фаррул", "Farrul's Rune of the Hunt");
        Add("Руна зимы леди Гестры", "Lady Hestra's Rune of Winter");
        Add("Руна эрозии Сакаваля", "Saqawal's Rune of Erosion");
        Add("Руна памяти Сакаваля", "Saqawal's Rune of Memory");
        Add("Руна неба Сакаваля", "Saqawal's Rune of the Sky");
        Add("Руна когтей Великого волка", "The Greatwolf's Rune of Claws");
        Add("Руна воли Великого волка", "The Greatwolf's Rune of Willpower");
        Add("Руна накопления", "Rune of Accumulation");
        Add("Руна акробатики", "Rune of Acrobatics");
        Add("Руна охоты", "Rune of the Hunt");
        Add("Адаптивный сплав", "Adaptive Alloy");
        Add("Мастерская руна", "Masterwork Rune");
        Add("Руна заряда", "Charging Rune");
        Add("Большая руна заряда", "Greater Charging Rune");
        Add("Безупречная руна заряда", "Perfect Charging Rune");
        Add("Большая руна стремления", "Greater Rune of Alacrity");
        Add("Большая руна лидерства", "Greater Rune of Leadership");
        Add("Большая руна дворянства", "Greater Rune of Nobility");
        Add("Большая руна десятины", "Greater Rune of Tithing");
        AddRuneFamily("искусности", "Adept");
        AddRuneFamily("тела", "Body");
        AddRuneFamily("пустыни", "Desert");
        AddRuneFamily("ледника", "Glacial");
        AddRuneFamily("вдохновения", "Inspiration");
        AddRuneFamily("железа", "Iron");
        AddRuneFamily("разума", "Mind");
        AddRuneFamily("перерождения", "Rebirth");
        AddRuneFamily("решительности", "Resolve");
        AddRuneFamily("мощи", "Robust");
        AddRuneFamily("камня", "Stone");
        AddRuneFamily("шторма", "Storm");
        AddRuneFamily("видения", "Vision");
        AddRuneFamily("барьера", "Ward");
        Add("Руна агонии Фенумы", "Fenumus' Rune of Agony");
        Add("Агонии", "Fenumus' Rune of Agony");
        Add("Руна высушивания Фенумы", "Fenumus' Rune of Draining");
        Add("Руна плетения Фенумы", "Fenumus' Rune of Spinning");
        Add("Барьерная руна уничтожения", "Warding Rune of Annihilation");
        Add("Барьерная руна панциря", "Warding Rune of Armature");
        Add("Барьерная руна телохранителей", "Warding Rune of Bodyguards");
        Add("Барьерная руна храбрости", "Warding Rune of Courage");
        Add("Барьерная руна отчаяния", "Warding Rune of Desperation");
        Add("Барьерная руна расщепления", "Warding Rune of Disintegration");
        Add("Барьерная руна равноденствия", "Warding Rune of Equinox");
        Add("Барьерная руна скольжения", "Warding Rune of Glancing");
        Add("Барьерная руна сердца", "Warding Rune of Heart");
        Add("Барьерная руна опустевания", "Warding Rune of Hollowing");
        Add("Барьерная руна пропитания", "Warding Rune of Nourishment");
        Add("Барьерная руна одержимости", "Warding Rune of Obsession");
        Add("Барьерная руна защиты", "Warding Rune of Protection");
        Add("Барьерная руна укрепления", "Warding Rune of Reinforcement");
        Add("Барьерная руна обломков", "Warding Rune of Salvaging");
        Add("Барьерная руна устойчивости", "Warding Rune of Stability");
        Add("Барьерная руна симбиоза", "Warding Rune of Symbiosis");

        return aliases;
    }

    private static readonly IReadOnlyDictionary<string, string> AncientRuneSuffixes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OcrScanner.NormalizeName("вражды")] = "animosity",
            [OcrScanner.NormalizeName("правления")] = "control",
            [OcrScanner.NormalizeName("тлена")] = "decay",
            [OcrScanner.NormalizeName("подрыва")] = "detonation",
            [OcrScanner.NormalizeName("находки")] = "discovery",
            [OcrScanner.NormalizeName("поединка")] = "dueling",
            [OcrScanner.NormalizeName("мастерства")] = "prowess",
            [OcrScanner.NormalizeName("расплаты")] = "retaliation",
            [OcrScanner.NormalizeName("разбивания")] = "shattering",
            [OcrScanner.NormalizeName("осколков")] = "splinters",
            [OcrScanner.NormalizeName("орды")] = "the horde",
            [OcrScanner.NormalizeName("титана")] = "the titan",
            [OcrScanner.NormalizeName("ведьмовства")] = "witchcraft",
        };

    private static readonly IReadOnlyDictionary<string, string> RuneOfSuffixes =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OcrScanner.NormalizeName("накопления")] = "accumulation",
            [OcrScanner.NormalizeName("акробатики")] = "acrobatics",
            [OcrScanner.NormalizeName("противоборства")] = "confrontation",
            [OcrScanner.NormalizeName("противостояния")] = "confrontation",
            [OcrScanner.NormalizeName("постоянства")] = "consistency",
            [OcrScanner.NormalizeName("кульминации")] = "culmination",
            [OcrScanner.NormalizeName("основ")] = "foundations",
            [OcrScanner.NormalizeName("охвата")] = "reach",
            [OcrScanner.NormalizeName("досягаемости")] = "reach",
            [OcrScanner.NormalizeName("славы")] = "renown",
            [OcrScanner.NormalizeName("цветения")] = "the blossom",
            [OcrScanner.NormalizeName("охоты")] = "the hunt",
            [OcrScanner.NormalizeName("призмы")] = "the prism",
            [OcrScanner.NormalizeName("живого пламени")] = "vital flame",
            [OcrScanner.NormalizeName("жизненного пламени")] = "vital flame",
            [OcrScanner.NormalizeName("живучести")] = "vitality",
            [OcrScanner.NormalizeName("жизненной силы")] = "vitality",
        };
}

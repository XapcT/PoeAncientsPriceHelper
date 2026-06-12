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

    private static Dictionary<string, string> CreateRussianAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        void Add(string russian, string english)
            => aliases[OcrScanner.NormalizeName(russian)] = PriceRepository.NormalizeName(english);

        Add("Точильный камень", "Blacksmith's Whetstone");
        Add("Резец чародея", "Arcanist's Etcher");
        Add("Свиток мудрости", "Scroll of Wisdom");
        Add("Сфера хаоса", "Chaos Orb");
        Add("Большая сфера хаоса", "Greater Chaos Orb");
        Add("Совершенная сфера хаоса", "Perfect Chaos Orb");
        Add("Деталь доспеха", "Armourer's Scrap");
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
        Add("Руна мудрости лесной ведьмы Ассандры", "Hedgewitch Assandra's Rune of Wisdom");
        Add("Руна дикости тана Гирта", "Thane Girt's Rune of Wildness");
        Add("Руна мастерства тана Граннеля", "Thane Grannell's Rune of Mastery");
        Add("Руна весны тана Лельда", "Thane Leld's Rune of Spring");
        Add("Руна лета тана Мирка", "Thane Myrk's Rune of Summer");
        Add("Древняя руна правления", "Ancient Rune of Control");
        Add("Руна накопления", "Rune of Accumulation");
        Add("Руна охоты", "Rune of the Hunt");
        Add("Адаптивный сплав", "Adaptive Alloy");
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
        Add("Большая руна заряда", "Greater Charging Rune");
        Add("Большая руна барьера", "Greater Ward Rune");

        return aliases;
    }
}

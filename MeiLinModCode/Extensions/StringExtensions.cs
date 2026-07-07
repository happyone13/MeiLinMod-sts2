namespace MeiLinMod.MeiLinModCode.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    private static bool ResourceExists(string path)
    {
        return Godot.ResourceLoader.Exists(path) || Godot.ResourceLoader.Exists($"res://{path}");
    }

    public static string ImagePath(this string path)
    {
        return $"{MainFile.ModId}/images/{path}";
    }

    public static string CardImagePath(this string path)
    {
        return $"{MainFile.ModId}/images/card_portraits/{path}";
    }

    public static string BigCardImagePath(this string path)
    {
        return path.CardImagePath();
    }

    public static string CardImagePathOrDefault(this string path)
    {
        var targetPath = path.CardImagePath();
        return ResourceExists(targetPath) ? targetPath : "card.png".CardImagePath();
    }

    public static string BigCardImagePathOrDefault(this string path)
    {
        return path.CardImagePathOrDefault();
    }

    public static string PowerImagePath(this string path)
    {
        return $"{MainFile.ModId}/images/powers/{path}";
    }

    public static string PowerImagePathOrDefault(this string path)
    {
        var targetPath = path.PowerImagePath();
        return ResourceExists(targetPath) ? targetPath : "power.png".PowerImagePath();
    }

    public static string BigPowerImagePath(this string path)
    {
        return path.PowerImagePath();
    }

    public static string BigPowerImagePathOrDefault(this string path)
    {
        return path.PowerImagePathOrDefault();
    }

    public static string PotionImagePath(this string path)
    {
        return $"{MainFile.ModId}/images/potions/{path}";
    }

    public static string RelicImagePath(this string path)
    {
        return $"{MainFile.ModId}/images/relics/{path}";
    }

    public static string BigRelicImagePath(this string path)
    {
        return path.RelicImagePath();
    }

    public static string CharacterUiPath(this string path)
    {
        return $"{MainFile.ModId}/images/charui/{path}";
    }

    public static string ToSnakeCaseAssetStem(this Type type)
    {
        var name = type.Name;
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var result = new System.Text.StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(c));
        }

        return result.ToString();
    }
}

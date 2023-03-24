namespace DiscordBot.Language;

public static class Romanize
{
    public static string FromBulgarian(string bulgarianText)
    {
        var romanizedText = string.Empty;
        foreach (var bg in bulgarianText)
        {
            var yes = BulgarianToRomanSwitch(bg);
            romanizedText += char.IsLower(bg) ? yes :
                yes.Length > 1 ? char.ToUpper(yes[0]) + yes[1..] : char.ToUpper(yes[0]);
        }

        return romanizedText;
    }

    public static string BulgarianToRomanSwitch(char bg)
    {
        return char.ToLower(bg) switch
        {
            'в' => 'v',
            'е' => 'e',
            'р' => 'r',
            'т' => 't',
            'ъ' => 'u',
            'у' => 'u',
            'и' => 'i',
            'о' => 'o',
            'п' => 'p',
            'а' => 'a',
            'с' => 's',
            'д' => 'd',
            'ф' => 'f',
            'г' => 'g',
            'х' => 'h',
            'й' => 'y',
            'к' => 'k',
            'л' => 'l',
            'з' => 'z',
            'ь' => 'y',
            'ц' => 'c',
            'б' => 'b',
            'н' => 'n',
            'м' => 'm',

            'я' => "ya",
            'ж' => "zh",
            'ч' => "ch",
            'ш' => "sh",
            'щ' => "sht",
            'ю' => "yu",
            _ => bg
        } + "";
    }
}
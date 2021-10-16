using System.Linq;

namespace Bat_Tosho.Messages
{
    public static class Translate
    {
        /// <summary>
        ///     Turns Bulgarian Traditional Phonetic keys into QWERTY for those who can't type properly.
        /// </summary>
        /// <param name="text">String. Can contain non Bulgarian characters.</param>
        /// <returns>Translated string.</returns>
        public static string BulgarianTraditionalToQwerty(string text)
        {
            if (text.Contains("://")) return text;
            return text.ToCharArray()
                .Aggregate("", (current, character) => current + character switch
                {
                    'я' => "q",
                    'в' => "w",
                    'е' => "e",
                    'р' => "r",
                    'т' => "t",
                    'ъ' => "y",
                    'у' => "u",
                    'и' => "i",
                    'о' => "o",
                    'п' => "p",
                    'а' => "a",
                    'с' => "s",
                    'д' => "d",
                    'ф' => "f",
                    'г' => "g",
                    'х' => "h",
                    'й' => "j",
                    'к' => "k",
                    'л' => "l",
                    'з' => "z",
                    'ь' => "x",
                    'ц' => "c",
                    'ж' => "v",
                    'б' => "b",
                    'н' => "n",
                    'м' => "m",
                    '`' => "ч",
                    'ю' => "\\",
                    'щ' => "]",
                    'ш' => "[",
                    'Я' => "Q",
                    'В' => "W",
                    'Е' => "E",
                    'Р' => "R",
                    'Т' => "T",
                    'Ъ' => "Y",
                    'У' => "U",
                    'И' => "I",
                    'О' => "O",
                    'П' => "P",
                    'А' => "A",
                    'С' => "S",
                    'Д' => "D",
                    'Ф' => "F",
                    'Г' => "G",
                    'Х' => "H",
                    'Й' => "J",
                    'К' => "K",
                    'Л' => "L",
                    'З' => "Z",
                    'Ь' => "X",
                    'Ц' => "C",
                    'Ж' => "V",
                    'Б' => "B",
                    'Н' => "N",
                    'М' => "M",
                    'Ч' => "~",
                    'Ю' => "|",
                    'Щ' => "}",
                    'Ш' => "{",
                    _ => current
                }).ToString();
        }

        public static string MorseToEnglish(string text)
        {
            return text.Count(t => t == '.') switch
            {
                >3 => text.Count(t => t == '-')
                    switch {>3 => text.Count(t => t == '/') switch {>0 => MteConverter(text), _ => text}, _ => text},
                _ => text
            };
        }

        private static string MteConverter(string text)
        {
            var words = text.Split("/");
            var returnString = "";
            foreach (var word in words)
            {
                var chars = word.Split(" ");
                foreach (var chr in chars)
                {
                }
            }

            return returnString;
        }
    }
}
using DiscordBot.Enums;

namespace DiscordBot.Objects
{
    public class Bulgarian : ILanguage
    {
        public string EnterChannelBeforeCommand(string command)
        {
            return $"Влез в гласов канал преди да използваш командата \"{command}\".";
        }

        public string NoFreeBotAccounts()
        {
            return "Бота няма свободни профили. Можеш да добавиш още профили от гилда за поддръжка на бота.";
        }

        public string ThisMessageWillUpdateShortly()
        {
            return "Здравей! Това съобщение ще се промени след малко.";
        }

        public string SelectVideo()
        {
            return "Избери видео.";
        }

        public string SelectVideoTimeout()
        {
            return "Времето за избиране на видео изтече.";
        }

        public string NoResultsFound(string term)
        {
            return $"Не бяха намерени резултати за търсенето: \"{term}\"";
        }

        public string AddedItem(string term)
        {
            return $"Добавяне на: \"{term}\"";
        }

        public string BotIsNotInTheChannel()
        {
            return "Бота не е в канала.";
        }

        public string CouldNotFindCommand(string command)
        {
            return $"Командата: \"{command}\" не съществува.";
        }

        public string LoopStatusUpdate(Loop loop)
        {
            return "Loop status is now: " + loop switch
            {
                Loop.None => "None", Loop.WholeQueue => "Looping whole queue.", Loop.One => "One Item Only.",
                _ => "None"
            };
        }

        public string NumberBiggerThanQueueLength(int number)
        {
            return $"Числото: {number} е по голямо от дължината на списъка. Търсене на числото в YouTube.";
        }

        public string PlayingItemAfterThis(int index, string name)
        {
            return $"Пускане на: ({index}) - \"{name}\" след това.";
        }

        public string PlayingItemAfterThis(string term)
        {
            return $"Пускане на: \"{term}\" след това.";
        }

        public string FailedToRemove(string text)
        {
            return $"Неуспешно премахване на: \"{text}\"";
        }

        public string RemovingItem(string name)
        {
            return $"Премахвам: \"{name}\"";
        }

        public string YouHaveAlreadyGeneratedAWebUiCode()
        {
            return "Вече си генерирал код за онлайн интерфейса";
        }

        public string ControlTheBotUsingAFancyInterface()
        {
            return "Контролирай бота през яко изглеждащ сайт.";
        }

        public string SendingADirectMessageContainingTheInformation()
        {
            return "Изпращам лично съобщение, съдържащо информацията.";
        }

        public string YourWebUiCodeIs()
        {
            return "Твоя код за онлайн интерфейса е";
        }

        public string FailedToMove()
        {
            return "Неуспешно преместване.";
        }

        public string Moved(int itemOne, string name, int item2)
        {
            return $"Преместих ({itemOne}) \"{name}\" на мястото ({item2})";
        }

        public string InvalidMoveFormat()
        {
            return "Невалиден формат на местене.\n" +
                   "Трябва да използваш две числа или да използваш формата написан под това съобщение:\n\n" +
                   "-move Exact Name !to Exact Name 2";
        }

        public string SwitchedThePlacesOf(string itemOne, string itemTwo)
        {
            return $"Размених местата на \"{itemOne}\" и \"{itemTwo}\"";
        }

        public string CurrentQueue()
        {
            return "Сегашния списък:";
        }

        public string TechTip()
        {
            return "\n\nВреме за един съвет. " +
                   "\nПрепоръчвам ти да използваш онлайн интерфейса на бота, защото той показва списъка автоматично. " +
                   "\nС него, ти можеш да добавяш, махаш, или да контролираш бота. " +
                   "\nМожеш да го ползваш с командата \"-getwebui\". " +
                   "\nБота ще ти изпрати лично съобщение с нужната информацията.";
        }

        public string GoingTo(int index, string thing)
        {
            return $"Пускам ({index}) - \"{thing}\"";
        }

        public string SetVolumeTo(double volume)
        {
            return $"Настройване на звука на: {volume}%";
        }

        public string InvalidVolumeRange()
        {
            return "Невалидна настройка на звука. Трябва да е между 0 and 200 процента.";
        }

        public string QueueSavedSuccessfully(string token)
        {
            return $"Списъка е запазен успешно. \n\nМожеш да го пуснеш отново с командата: \"-p pl:{token}\", " +
                   "или ако го прикачиш като използваш \"play\" командата.";
        }

        public string OneCannotRecieveBlessingNotInChannel()
        {
            //return "One cannot recieve the blessing of playback if they're not in a channel.";
            return "Даден човешки индивид не може да получи просветлението на звук ако не са в канал.";
        }

        public string OneCannotRecieveBlessingNothingToPlay()
        {
            return "Даден човешки индивид не може да получи просветлението на звук ако няма нищо за пускане.";
        }

        public string UserNotInChannelLyrics()
        {
            return "Влез в канал преди да използваш \"lyrics\" командата без да търсиш песен с нея.";
        }

        public string BotNotInChannelLyrics()
        {
            return
                "Бота не е в канала. Ако искаш да знаеш текста на някоя песен, напиши ѝ името след тази команда.";
        }

        public string NoResultsFoundLyrics(string search)
        {
            return $"Не бяха намерени резултати за: \"{search}\".";
        }

        public string LyricsLong()
        {
            return
                "Текста на песента е над 2000 букви, което е лимита на Discord за текст. Изпращам текста като файл.";
        }

        public string YouAreNotInTheChannel()
        {
            return "Не си в сегашния канал на бота.";
        }

        public string ShufflingTheQueue()
        {
            return "Размешване на списъка.";
        }

        public string SkippingOneTime()
        {
            return "Пропускам един път.";
        }

        public string PausingThePlayer()
        {
            return "Спирам музиката.";
        }

        public string UnpausingThePlayer()
        {
            return "Продължавам музиката.";
        }

        public string SkippingOneTimeBack()
        {
            return "Връщам се един път назад.";
        }
        
        public string Playing()
        {
            return "Сега се слуша";
        }

        public string RequestedBy()
        {
            return "Добавено от";
        }

        public string NextUp()
        {
            return "Следва";
        }

        public string DefaultStatusbarMessage()
        {
            return "В момента се добавят много нови екстри на бота и неговите компоненти могат да се държат нестабилно. Извинявам се предварително за бъгове, ако има.";
        }
    }
}
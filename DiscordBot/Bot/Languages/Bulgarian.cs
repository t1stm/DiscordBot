using System;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Enums;

namespace DiscordBot.Objects;

public class Bulgarian : AbstractLanguage
{
    public override string EnterChannelBeforeCommand(string command)
    {
        return $"Влез в гласов канал преди да използваш командата \"{command}\".";
    }

    public override string NoFreeBotAccounts()
    {
        return "Бота няма свободни профили. Можеш да добавиш още профили от гилда за поддръжка на бота.";
    }

    public override string ThisMessageWillUpdateShortly()
    {
        return "Здравей! Това съобщение ще се промени след малко.";
    }

    public override string SelectVideo()
    {
        return "Избери видео.";
    }

    public override string SelectVideoTimeout()
    {
        return "Времето за избиране на видео изтече.";
    }

    public override string NoResultsFound(string term)
    {
        return $"Не бяха намерени резултати за търсенето: \"{term}\"";
    }

    public override string AddedItem(string term)
    {
        return $"Добавяне на: \"{term}\"";
    }

    public override string BotIsNotInTheChannel()
    {
        return "Бота не е в канала.";
    }

    public override string CouldNotFindCommand(string command)
    {
        return $"Командата: \"{command}\" не съществува.";
    }

    public override string LoopStatusUpdate(Loop loop)
    {
        return "Loop status is now: " + loop switch
        {
            Loop.None => "None", Loop.WholeQueue => "Looping whole queue.", Loop.One => "One Item Only.",
            _ => "None"
        };
    }

    public override string NumberBiggerThanQueueLength(int number)
    {
        return $"Числото: {number} е по голямо от дължината на списъка. Търсене на числото в YouTube.";
    }

    public override string PlayingItemAfterThis(int index, string name)
    {
        return $"Пускане на: ({index}) - \"{name}\" след това.";
    }

    public override string PlayingItemAfterThis(string term)
    {
        return $"Пускане на: \"{term}\" след това.";
    }

    public override string FailedToRemove(string text)
    {
        return $"Неуспешно премахване на: \"{text}\"";
    }

    public override string RemovingItem(string name)
    {
        return $"Премахвам: \"{name}\"";
    }

    public override string YouHaveAlreadyGeneratedAWebUiCode()
    {
        return "Вече си генерирал код за онлайн интерфейса";
    }

    public override string ControlTheBotUsingAFancyInterface()
    {
        return "Контролирай бота през яко изглеждащ сайт.";
    }

    public override string SendingADirectMessageContainingTheInformation()
    {
        return "Изпращам лично съобщение, съдържащо информацията.";
    }

    public override string YourWebUiCodeIs()
    {
        return "Твоя код за онлайн интерфейса е";
    }

    public override string FailedToMove()
    {
        return "Неуспешно преместване.";
    }

    public override string Moved(int itemOne, string name, int item2)
    {
        return $"Преместих ({itemOne}) \"{name}\" на мястото ({item2})";
    }

    public override string InvalidMoveFormat()
    {
        return "Невалиден формат на местене.\n" +
               "Трябва да използваш две числа или да използваш формата написан под това съобщение:\n\n" +
               "-move Exact Name !to Exact Name 2";
    }

    public override string SwitchedThePlacesOf(string itemOne, string itemTwo)
    {
        return $"Размених местата на \"{itemOne}\" и \"{itemTwo}\"";
    }

    public override string CurrentQueue()
    {
        return "Сегашния списък:";
    }

    public override string TechTip()
    {
        return "\n\nВреме за един съвет. " +
               "\nПрепоръчвам ти да използваш онлайн интерфейса на бота, защото той показва списъка автоматично. " +
               "\nС него, ти можеш да добавяш, махаш, или да контролираш бота. " +
               "\nМожеш да го ползваш с командата \"-getwebui\". " +
               "\nБота ще ти изпрати лично съобщение с нужната информацията.";
    }

    public override string GoingTo(int index, string thing)
    {
        return $"Пускам ({index}) - \"{thing}\"";
    }

    public override string SetVolumeTo(double volume)
    {
        return $"Настройване на звука на: {volume}%";
    }

    public override string InvalidVolumeRange()
    {
        return "Невалидна настройка на звука. Трябва да е между 0 and 200 процента.";
    }

    public override string QueueSavedSuccessfully(string token)
    {
        return $"Списъка е запазен успешно. \n\nМожеш да го пуснеш отново с командата: \"-p pl:{token}\", " +
               "или ако го прикачиш като използваш \"play\" командата.";
    }

    public override string OneCannotRecieveBlessingNotInChannel()
    {
        //return "One cannot recieve the blessing of playback if they're not in a channel.";
        return "Един даден човешки индивид не може да получи просветлението на звук ако не е в канал.";
    }

    public override string OneCannotRecieveBlessingNothingToPlay()
    {
        return "Един даден човешки индивид не може да получи просветлението на звук ако няма нищо за слушане.";
    }

    public override string UserNotInChannelLyrics()
    {
        return "Влез в канал преди да използваш \"lyrics\" командата без да търсиш песен с нея.";
    }

    public override string BotNotInChannelLyrics()
    {
        return
            "Бота не е в канала. Ако искаш да знаеш текста на някоя песен, напиши ѝ името след тази команда.";
    }

    public override string NoResultsFoundLyrics(string search)
    {
        return $"Не бяха намерени резултати за: \"{search}\".";
    }

    public override string LyricsLong()
    {
        return
            "Текста на песента е над 2000 букви, което е лимита на Discord за текст. Изпращам текста като файл.";
    }

    public override string YouAreNotInTheChannel()
    {
        return "Не си в сегашния канал на бота.";
    }

    public override string ShufflingTheQueue()
    {
        return "Размешване на списъка.";
    }

    public override string SkippingOneTime()
    {
        return "Пропускам един път.";
    }

    public override string PausingThePlayer()
    {
        return "Спирам музиката.";
    }

    public override string UnpausingThePlayer()
    {
        return "Продължавам музиката.";
    }

    public override string SkippingOneTimeBack()
    {
        return "Връщам се един път назад.";
    }

    public override string Playing()
    {
        return "Сега се слуша";
    }

    public override string RequestedBy()
    {
        return "Добавено от";
    }

    public override string NextUp()
    {
        return "Следва";
    }

    public override string DefaultStatusbarMessage()
    {
        return
            "ВАЖНО СЪОБЩЕНИЕ: \n YouTube отново се изпика на труда ми, извинете ме. Използвайте линкове за сега.";
    }

    public override string DiscordDidTheFunny()
    {
        return
            "Discord направи смешното на бота, и заради това той се опита да се върже отново в канала. Ако е спрял, пропуснете веднъж назад и се върнете.";
    }

    public override string GayRatePercentMessage(int percent)
    {
        return percent switch
        {
            < 2 => "You're so straight, that scientists want to study how perfect you are.",
            >= 2 and < 10 => "Your gay level is so low, it's basically non-existant.",
            >= 10 and < 20 => "You have some gay quirks, but you're still straight.",
            >= 20 and < 40 => "You have some subtle gayness but you can still be called straight.",
            >= 40 and < 70 => "Your gay level is mediocre, but you can still be saved.",
            >= 70 and < 80 => "Your gay level is getting high. This isn't turning out great.",
            >= 80 and < 90 => "Your gay level is higher that Mount Everest, may god send you help.",
            >= 90 and < 100 => "Your gay level is so high, that you're among the world's greatest gays.",
            >= 100 => "You are omega gay."
        };
    }

    public override string GetTypeOfTrack(PlayableItem it)
    {
        return it switch
        {
            YoutubeVideoInformation yt when yt.GetIfLiveStream() => "Youtube Live Stream",
            YoutubeVideoInformation yt when !yt.GetIfLiveStream() => "Youtube Видео",
            OnlineFile => "Онлайн Файл",
            SpotifyTrack => "Песен от Spotify",
            SystemFile => "Локален Файл",
            TtsText => "Text to Speech",
            TwitchLiveStream => "Twitch Live Stream",
            YoutubeOverride => "Избран Запис",
            MusicObject => "Музика",
            _ => "Нещо"
        };
    }

    public override string SavedQueueAfterLeavingMessage()
    {
        return "Списъка на бота е запазен успешно. " +
               "Можеш да го пуснеш отново като натиснеш бутона под това съобщение.\n\n" +
               $"ВАЖНО СЪОБЩЕНИЕ: \nЗабравих да сменя това съобщение преди {MonthsSinceLastMessageUpdate()} месеца. 💀";
    }

    #region Slash Commands

    public override string SlashHello()
    {
        return "Здравей!";
    }

    public override string SlashNotInChannel()
    {
        return "Не можеш да използваш тази команда, ако не си в гласов канал.";
    }

    public override string SlashPlayCommand(string term)
    {
        return $"Търсене на: \"{term}\"";
    }

    public override string SlashBotNotInChannel()
    {
        return "Бота не е в сегашния гласов канал.";
    }

    public override string SlashLeaving()
    {
        return "Напускане на канала.";
    }

    public override string SlashSkipping(int times, bool back = false)
    {
        if (times >= 0) return $"Пропускане {(times == 1 ? "един път" : times)} {(back ? "назад" : "напред")}.";
        times = Math.Abs(times);
        return $"Пропускане {(times == 1 ? "един път" : times)} назад.";
    }

    public override string SlashPausing()
    {
        return "Спиране на пауза.";
    }

    public override string SlashPrayingToTheRngGods()
    {
        return "Ти се помоли на RNG боговете.";
    }

    public override string UpdatingToken()
    {
        return "Старият ти код е изтрит. Ще получиш съобщение съдържащо новия ти код.";
    }

    #endregion
}
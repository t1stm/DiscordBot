using System;
using DiscordBot.Abstract;
using DiscordBot.Audio.Objects;
using DiscordBot.Enums;

namespace DiscordBot.Objects;

public class English : AbstractLanguage
{
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
            YoutubeVideoInformation yt when !yt.GetIfLiveStream() => "Youtube Video",
            OnlineFile => "Online File",
            SpotifyTrack => "Spotify Track",
            SystemFile => "Local File",
            TtsText => "Text to Speech",
            TwitchLiveStream => "Twitch Live Stream",
            YoutubeOverride => "Chosen Track",
            MusicObject => "Music",
            _ => "Item"
        };
    }

    public override string SavedQueueAfterLeavingMessage()
    {
        return
            "Queue was saved successfully. If you want to play it again, use the button below this message.\n\n" +
            $"IMPORTANT MESSAGE: \nI forgor to change this message {MonthsSinceLastMessageUpdate()} months ago. 💀";
    }

    #region Mixed Commands

    public override string EnterChannelBeforeCommand(string command)
    {
        return $"Enter a channel before using the \"{command}\" command.";
    }

    public override string NoFreeBotAccounts()
    {
        return "No free bot accounts in this guild. You can add more bot accounts from the bot's support server.";
    }

    public override string ThisMessageWillUpdateShortly()
    {
        return "Hello! This message will update shortly.";
    }

    public override string SelectVideo()
    {
        return "Select a video.";
    }

    public override string SelectVideoTimeout()
    {
        return "Time to select video ran out.";
    }

    public override string NoResultsFound(string term)
    {
        return $"No results could be found for the search term: \"{term}\"";
    }

    public override string AddedItem(string term)
    {
        return $"Added: {term}";
    }

    public override string BotIsNotInTheChannel()
    {
        return "The bot isn't in the channel.";
    }

    public override string CouldNotFindCommand(string command)
    {
        return $"Couldn't find command: \"{command}\"";
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
        return $"Specified number: {number} is bigger than the Queue's Length. Searching the number instead.";
    }

    public override string PlayingItemAfterThis(int index, string name)
    {
        return $"Playing: ({index}) - \"{name}\" after this.";
    }

    public override string PlayingItemAfterThis(string term)
    {
        return $"Playing: \"{term}\" after this.";
    }

    public override string FailedToRemove(string text)
    {
        return $"Failed to remove: \"{text}\"";
    }

    public override string RemovingItem(string name)
    {
        return $"Removing \"{name}\"";
    }

    public override string YouHaveAlreadyGeneratedAWebUiCode()
    {
        return "You have already generated a Web UI code.";
    }

    public override string ControlTheBotUsingAFancyInterface()
    {
        return "Control the bot using a fancy interface.";
    }

    public override string SendingADirectMessageContainingTheInformation()
    {
        return "Sending a Direct Message containing the information.";
    }

    public override string YourWebUiCodeIs()
    {
        return "Your Web UI Code is";
    }

    public override string FailedToMove()
    {
        return "Failed to move.";
    }

    public override string Moved(int itemOne, string name, int item2)
    {
        return $"Moved ({itemOne}) \"{name}\" to ({item2})";
    }

    public override string InvalidMoveFormat()
    {
        return "Invalid move format.\n" +
               "You must use two numbers or use the format specified below:\n\n" +
               "-mv Exact Name !to Exact Name 2 ";
    }

    public override string SwitchedThePlacesOf(string itemOne, string itemTwo)
    {
        return $"Switched the places of \"{itemOne}\" and \"{itemTwo}\"";
    }

    public override string CurrentQueue()
    {
        return "Current Queue:";
    }

    public override string TechTip()
    {
        return "\n\nHere's a tech tip. " +
               "\nYou can use the bot web interface which displays the list automatically. " +
               "\nYou can add, remove and overall control the bot using a spicy looking interface. " +
               "\nYou can use it with the -webui command. " +
               "\nThe bot will DM you a link which you can use to login, and a token for authentication.";
    }

    public override string GoingTo(int index, string thing)
    {
        return $"Going to ({index}) - \"{thing}\"";
    }

    public override string SetVolumeTo(double volume)
    {
        return $"Set the volume to {volume}%";
    }

    public override string InvalidVolumeRange()
    {
        return "Invalid volume range. Must be between 0 and 200%.";
    }

    public override string QueueSavedSuccessfully(string token)
    {
        return $"Queue saved sucessfully. \n\nYou can play it again with this command: \"-p pl:{token}\", " +
               "or by sending the attached file and using the play command.";
    }

    public override string OneCannotRecieveBlessingNotInChannel()
    {
        return "One cannot recieve the blessing of playback if they're not in a channel.";
    }

    public override string OneCannotRecieveBlessingNothingToPlay()
    {
        return "One cannot recieve the blessing of playback if there's nothing to play.";
    }

    public override string UserNotInChannelLyrics()
    {
        return "Enter a channel before using the lyrics command without a search term.";
    }

    public override string BotNotInChannelLyrics()
    {
        return
            "The bot isn't in the channel. If you want to know the lyrics of a song add it's name after the command.";
    }

    public override string NoResultsFoundLyrics(string search)
    {
        return $"No results found for \"{search}\".";
    }

    public override string LyricsLong()
    {
        return
            "The lyrics are longer than 2000 characters, which is Discord's length limit. Too bad. Sending song as a file.";
    }

    #endregion

    #region Player Interations

    public override string YouAreNotInTheChannel()
    {
        return "You're not in the bot's current channel.";
    }

    public override string ShufflingTheQueue()
    {
        return "Shuffling the queue.";
    }

    public override string SkippingOneTime()
    {
        return "Skipping one time";
    }

    public override string PausingThePlayer()
    {
        return "Pausing the player.";
    }

    public override string UnpausingThePlayer()
    {
        return "Unpausing the player.";
    }

    public override string SkippingOneTimeBack()
    {
        return "Skipping one time back.";
    }

    public override string Playing()
    {
        return "Playing";
    }

    public override string RequestedBy()
    {
        return "Requested by";
    }

    public override string NextUp()
    {
        return "Next";
    }

    #endregion

    #region Player Messages

    public override string DefaultStatusbarMessage()
    {
        return
            "The bot is currently being reworked majorly, so please note that there may be many bugs. Sorry for any bugs in advance.\n\n" +
            "IMPORTANT MESSAGE: \n YouTube pissed on my hard work again, so at the moment searching doesn't work. Please use links for the time being.";
    }

    public override string DiscordDidTheFunny()
    {
        return
            "Discord did the funny, so the bot tried to reconnect. If the playback stopped skip one time back and return to the current item.";
    }

    #endregion

    #region Slash Commands

    public override string SlashHello()
    {
        return "Hello!";
    }

    public override string SlashNotInChannel()
    {
        return "You cannot use this command while not being in a channel.";
    }

    public override string SlashPlayCommand(string term)
    {
        return $"Running play command with search term: \"{term}\"";
    }

    public override string SlashBotNotInChannel()
    {
        return "The bot isn't in the current voice channel.";
    }

    public override string SlashLeaving()
    {
        return "Leaving.";
    }

    public override string SlashSkipping(int times, bool back = false)
    {
        if (times >= 0) return $"Skipping {(times == 1 ? "one time" : times)}{(back ? " back" : "")}.";
        times = Math.Abs(times);
        return $"Skipping {(times == 1 ? "one time" : times)} back.";
    }

    public override string SlashPausing()
    {
        return "Pausing the current item.";
    }

    public override string SlashPrayingToTheRngGods()
    {
        return "Praying to the RNG gods.";
    }

    public override string UpdatingToken()
    {
        return "Resetting your client token. You will recieve a message containing the information.";
    }

    #endregion
}
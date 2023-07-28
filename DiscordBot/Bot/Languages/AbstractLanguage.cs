using System;
using DiscordBot.Abstract;
using DiscordBot.Enums;

namespace DiscordBot.Objects;

public abstract class AbstractLanguage
{
    public int MonthsSinceLastMessageUpdate()
    {
        var last_update_date = new DateTime(2023, 3, 28, 0, 00, 0);
        var now = DateTime.Now;

        return new DateTime(now.Ticks - last_update_date.Ticks).Month;
    }

    public abstract string GayRatePercentMessage(int percent);
    public abstract string GetTypeOfTrack(PlayableItem it);
    public abstract string SavedQueueAfterLeavingMessage();

    #region Mixed Commands

    public abstract string EnterChannelBeforeCommand(string command);
    public abstract string NoFreeBotAccounts();
    public abstract string ThisMessageWillUpdateShortly();
    public abstract string SelectVideo();
    public abstract string SelectVideoTimeout();
    public abstract string NoResultsFound(string term);
    public abstract string AddedItem(string term);
    public abstract string BotIsNotInTheChannel();
    public abstract string CouldNotFindCommand(string command);
    public abstract string LoopStatusUpdate(Loop loop);
    public abstract string NumberBiggerThanQueueLength(int number);
    public abstract string PlayingItemAfterThis(int index, string name);
    public abstract string PlayingItemAfterThis(string term);
    public abstract string FailedToRemove(string text);
    public abstract string RemovingItem(string name);
    public abstract string YouHaveAlreadyGeneratedAWebUiCode();
    public abstract string ControlTheBotUsingAFancyInterface();
    public abstract string SendingADirectMessageContainingTheInformation();
    public abstract string YourWebUiCodeIs();
    public abstract string FailedToMove();
    public abstract string Moved(int itemOne, string name, int item2);
    public abstract string InvalidMoveFormat();
    public abstract string SwitchedThePlacesOf(string itemOne, string itemTwo);
    public abstract string CurrentQueue();
    public abstract string TechTip();
    public abstract string GoingTo(int index, string thing);
    public abstract string SetVolumeTo(double volume);
    public abstract string InvalidVolumeRange();
    public abstract string QueueSavedSuccessfully(string token);
    public abstract string OneCannotRecieveBlessingNotInChannel();
    public abstract string OneCannotRecieveBlessingNothingToPlay();
    public abstract string UserNotInChannelLyrics();
    public abstract string BotNotInChannelLyrics();
    public abstract string NoResultsFoundLyrics(string search);
    public abstract string LyricsLong();

    #endregion

    #region Player Interations

    public abstract string YouAreNotInTheChannel();
    public abstract string ShufflingTheQueue();
    public abstract string SkippingOneTime();
    public abstract string PausingThePlayer();
    public abstract string UnpausingThePlayer();
    public abstract string SkippingOneTimeBack();
    public abstract string Playing();
    public abstract string RequestedBy();
    public abstract string NextUp();

    #endregion

    #region Player Messages

    public abstract string DefaultStatusbarMessage();
    public abstract string DiscordDidTheFunny();

    #endregion

    #region Slash Commands

    public abstract string SlashHello();
    public abstract string SlashNotInChannel();
    public abstract string SlashPlayCommand(string term);
    public abstract string SlashBotNotInChannel();
    public abstract string SlashLeaving();
    public abstract string SlashSkipping(int times, bool back = false);
    public abstract string SlashPausing();
    public abstract string SlashPrayingToTheRngGods();
    public abstract string UpdatingToken();

    #endregion
}
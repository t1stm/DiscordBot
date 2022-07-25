using DiscordBot.Abstract;

namespace DiscordBot.Objects
{
    public interface ILanguage
    {
        #region Mixed Commands
        
        public string EnterChannelBeforeCommand(string command);
        public string NoFreeBotAccounts();
        public string ThisMessageWillUpdateShortly();
        public string SelectVideo();
        public string SelectVideoTimeout();
        public string NoResultsFound(string term);
        public string AddedItem(string term);
        public string BotIsNotInTheChannel();
        public string CouldNotFindCommand(string command);
        public string LoopStatusUpdate(Enums.Loop loop);
        public string NumberBiggerThanQueueLength(int number);
        public string PlayingItemAfterThis(int index, string name);
        public string PlayingItemAfterThis(string term);
        public string FailedToRemove(string text);
        public string RemovingItem(string name);
        public string YouHaveAlreadyGeneratedAWebUiCode();
        public string ControlTheBotUsingAFancyInterface();
        public string SendingADirectMessageContainingTheInformation();
        public string YourWebUiCodeIs();
        public string FailedToMove();
        public string Moved(int itemOne, string name, int item2);
        public string InvalidMoveFormat();
        public string SwitchedThePlacesOf(string itemOne, string itemTwo);
        public string CurrentQueue();
        public string TechTip();
        public string GoingTo(int index, string thing);
        public string SetVolumeTo(double volume);
        public string InvalidVolumeRange();
        public string QueueSavedSuccessfully(string token);
        public string OneCannotRecieveBlessingNotInChannel();
        public string OneCannotRecieveBlessingNothingToPlay();
        public string UserNotInChannelLyrics();
        public string BotNotInChannelLyrics();
        public string NoResultsFoundLyrics(string search);
        public string LyricsLong();
        
        #endregion
        
        #region Player Interations
        public string YouAreNotInTheChannel();
        public string ShufflingTheQueue();
        public string SkippingOneTime();
        public string PausingThePlayer();
        public string UnpausingThePlayer();
        public string SkippingOneTimeBack();
        public string Playing();
        public string RequestedBy();
        public string NextUp();
        
        #endregion
        
        #region Player Messages
        
        public string DefaultStatusbarMessage();
        public string DiscordDidTheFunny();
        
        #endregion
        
        public string GayRatePercentMessage(int percent);
        
        #region Slash Commands

        public string SlashHello();
        public string SlashNotInChannel();
        public string SlashPlayCommand(string term);
        public string SlashBotNotInChannel();
        public string SlashLeaving();
        public string SlashSkipping(int times, bool back = false);
        public string SlashPausing();
        public string SlashPrayingToTheRngGods();
        public string UpdatingToken();
        
        #endregion
        public string GetTypeOfTrack(PlayableItem it);
        public string SavedQueueAfterLeavingMessage(string cmd);
    }
}
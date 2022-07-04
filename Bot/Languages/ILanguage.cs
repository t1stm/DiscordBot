namespace DiscordBot.Objects
{
    public interface ILanguage
    {
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
        public string YouAreNotInTheChannel();
        public string ShufflingTheQueue();
        public string SkippingOneTime();
        public string PausingThePlayer();
        public string UnpausingThePlayer();
        public string SkippingOneTimeBack();
    }
}
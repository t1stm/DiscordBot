namespace DiscordBot.Miscellaneous
{
    public static class HelpMessages
    {
        #region Home Command

        private const string HomeHelp =
            @"These are all the commands this bot has to offer. In some commands n and m are places of an item.
`-help play`
If you want to see more information regarding a command you can specify the command which you want to know about, like for example it's prefixes or some complex behavior.
You can replace the - with = or /
`-play search term or link here`
This is the play command, it makes the bot join the current voice channel and starts playing whatever it's given.
`-playnext search term or link here` `playnext n`
This command is the same as the previous one, but with one twist. The result is placed after the current track.
`-playselect`
This command allows you to select a result when searching.
`-queue`
This command lists all the items in the queue, and writes their position.
`-leave`
This command makes the bot leave if it's in a channel.
`-skip` `-skip n` `-back` `-back n`
This command skips or goes back once or **n** times.
`-shuffle`
This command shuffles the current queue.
`-loop`
This command loops the whole queue or one item.
`-pause`
This command pauses the current item.
`-move n m`
This command moves an item to a new place in the queue.
`-remove n`
This command removes an item from the queue.
`-queue`
This command sends the current queue.
`-clear`
This command clears the whole queue except the current playing song.
`-getwebui`
This command DM's you a token to use in the bot's web interface. You don't have to use it but you can at least take a look at it and see if you like it.
`-goto n`
This command goes to an item at the given index.
`-lyrics` `lyrics Song - Author`
This command searches for the lyrics of an item. This command is **NOT** reliable most of the times.
`-meme`
This command sends a meme from the collection at my website.
`-saveplaylist`
This command saves a playlist which you can use to resume your session.
";

        #endregion

        public static string GetMessage(string category)
        {
            return category switch
            {
                "home" => HomeHelp,
                "play" or "p" => PlayCommand,
                "playnext" or "pn" => PlayNextCommand,
                "playselect" or "ps" => PlaySelectCommand,
                "skip" => SkipCommand,
                "leave" or "l" => LeaveCommand,
                "back" => BackCommand,
                "shuffle" => ShuffleCommand,
                "loop" => LoopCommand,
                "pause" => PauseCommand,
                "remove" or "rm" or "r" => RemoveCommand,
                "move" or "mv" => MoveCommand,
                "list" or "queue" => ListCommand,
                "clear" => ClearCommand,
                "webui" or "getwebui" or "wu" => GetWebUiCommand,
                "goto" or "go" => GoToCommand,
                "saveplaylist" or "savequeue" or "sq" or "sp" => SavePlaylistCommand,
                "lyrics" => LyricsCommand,
                "getavatar" => GetAvatarCommand,
                "meme" => MemeCommand,
                "plsfix" => PlsFixCommand,
                _ => null
            };
        }

        #region Other Commands

        private const string GetAvatarCommand = @"`-getavatar @SomeUser`
This command sends the specified user's avatar.";

        private const string MemeCommand = @"`-meme`
This command sends a meme from the site's meme page.";

        #endregion

        #region Play Commands

        private const string PlayCommand = @"`-play search term or link here` Aliases: `-p` `-п` `-плаъ`
This is the play command. There is not much to explain for this command. You enter what you want to play, and if it exists on YouTube or is a Discord attachment or a link, the bot will play it.";

        private const string PlayNextCommand =
            @"`-playnext search term or link here` `-playnext n` Aliases: `-pn` `-плаън` `пн`
Really there is not much to explain for this command. It behaves like the play command but instead of putting the video/song/playlist/album at the end of the queue, it puts it after the current track";

        private const string PlaySelectCommand = @"`-playselect search term here` Aliases: `-ps`
This command lists all the results of the given search term and allows the sender to choose an item.";

        #endregion

        #region Player Interactions

        private const string SkipCommand = @"`-skip` `-skip n` Aliases: `-next` `-скип` `-неьт` 
Pretty much everything was already explained. This command skips one time if a number isn't specified and skips n times if it is. You can also enter negative numbers, but there's no need because of the -back command";

        private const string BackCommand =
            @"`-back` `-back n` Aliases: `-previous` `-prev` `-бацк` `-прев` `-прежиоус` 
Pretty much everything was already explained. This command skips one time if a number isn't specified and skips n times if it is. You can also enter negative numbers, but there's no need because of the -back command";

        private const string LeaveCommand =
            @"`-leave` Aliases: `-l` `-stop` `-леаже` `-л` `-стоп` `-с` `-s` `-die` `-дие`
This is a self explanatory command, so I don't see any need in writing an explanation for it.";

        private const string ShuffleCommand = @"`-shuffle` Aliases: `-rand` `-схуффле` `-ранд`
This command is also pretty self explanatory, I hope.";

        private const string LoopCommand = @"`-loop` Aliases: `-лооп`
This command loops the queue. Let's dig into how the player works. 
The player has three states when it comes to looping.
1. None
2. Looping whole queue
3. Looping one item

This command switches between them.";

        private const string PauseCommand = @"`-pause` Aliases: `-паусе`
This command pauses the current track.";

        private const string RemoveCommand =
            @"`-remove n m x y z` `-remove Song - Author` Aliases: `-r` `-rm` `-реможе` `-рм` `-р`
This command removes items from the queue. It can remove multiple items listed with spaces or commas or remove an item using it's name and author.";

        private const string MoveCommand =
            @"`-move n m` `-move Song1 - Author1 !to Song2 - Author2` Aliases: `-m` `-mv` `-м` `-мж` `-може`
This command moves an item from its place to the specified place. If the other move format is used this command will swap the places of the two items which are specified, however this isn't very accurate, and won't work if there are multiple items which are the with the same title.";

        private const string ListCommand = @"`-queue` Aliases: `-list` `-яуеуе` `-лист`
This command sends the current queue as a .txt file in order to avoid Discord's 2000 character limit. This shouldn't pose a problem, as the desktop version of Discord supports previewing .txt files. 
To all mobile users, I am sorry, because it seems like Discord won't implement this feature soon,";

        private const string ClearCommand = @"`-clear`
This command clears the queue leaving only the currently playing song, or if waiting to disconnect: last song.";

        private const string GetWebUiCommand =
            @"`-getwebui` Aliases: `-webui` `-wu` `and their Bulgarian phonetic keyboard counterparts`
This command DM's you the token for the bot interface along with a qr code you can scan on your phone. The token is needed in order to not let trolls troll others.";

        private const string GoToCommand =
            @"`-goto n` Aliases: `go` `skipto` `гото` `го` `скипто`
This command is pretty self explanatory. It goes to the element which place on the queue is n.";

        private const string SavePlaylistCommand =
            @"`-saveplaylist` Aliases: `-savequeue` `-sq` `-sp` `-сажеяуеуе` `-сажеплаълист`
This command saves the current queue using a file format which I created. When saving, the command will give you the instructions of how to use the playlist again. 
For the fellow developers these next few lines will explain how the data is stored, if they want to use it for some reason, which I doubt but here I am writing about this.

The file starts with the bytes (0-255) 84,7,70,60 (which is an easter egg that spells BatTo6o534)  (6 is read like sh in Bulgarian when used in a word, this is the so called schlokavica - шльокавица)
After these bytes are the padding bytes 00,02 - These two bytes indicate to the decoder where the new string starts.
After the padding bytes the first byte of the data is the ""encoding byte"" this is a special byte which indicates what type of encoding the current string uses. It's values are 00 - UTF8, 01 - ASCII, and 02 - ASCII but with a twist, I brutally murdered it to allow it to store bulgarian characters in one byte. So in one word it's ASCII and not.
After that until the next padding bytes (00,02) , it's pure data. 
For more information you can DM the developer on Discord. You can find him in the bot's support Discord Guild.
";

        private const string LyricsCommand = @"`-lyrics` `-lyrics Song - Author`
This command is pretty well explained in the home page of the help command. It uses an API to search the lyrics, so it may not be as accurate as you're going to be when you search for them yourself.";

        private const string PlsFixCommand = @"`-plsfix`
This command makes you pray to the RNG gods, and if you recieve a blessing, the bot will restart the current item.";

        #endregion
    }
}
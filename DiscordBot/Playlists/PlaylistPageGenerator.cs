using System.Linq;
using System.Threading.Tasks;
using CustomPlaylistFormat.Objects;
using DiscordBot.Abstract;
using DiscordBot.Audio.Platforms;

namespace DiscordBot.Playlists;

public static class PlaylistPageGenerator
{
    public static async Task<string> GenerateNormalPage(Playlist playlist)
    {
        var name = playlist.Info?.Name;
        var description = playlist.Info?.Description;
        var guid = playlist.Info?.Guid.ToString();
        var maker = playlist.Info?.Maker;
        var pageTitle = $"\'{name}\' \nMade by: \'{maker}'";
        var image = $"https://playlists.{Bot.MainDomain}/Image/{guid}";
        var value =

            #region HTML Boilerplate

            $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
    <title>{pageTitle}</title>
    <meta content=""#0E5445"" name=""theme-color"">
    <meta property=""og:site_name"" content=""{Bot.MainDomain} Playlists"">
    <meta property=""og:type"" content=""website"">
    <meta property=""og:url"" content=""https://playlists.{Bot.MainDomain}/"">
    <meta name=""title"" content=""{pageTitle}"">
    <meta name=""description"" content=""{description}"">
    <meta property=""og:title"" content=""{pageTitle}"">
    <meta property=""og:description"" content=""{description}"">
    <meta property=""og:image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail/{guid}"">
    <meta property=""image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail/{guid}"">
    <meta property=""og:image:width"" content=""1200"" />
    <meta property=""og:image:height"" content=""540"" />
    <meta content=""summary_large_image"" name=""twitter:card"">
    <meta content=""@t1stm"" name=""twitter:creator"">
    <meta content=""@t1stm"" name=""twitter:site"">
    <meta content=""{pageTitle}"" name=""twitter:title"">
    <meta content=""{description}"" name=""twitter:description""> 
    <meta property=""twitter:image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail/{guid}"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=4""/>
</head>
<style>
    body {{
        background-color: black;
    }}
    p,label {{
        font-family: sans-serif;
    }}
    #bkg {{
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(135deg, #385F71, #558be0, #23a6d5, #23d5ab);
        background-size: 100% 100%;
        opacity: 40%;
    }}
    #box {{
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: linear-gradient(-115deg, #385F71, #2B4162);
        position: absolute;
        display: flex;
        flex-direction: row;
        z-index: 2;
        width: 20vw;
        height: 50%;
        border-radius: 16px;
        align-items: center;
        padding: 2rem;
        transition-duration: 300ms;

    }}
    #playlistInfo {{
        position: relative;
        width: 20vw;
        height: 100%;
        overflow: auto;
        display: flex;
        flex-direction: column;
        align-items: center;
        background: transparent;
        border-radius: 16px;
        transition-duration: 300ms;
        flex-shrink: 0;
    }}
    #playlistImage {{
        margin-top: 1rem;
        width: 95%;
        height: auto;
        object-fit: contain;
        margin-bottom: 0;
        transition-duration: 300ms;
    }}
    #playlistName {{
        color: white;
        font-size: 2rem;
        font-weight: bold;
        margin-bottom: 0;
    }}
    #playlistCreator {{
        color: white;
        font-size: 1.1rem;
        font-weight: bold;
        margin-bottom: 0.5rem;
        opacity: .80;
    }}
    #playlistCreator::before {{
        content: ""Made by: "";
    }}
    #playlistDescription {{
        color: white;
        font-size: 1rem;
        opacity: .80;
        text-align: center;
        transition-duration: 300ms;
    }}
    #showMore {{
        background-color: #75507B;
        padding: 0.5rem;
        border-radius: 10px;
        color: #FFFFFFCC;
        font-weight: bold;
        transition-duration: 100ms;
    }}
    #showMore::before {{
        content: ""Toggle playlist items.""
    }}
    #showMoreButton {{
        z-index: 5;
    }}
    #playSiteButton {{
        background-color: #75507B;
        padding: 0.5rem;
        border-radius: 10px;
        color: #FFFFFFCC;
        font-weight: bold;
        transition-duration: 100ms;
        border-style: none;
        margin: 1rem;
    }}
    #showMore:focus,
    #showMore:hover,
    #playSiteButton:focus,
    #playSiteButton:hover{{
        outline: solid rgba(255, 243, 243, 0.5) 4px;
        cursor: pointer;
    }}
    #showMoreButton:checked ~ #box {{
        width: 50%;
    }}
    #showMoreButton {{
        visibility: hidden;
    }}
    #showMoreButton:checked ~ #box #playlist {{
        width: 90%;
    }}
    #playlist {{
        position: relative;
        right: 0;
        transition-duration: 300ms;
        flex-direction: column;
        height: 100%;
        width: 0;
        padding: 0;
        list-style: none;
        border-radius: 12px;
        background-color: transparent;
        overflow-y: auto;
        overflow-x: hidden;
    }}
    .playlistItem {{
        position: relative;
        display: flex;
        width: auto;
        height: auto;
        background: transparent;
        align-items: center;
        margin: 0.5rem;
        border-radius: 10px;
        transition-duration: 100ms;
    }}
    .playlistItem:hover,
    .playlistItem:focus {{
        background: rgba(33,33,33,0.30);
    }}
    .info {{
        width: 100%;
        height: 100%;
        margin-left: 1rem;
        display: flex;
        flex-direction: column;
        justify-content: space-evenly;
    }}
    .playlistItemTitle {{
        font-size: 1.1rem;
        font-weight: bold;
        margin: 0;
    }}
    .playlistItemAuthor{{
        font-size: 0.8rem;
        font-weight: bold;
        margin: 0;
        opacity: .80;
    }}
    .playlistItemAuthor {{
        margin: 0;
    }}
    .info * {{
        color: white;
    }}
    .playlistItem > img {{
        margin-left: 0.5rem;
        height: 4.5rem;
        width: 8rem;
        object-fit: contain;
        border-radius: 10px;
    }}

    @media screen and (orientation: portrait) {{
        #box {{
            width: calc(90% - 4rem);
            height: 50%;
            flex-direction: column;
        }}

        #playlistInfo {{
            width: 100%;
            height: 100%;
        }}

        #showMoreButton:checked ~ #box {{
            height: 90%;
            width: 90%;
            padding: 0;
        }}

        #showMoreButton:checked ~ #box #playlistInfo {{
            width: 100%;
            height: 30%;
        }}

        #showMoreButton:checked ~ #box #playlistInfo #playlistImage {{
            opacity: 0;
            transform: scaleY(0);
            height: 0;
        }}

        #showMoreButton:checked ~ #box #playlistInfo #playlistDescription {{
            opacity: 0;
            height: 0;
            transform: scaleY(0);
            display: none;
        }}
        #playlist {{
            width: 100%;
            height: 0;
        }}
        #showMoreButton:checked ~ #box #playlist {{
            height: 100%;
        }}
    }}
</style>
<body>
<input id=""showMoreButton"" type=""checkbox""/>
<div id=""box"">
    <div id=""playlistInfo"">
        <img src=""{image}"" alt=""Playlist image here."" id=""playlistImage"">
        <p id=""playlistName"">{name}</p>
        <p id=""playlistCreator"">{maker}</p>
        <p id=""playlistDescription"">{description}</p>
        <label id=""showMore"" for=""showMoreButton""></label>
        <a href=""{Bot.SiteDomain}/{Bot.WebUiPage}?play={guid}""><button id=""playSiteButton"">Play here.</button></a>
    </div>
    <ul id=""playlist"">
";

        #endregion

        if (playlist.PlaylistItems != null)
            foreach (var entry in playlist.PlaylistItems)
            {
                var video = $"{PlaylistManager.ItemTypeToString(entry.Type)}://{entry.Data}";
                var search = await Search.Get(video);
                if (search != Status.OK) continue;
                var result = search.GetOK().First();
                value += $@"<li class=""playlistItem"">
            <img src=""{(string.IsNullOrEmpty(result?.GetThumbnailUrl()) ? $"https://{Bot.MainDomain}/WebUi/NoVideoImage.png" : result.GetThumbnailUrl())}"" alt=""Item Image"">
            <div class=""info"">
                <p class=""playlistItemTitle"">{result?.GetTitle()}</p>
                <p class=""playlistItemAuthor"">{result?.GetAuthor()}</p>
            </div>
        </li>";
            }

        value += "</ul>\n</div>\n<div id=\"bkg\"></div>\n</body>\n</html>";

        return value;
    }

    public static string GenerateNotFoundPage()
    {
        var value = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>Not Found</title>
    <meta content=""#0E5445"" name=""theme-color"">
    <meta property=""og:site_name"" content=""{Bot.MainDomain} Playlists"">
    <meta property=""og:type"" content=""website"">
    <meta property=""og:url"" content=""https://playlists.{Bot.MainDomain}/"">
    <meta name=""title"" content=""Not Found"">
    <meta name=""description"" content=""Requested playlist not found."">
    <meta property=""og:title"" content=""Not Found"">
    <meta property=""og:description"" content=""Requested playlist not found"">
    <meta property=""og:image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail?id=none"">
    <meta property=""image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail?id=none"">
    <meta property=""og:image:width"" content=""1200"" />
    <meta property=""og:image:height"" content=""540"" />
    <meta content=""summary_large_image"" name=""twitter:card"">
    <meta content=""@t1stm"" name=""twitter:creator"">
    <meta content=""@t1stm"" name=""twitter:site"">
    <meta content=""Not Found"" name=""twitter:title"">
    <meta content=""Requested playlist not found."" name=""twitter:description""> 
    <meta property=""twitter:image"" content=""https://playlists.{Bot.MainDomain}/Thumbnail?id=none"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=4""/>
</head>
<body>
" + @"
<style>
    body {
        background-color: black;
    }
    p,label {
        font-family: sans-serif;
    }
    #bkg {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: linear-gradient(135deg, #385F71, #558be0, #23a6d5, #23d5ab);
        background-size: 100% 100%;
        opacity: 40%;
    }
    #box {
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: linear-gradient(-115deg, #385F71, #2B4162);
        position: absolute;
        display: flex;
        flex-direction: row;
        z-index: 2;
        width: 20vw;
        height: 50%;
        border-radius: 16px;
        align-items: center;
        padding: 2rem;
        transition-duration: 300ms;

    }
    #text {
        color: white;
        text-align: center;

    }

    @media screen and (any-hover: none) and (orientation: portrait) {
        #box {
            width: 93%;
            padding: 2%;
            height: 20%;
        }
    }
</style>
<div id=""box"">
    <h1 id=""text"">Requested playlist not found.</h1>
</div>
<div id=""bkg""></div>
</body>
</html>";

        return value;
    }
}
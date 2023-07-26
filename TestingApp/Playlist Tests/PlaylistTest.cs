using CustomPlaylistFormat.Objects;
using DiscordBot;
using DiscordBot.Abstract;
using DiscordBot.Audio.Platforms;
using DiscordBot.Playlists;
using Result.Objects;

namespace TestingApp;

public static class PlaylistTest
{
    public static async Task Test()
    {
        Console.WriteLine("Starting PlaylistTest.");
        var notExistant = PlaylistManager.GetIfExists(new Guid());
        Console.WriteLine($"Non existant playlist is: \"{notExistant}\"");
        var songIds = new[]
        {
            "audio://vazhega0-vn",
            "audio://losandok-sd",
            "audio://tokoy-ba-Hq",
            "audio://togreshn-mC",
            "audio://bodrink--3S",
            "audio://sl100-sm-dT",
            "audio://sledno-f-mq",
            "audio://kadulsin-It",
            "audio://pokuco-m-FA",
            "audio://debeli-n-zr",
            "audio://tobedni--n9",
            "audio://tozhica,-k7",
            "audio://vaθα-της-1d",
            "audio://evradka--XY",
            "audio://deidi-si-mm",
            "audio://bopiyan0-bZ",
            "audio://migsm000-T5",
            "audio://rushto-t-1M",
            "audio://slprolog-ex",
            "audio://slfrensk-Mq",
            "audio://koprosto-oV",
            "audio://slmagyos-TA",
            "audio://rabeli-p-DG",
            "audio://rudurpay-XO",
            "audio://tonyama--Lh",
            "audio://diizturv-Y0",
            "audio://kazashto-bU",
            "audio://shtoshka-vA",
            "audio://toza-edn-mM",
            "audio://orti-nya-GF",
            "audio://ivneshto-ZZ",
            "audio://trσπάω-τ-OD",
            "audio://duivan00-r6",
            "audio://tomilion-kr",
            "audio://koataka--XN",
            "audio://to2,-3,--Lc",
            "audio://sisuperm-UA",
            "audio://prvodka--yn",
            "audio://ivsto-pa-UZ",
            "audio://komuni,--QN",
            "audio://rugot-mi-Oh",
            "audio://rabeli-p-DG",
            "audio://ambelgiy-xS",
            "audio://lomerced-jk",
            "audio://deprosto-wq",
            "audio://rashopsk-Yy",
            "audio://beergen0-ov",
            "audio://kasweet--kL",
            "audio://huad-i-r-HY",
            "audio://slsveti--I6",
            "audio://slmoya-s-e5",
            "audio://ornovo-g-Qw",
            "audio://totri-ki-mZ",
            "audio://orstuden-eg",
            "audio://tadudu00-CX",
            "audio://ivshampa-Gn",
            "audio://amguci,--pi",
            "audio://kaluda-p-FG",
            "audio://tomilion-SW",
            "audio://hustudio-mS",
            "audio://toako-ed-Pb",
            "audio://rubig-pi-my",
            "audio://kodoko,--QK",
            "audio://tonay-do-Rl",
            "audio://sli-bez--fH",
            "audio://ivgreshn-M6",
            "audio://varibna--Ub",
            "audio://vaφαινόμ-3k",
            "audio://ivkato-n-w1",
            "audio://shseksi0-qW",
            "audio://shako-si-l8",
            "audio://ortvoite-1s",
            "audio://orza-mil-tq",
            "audio://tokarava-Xk",
            "audio://kataysun-8k",
            "audio://tonema-p-fm",
            "audio://todvete--76",
            "audio://orrakiya-oT",
            "audio://or6-bez--8l",
            "audio://gaselska-RO",
            "audio://kaogun-m-T0",
            "audio://slela,-e-mP",
            "audio://slti-si0-kW",
            "audio://sloborot-CA",
            "audio://slnirvan-Q3",
            "audio://slpo-rub-3d",
            "audio://sliskam--kW",
            "audio://sldyavol-2R",
            "audio://sli-da-p-LZ",
            "audio://vazhigol-aa",
            "audio://vaseksin-Gz",
            "audio://vahali-g-Sx",
            "audio://vamaneke-6G",
        };
        var demoPlaylist = new List<PlayableItem>();

        foreach (var song in songIds)
        {
            var found_result = await Search.Get(song);
            if (found_result == Status.Error)
            {
                throw new Exception($"Found result error: {found_result.GetError()}");
            }

            var playableItem = found_result.GetOK().First();
            demoPlaylist.Add(playableItem);
        }

        var saved = PlaylistManager.SavePlaylist(demoPlaylist, new PlaylistInfo
        {
            Name = "Мазен Тираджия",
            Maker = "t1stm",
            Description = "Този плейлист съдържа най-мазните парчета направени в периода 1980-2010г."
        }, Guid.Parse("e30e0c85-e8c2-4d7c-848c-55ef32cca156"));
        var guid = saved?.Info?.Guid ?? Guid.Empty;
        var exists = PlaylistManager.GetIfExists(guid);
        M3U8Exporter.Export(saved!.Value);
        Console.WriteLine(
            $"({exists?.PlaylistItems?.Length}): \"{string.Concat(exists?.PlaylistItems?.Select(r => $"{r.Data},") ?? Array.Empty<string>())}\"");
    }
}
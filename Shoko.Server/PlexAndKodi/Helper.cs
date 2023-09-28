﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Shoko.Commons.Extensions;
using Shoko.Commons.Utils;
using Shoko.Models.Client;
using Shoko.Models.Enums;
using Shoko.Models.PlexAndKodi;
using Shoko.Server.Models;
using Shoko.Server.Repositories;
using Shoko.Server.Server;
using Shoko.Server.Utilities;
using Directory = Shoko.Models.PlexAndKodi.Directory;

namespace Shoko.Server.PlexAndKodi;

public static class Helper
{
    public static string ConstructVideoLocalStream(this IProvider prov, int userid, int vid, string name,
        bool autowatch)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            "Stream/" + vid + "/" + userid + "/" + autowatch + "/" + name, prov.IsExternalRequest());
    }

    public static string ConstructFileStream(this IProvider prov, int userid, string file, bool autowatch)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            "Stream/Filename/" + Base64EncodeUrl(file) + "/" + userid + "/" + autowatch, prov.IsExternalRequest());
    }

    public static string ConstructImageLink(this IProvider prov, int type, int id)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            ShokoServer.PathAddressREST + "/" + type + "/" + id);
    }

    public static string ConstructSupportImageLink(this IProvider prov, string name)
    {
        var relation = prov.GetRelation().ToString(CultureInfo.InvariantCulture);
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            ShokoServer.PathAddressREST + "/Support/" + name + "/" + relation);
    }

    public static string ConstructSupportImageLinkTV(this IProvider prov, string name)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            ShokoServer.PathAddressREST + "/Support/" + name);
    }

    public static string ConstructThumbLink(this IProvider prov, int type, int id)
    {
        var relation = prov.GetRelation().ToString(CultureInfo.InvariantCulture);
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            ShokoServer.PathAddressREST + "/Thumb/" + type + "/" + id + "/" + relation);
    }

    public static string ConstructTVThumbLink(this IProvider prov, int type, int id)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort,
            ShokoServer.PathAddressREST + "/Thumb/" + type + "/" + id + "/1.3333");
    }

    public static string ConstructCharacterImage(this IProvider prov, int id)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort, ShokoServer.PathAddressREST + "/2/" + id);
    }

    public static string ConstructSeiyuuImage(this IProvider prov, int id)
    {
        return prov.ServerUrl(Utils.SettingsProvider.GetSettings().ServerPort, ShokoServer.PathAddressREST + "/3/" + id);
    }

    public static readonly Lazy<Dictionary<string, double>> _relations = new(CreateRelationsMap, true);

    private static double GetRelation(this IProvider prov)
    {
        var relations = _relations.Value;

        var product = prov.RequestHeader("X-Plex-Product");
        if (product != null)
        {
            var kh = product.ToUpper();
            foreach (var n in relations.Keys.Where(a => a != "DEFAULT"))
            {
                if (n != null && kh.Contains(n))
                {
                    return relations[n];
                }
            }
        }

        return relations["DEFAULT"];
    }

    private static Dictionary<string, double> CreateRelationsMap()
    {
        var relations = new Dictionary<string, double>();
        var aspects = Utils.SettingsProvider.GetSettings().Plex.ThumbnailAspects.Split(',');

        for (var x = 0; x < aspects.Length; x += 2)
        {
            var key = aspects[x].Trim().ToUpper();

            double.TryParse(aspects[x + 1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var val);
            relations.Add(key, val);
        }

        if (!relations.ContainsKey("DEFAULT"))
        {
            relations.Add("DEFAULT", 0.666667D);
        }

        return relations;
    }

    public static string Base64EncodeUrl(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes).Replace("+", "-").Replace("/", "_").Replace("=", ",");
    }


    public static void AddLinksToAnimeEpisodeVideo(IProvider prov, Video v, int userid)
    {
        if (v.AnimeType == AnimeTypes.AnimeEpisode.ToString())
        {
            v.Key = prov.ContructVideoUrl(userid, v.Id, JMMType.Episode);
        }
        else if (v.Medias != null && v.Medias.Count > 0)
        {
            v.Key = prov.ContructVideoUrl(userid, v.Medias[0].Id, JMMType.File);
        }

        if (v.Medias == null)
        {
            return;
        }

        foreach (var m in v.Medias)
        {
            if (m?.Parts == null)
            {
                continue;
            }

            foreach (var p in m.Parts)
            {
                var ff = "file." + p.Container;
                p.Key = prov.ConstructVideoLocalStream(userid, m.Id, ff, prov.AutoWatch);
                if (p.Streams == null)
                {
                    continue;
                }

                foreach (var s in p.Streams.Where(a => a.File != null && a.StreamType == 3))
                {
                    s.Key = prov.ConstructFileStream(userid, s.File, prov.AutoWatch);
                }
            }
        }
    }

    private static readonly Regex UrlSafe = new("[ \\$^`:<>\\[\\]\\{\\}\"“\\+%@/;=\\?\\\\\\^\\|~‘,]",
        RegexOptions.Compiled);

    private static readonly Regex UrlSafe2 = new("[^0-9a-zA-Z_\\.\\s]", RegexOptions.Compiled);

    public static Video GenerateVideoFromAnimeEpisode(SVR_AnimeEpisode ep, int userID)
    {
        var l = new Video();
        var vids = ep.GetVideoLocals();
        l.Type = "episode";
        l.Summary = "Episode Overview Not Available"; //TODO Intenationalization
        l.Id = ep.AnimeEpisodeID;
        l.AnimeType = AnimeTypes.AnimeEpisode.ToString();
        if (vids.Count > 0)
        {
            //List<string> hashes = vids.Select(a => a.Hash).Distinct().ToList();
            l.Title = Path.GetFileNameWithoutExtension(vids[0].FileName);
            l.AddedAt = vids[0].DateTimeCreated.ToUnixTime();
            l.UpdatedAt = vids[0].DateTimeUpdated.ToUnixTime();
            l.OriginallyAvailableAt = vids[0].DateTimeCreated.ToPlexDate();
            l.Year = vids[0].DateTimeCreated.Year;
            l.Medias = new List<Media>();
            foreach (var v in vids)
            {
                if (v?.Media == null)
                {
                    continue;
                }

                var legacy = new Media(v.VideoLocalID, v.Media);
                var place = v.GetBestVideoLocalPlace();
                legacy.Parts.ForEach(p =>
                {
                    if (string.IsNullOrEmpty(p.LocalKey))
                    {
                        p.LocalKey = place.FullServerPath;
                    }

                    var name = UrlSafe.Replace(Path.GetFileName(place.FilePath), " ").CompactWhitespaces()
                        .Trim();
                    name = UrlSafe2.Replace(name, string.Empty)
                        .Trim()
                        .CompactCharacters('.')
                        .Replace(" ", "_")
                        .CompactCharacters('_')
                        .Replace("_.", ".");
                    while (name.StartsWith("_"))
                    {
                        name = name.Substring(1);
                    }

                    while (name.StartsWith("."))
                    {
                        name = name.Substring(1);
                    }

                    p.Key = ((IProvider)null).ReplaceSchemeHost(
                        ((IProvider)null).ConstructVideoLocalStream(userID, v.VideoLocalID, name, false));
                    if (p.Streams == null)
                    {
                        return;
                    }

                    foreach (var s in p.Streams.Where(a => a.File != null && a.StreamType == 3).ToList())
                    {
                        s.Key =
                            ((IProvider)null).ReplaceSchemeHost(
                                ((IProvider)null).ConstructFileStream(userID, s.File, false));
                    }
                });
                l.Medias.Add(legacy);
            }

            var title = ep.Title;
            if (!string.IsNullOrEmpty(title))
            {
                l.Title = title;
            }

            var romaji = RepoFactory.AniDB_Episode_Title.GetByEpisodeIDAndLanguage(ep.AniDB_EpisodeID,
                    Shoko.Plugin.Abstractions.DataModels.TitleLanguage.Romaji)
                .FirstOrDefault()?.Title;
            if (!string.IsNullOrEmpty(romaji))
            {
                l.OriginalTitle = romaji;
            }

            var aep = ep?.AniDB_Episode;
            if (aep != null)
            {
                l.EpisodeNumber = aep.EpisodeNumber;
                l.Index = aep.EpisodeNumber;
                l.EpisodeType = aep.EpisodeType;
                l.Rating = (int)float.Parse(aep.Rating, CultureInfo.InvariantCulture);
                var vote =
                    RepoFactory.AniDB_Vote.GetByEntityAndType(ep.AnimeEpisodeID, AniDBVoteType.Episode);
                if (vote != null)
                {
                    l.UserRating = (int)(vote.VoteValue / 100D);
                }

                if (aep.GetAirDateAsDate().HasValue)
                {
                    l.Year = aep.GetAirDateAsDate()?.Year ?? 0;
                    l.OriginallyAvailableAt = aep.GetAirDateAsDate()?.ToPlexDate();
                }

                #region TvDB

                var tvep = ep.TvDBEpisode;
                if (tvep != null)
                {
                    l.Thumb = tvep.GenPoster(null);
                    l.Summary = tvep.Overview;
                    l.Season = $"{tvep.SeasonNumber}x{tvep.EpisodeNumber:0#}";
                }

                #endregion
            }

            if (l.Thumb == null || l.Summary == null)
            {
                l.Thumb = ((IProvider)null).ConstructSupportImageLink("plex_404.png");
                l.Summary = "Episode Overview not Available";
            }
        }

        l.Id = ep.AnimeEpisodeID;
        return l;
    }

    public static void AddInformationFromMasterSeries(Video v, CL_AnimeSeries_User cserie, Video nv,
        bool omitExtraData = false)
    {
        var ret = false;
        v.ParentThumb = v.GrandparentThumb = nv.Thumb;
        if (cserie.AniDBAnime.AniDBAnime.Restricted > 0)
        {
            v.ContentRating = "R";
        }

        switch (cserie.AniDBAnime.AniDBAnime.AnimeType)
        {
            case (int)AnimeType.Movie:
                v.Type = "movie";
                if (v.Title.StartsWith("Complete Movie"))
                {
                    v.Title = nv.Title;
                    v.Summary = nv.Summary;
                    v.Index = 0;
                    ret = true;
                }
                else if (v.Title.StartsWith("Part "))
                {
                    v.Title = nv.Title + " - " + v.Title;
                    v.Summary = nv.Summary;
                }

                v.Thumb = nv.Thumb;
                break;
            case (int)AnimeType.OVA:
                if (v.Title == "OVA")
                {
                    v.Title = nv.Title;
                    v.Type = "movie";
                    v.Thumb = nv.Thumb;
                    v.Summary = nv.Summary;
                    v.Index = 0;
                    ret = true;
                }

                break;
        }

        if (string.IsNullOrEmpty(v.Art))
        {
            v.Art = nv.Art;
        }

        if (!omitExtraData)
        {
            if (v.Tags == null)
            {
                v.Tags = nv.Tags;
            }

            if (v.Genres == null)
            {
                v.Genres = nv.Genres;
            }

            if (v.Roles == null)
            {
                v.Roles = nv.Roles;
            }
        }

        if (v.Rating == 0)
        {
            v.Rating = nv.Rating;
        }

        if (v.Thumb == null)
        {
            v.Thumb = v.ParentThumb;
        }

        v.IsMovie = ret;
    }

    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source, int seed = -1)
    {
        var rnd = seed == -1 ? new Random() : new Random(seed);
        return source.OrderBy(item => rnd.Next());
    }

    public static Video GenerateFromAnimeGroup(SVR_AnimeGroup grp, int userid, List<SVR_AnimeSeries> allSeries)
    {
        var cgrp = grp.GetUserContract(userid);
        var subgrpcnt = grp.GetAllChildGroups().Count;

        if (cgrp.Stat_SeriesCount == 1 && subgrpcnt == 0)
        {
            var ser = ShokoServiceImplementation.GetSeriesForGroup(grp.AnimeGroupID, allSeries);
            var cserie = ser?.GetUserContract(userid);
            if (cserie == null)
            {
                return null;
            }

            var v = GenerateFromSeries(cserie, ser, ser.GetAnime(), userid);
            v.AirDate = ser.AirDate ?? DateTime.MinValue;
            v.UpdatedAt = ser.LatestEpisodeAirDate.HasValue
                ? ser.LatestEpisodeAirDate.Value.ToUnixTime()
                : null;
            v.Group = cgrp;
            return v;
        }
        else
        {
            var ser = grp.DefaultAnimeSeriesID.HasValue
                ? allSeries.FirstOrDefault(a => a.AnimeSeriesID == grp.DefaultAnimeSeriesID.Value)
                : allSeries.Find(a => a.AirDate.HasValue);
            if (ser == null && allSeries.Count > 0)
            {
                ser = allSeries[0];
            }

            var cserie = ser?.GetUserContract(userid);
            var v = FromGroup(cgrp, cserie, userid, subgrpcnt);
            v.Group = cgrp;
            v.AirDate = cgrp.Stat_AirDate_Min ?? DateTime.MinValue;
            v.UpdatedAt = cgrp.LatestEpisodeAirDate?.ToUnixTime();
            v.Rating = (int)Math.Round(grp.AniDBRating / 100, 1);
            var newTags = new List<Tag>();
            foreach (var tag in grp.Tags)
            {
                var newTag = new Tag();
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                newTag.Value = textInfo.ToTitleCase(tag.TagName.Trim());
                if (!newTags.Contains(newTag))
                {
                    newTags.Add(newTag);
                }
            }

            v.Genres = newTags;
            if (ser == null)
            {
                return v;
            }

            var newTitles = ser.GetAnime()
                .GetTitles()
                .Select(title => new AnimeTitle
                {
                    Title = title.Title, Language = title.LanguageCode, Type = title.TitleType.ToString().ToLower()
                })
                .ToList();
            v.Titles = newTitles;

            v.Roles = new List<RoleTag>();

            //TODO Character implementation is limited in JMM, One Character, could have more than one Seiyuu
            if (ser.GetAnime()?.Contract?.AniDBAnime?.Characters != null)
            {
                foreach (var c in ser.GetAnime().Contract.AniDBAnime.Characters)
                {
                    var ch = c?.CharName;
                    var seiyuu = c?.Seiyuu;
                    if (string.IsNullOrEmpty(ch))
                    {
                        continue;
                    }

                    var t = new RoleTag { Value = seiyuu?.SeiyuuName };
                    if (seiyuu != null)
                    {
                        t.TagPicture = ConstructSeiyuuImage(null, seiyuu.AniDB_SeiyuuID);
                    }

                    t.Role = ch;
                    t.RoleDescription = c?.CharDescription;
                    t.RolePicture = ConstructCharacterImage(null, c.CharID);
                    v.Roles.Add(t);
                }
            }

            if (cserie?.AniDBAnime?.AniDBAnime?.Fanarts != null)
            {
                v.Fanarts = new List<Contract_ImageDetails>();
                cserie?.AniDBAnime?.AniDBAnime?.Fanarts.ForEach(
                    a =>
                        v.Fanarts.Add(new Contract_ImageDetails
                        {
                            ImageID = a.AniDB_Anime_DefaultImageID, ImageType = a.ImageType
                        }));
            }

            if (cserie?.AniDBAnime?.AniDBAnime?.Banners == null)
            {
                return v;
            }

            v.Banners = new List<Contract_ImageDetails>();
            cserie?.AniDBAnime?.AniDBAnime?.Banners.ForEach(
                a =>
                    v.Banners.Add(new Contract_ImageDetails
                    {
                        ImageID = a.AniDB_Anime_DefaultImageID, ImageType = a.ImageType
                    }));
            return v;
        }
    }

    public static Video MayReplaceVideo(Video v1, SVR_AnimeSeries ser, CL_AnimeSeries_User cserie, int userid,
        bool all = true, Video serie = null)
    {
        var epcount = all
            ? ser.GetAnimeEpisodesCountWithVideoLocal()
            : ser.GetAnimeEpisodesNormalCountWithVideoLocal();
        if (epcount != 1 || (cserie.AniDBAnime.AniDBAnime.AnimeType != (int)AnimeType.OVA &&
                             cserie.AniDBAnime.AniDBAnime.AnimeType != (int)AnimeType.Movie))
        {
            return v1;
        }

        try
        {
            var episodes = ser.GetAnimeEpisodes();
            var v2 = GenerateVideoFromAnimeEpisode(episodes[0], userid);
            if (v2.IsMovie)
            {
                AddInformationFromMasterSeries(v2, cserie, serie ?? v1);
                v2.Thumb = (serie ?? v1).Thumb;
                return v2;
            }
        }
        catch
        {
            //Fast fix if file do not exist, and still is in db. (Xml Serialization of video info will fail on null)
        }

        return v1;
    }


    private static Video FromGroup(CL_AnimeGroup_User grp, CL_AnimeSeries_User ser, int userid, int subgrpcnt)
    {
        var p = new Directory
        {
            Id = grp.AnimeGroupID,
            AnimeType = AnimeTypes.AnimeGroup.ToString(),
            Title = grp.GroupName,
            Summary = grp.Description,
            Type = "show",
            AirDate = grp.Stat_AirDate_Min ?? DateTime.MinValue
        };
        if (grp.Stat_AllYears.Count > 0)
        {
            p.Year = grp.Stat_AllYears?.Min() ?? 0;
        }

        if (ser != null)
        {
            p.Thumb = ser.AniDBAnime?.AniDBAnime.DefaultImagePoster.GenPoster(null);
            p.Art = ser.AniDBAnime?.AniDBAnime.DefaultImageFanart.GenArt(null);
        }

        p.LeafCount = grp.UnwatchedEpisodeCount + grp.WatchedEpisodeCount;
        p.ViewedLeafCount = grp.WatchedEpisodeCount;
        p.ChildCount = grp.Stat_SeriesCount + subgrpcnt;
        if (grp.UnwatchedEpisodeCount == 0 && grp.WatchedDate.HasValue)
        {
            p.LastViewedAt = grp.WatchedDate.Value.ToUnixTime();
        }

        return p;
    }

    public static Video GenerateFromSeries(CL_AnimeSeries_User cserie, SVR_AnimeSeries ser, SVR_AniDB_Anime anidb,
        int userid)
    {
        Video v = new Directory();
        var episodes = ser.GetAnimeEpisodes()
            .ToDictionary(a => a, a => a.GetUserContract(userid));
        episodes = episodes.Where(a => a.Value == null || a.Value.LocalFileCount > 0)
            .ToDictionary(a => a.Key, a => a.Value);
        FillSerie(v, ser, episodes, anidb, cserie);
        if (ser.GetAnimeNumberOfEpisodeTypes() > 1)
        {
            v.Type = "show";
        }
        else if (cserie.AniDBAnime.AniDBAnime.AnimeType == (int)AnimeType.Movie ||
                 cserie.AniDBAnime.AniDBAnime.AnimeType == (int)AnimeType.OVA)
        {
            v = MayReplaceVideo(v, ser, cserie, userid);
        }

        return v;
    }

    private static string SummaryFromAnimeContract(CL_AnimeSeries_User c)
    {
        var s = c.AniDBAnime.AniDBAnime.Description;
        if (string.IsNullOrEmpty(s) && c.MovieDB_Movie != null)
        {
            s = c.MovieDB_Movie.Overview;
        }

        if (string.IsNullOrEmpty(s) && c.TvDB_Series != null && c.TvDB_Series.Count > 0)
        {
            s = c.TvDB_Series[0].Overview;
        }

        return s;
    }


    private static void FillSerie(Video p, SVR_AnimeSeries aser,
        Dictionary<SVR_AnimeEpisode, CL_AnimeEpisode_User> eps,
        SVR_AniDB_Anime anidb, CL_AnimeSeries_User ser)
    {
        var anime = ser.AniDBAnime.AniDBAnime;
        p.Id = ser.AnimeSeriesID;
        p.AnimeType = AnimeTypes.AnimeSerie.ToString();
        if (ser.AniDBAnime.AniDBAnime.Restricted > 0)
        {
            p.ContentRating = "R";
        }

        p.Title = aser.GetSeriesName();
        p.Summary = SummaryFromAnimeContract(ser);
        p.Type = "show";
        p.AirDate = DateTime.MinValue;
        var textInfo = new CultureInfo("en-US", false).TextInfo;
        if (anime.GetAllTags().Count > 0)
        {
            p.Genres = new List<Tag>();
            anime.GetAllTags()
                .ToList()
                .ForEach(a => p.Genres.Add(new Tag { Value = textInfo.ToTitleCase(a.Trim()) }));
        }

        //p.OriginalTitle
        if (anime.AirDate.HasValue)
        {
            p.AirDate = anime.AirDate.Value;
            p.OriginallyAvailableAt = anime.AirDate.Value.ToPlexDate();
            p.Year = anime.AirDate.Value.Year;
        }

        p.LeafCount = anime.EpisodeCount;
        //p.ChildCount = p.LeafCount;
        p.ViewedLeafCount = ser.WatchedEpisodeCount;
        p.Rating = (int)Math.Round(anime.Rating / 100D, 1);
        var vote = RepoFactory.AniDB_Vote.GetByEntityAndType(anidb.AnimeID, AniDBVoteType.Anime) ??
                   RepoFactory.AniDB_Vote.GetByEntityAndType(anidb.AnimeID, AniDBVoteType.AnimeTemp);
        if (vote != null)
        {
            p.UserRating = (int)(vote.VoteValue / 100D);
        }

        var ls = ser.CrossRefAniDBTvDBV2;
        if (ls != null && ls.Count > 0)
        {
            foreach (var c in ls)
            {
                if (c.TvDBSeasonNumber == 0)
                {
                    continue;
                }

                p.Season = c.TvDBSeasonNumber.ToString();
                p.Index = c.TvDBSeasonNumber;
            }
        }

        p.Thumb = p.ParentThumb = anime.DefaultImagePoster.GenPoster(null);
        p.Art = anime?.DefaultImageFanart?.GenArt(null);
        if (anime?.Fanarts != null)
        {
            p.Fanarts = new List<Contract_ImageDetails>();
            anime.Fanarts.ForEach(
                a =>
                    p.Fanarts.Add(new Contract_ImageDetails
                    {
                        ImageID = a.AniDB_Anime_DefaultImageID, ImageType = a.ImageType
                    }));
        }

        if (anime?.Banners != null)
        {
            p.Banners = new List<Contract_ImageDetails>();
            anime.Banners.ForEach(
                a =>
                    p.Banners.Add(new Contract_ImageDetails
                    {
                        ImageID = a.AniDB_Anime_DefaultImageID, ImageType = a.ImageType
                    }));
        }

        if (eps != null)
        {
            var types = eps.Keys.Where(a => a.AniDB_Episode != null)
                .Select(a => a.EpisodeTypeEnum).Distinct().ToList();
            p.ChildCount = types.Count > 1 ? types.Count : eps.Keys.Count;
        }

        p.Roles = new List<RoleTag>();

        //TODO Character implementation is limited in JMM, One Character, could have more than one Seiyuu
        if (anime.Characters != null)
        {
            foreach (var c in anime.Characters)
            {
                var ch = c?.CharName;
                var seiyuu = c?.Seiyuu;
                if (string.IsNullOrEmpty(ch))
                {
                    continue;
                }

                var t = new RoleTag { Value = seiyuu?.SeiyuuName };
                if (seiyuu != null)
                {
                    t.TagPicture = ConstructSeiyuuImage(null, seiyuu.AniDB_SeiyuuID);
                }

                t.Role = ch;
                t.RoleDescription = c?.CharDescription;
                t.RolePicture = ConstructCharacterImage(null, c.CharID);
                p.Roles.Add(t);
            }
        }

        p.Titles = new List<AnimeTitle>();
        foreach (var title in anidb.GetTitles())
        {
            p.Titles.Add(
                new AnimeTitle
                {
                    Language = title.LanguageCode,
                    Title = title.Title,
                    Type = title.TitleType.ToString().ToLower()
                });
        }
    }
}

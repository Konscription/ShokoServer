using System;
using System.Collections.Generic;

#nullable enable
namespace Shoko.Plugin.Abstractions.DataModels;

public interface ISeries : IWithTitles, IMetadata<int>
{
    /// <summary>
    /// The Anime Type.
    /// </summary>
    AnimeType Type { get; }

    /// <summary>
    /// The first aired date, if known.
    /// </summary>
    /// <value></value>
    DateTime? AirDate { get; }

    /// <summary>
    /// The end date of the series. Null means that it's still airing.
    /// </summary>
    DateTime? EndDate { get; }

    /// <summary>
    /// Overall user rating for the show, normalised on a scale of 1-10.
    /// </summary>
    double Rating { get; }

    /// <summary>
    /// Indicates it's restricted for non-adult viewers. 😉
    /// </summary>
    bool Restricted { get; }

    /// <summary>
    /// Related series.
    /// </summary>
    IReadOnlyList<IRelatedMetadata<ISeries>> RelatedSeries { get; }

    /// <summary>
    /// All cross-references linked to the series.
    /// </summary>
    IReadOnlyList<IVideoCrossReference> CrossReferences { get; }

    /// <summary>
    /// All known episodes for the show.
    /// </summary>
    IReadOnlyList<IEpisode> EpisodeList { get; }

    /// <summary>
    /// Episode counts for every episode type.
    /// </summary>
    IReadOnlyDictionary<EpisodeType, int> EpisodeCountDict { get; }

    /// <summary>
    /// Get all videos linked to the series, if any.
    /// </summary>
    IReadOnlyList<IVideo> VideoList { get; }
}

using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace Tubifarry.Core
{
    /// <summary>
    /// Contains combined information about an album, search parameters, and search results.
    /// </summary>
    public class AlbumData
    {
        // Mixed
        public string AlbumId { get; set; } = string.Empty;

        // Properties from AlbumInfo
        public string AlbumName { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string SpotifyUrl { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public DateTime ReleaseDateTime { get; set; }
        public string ReleaseDatePrecision { get; set; } = string.Empty;
        public int TotalTracks { get; set; }
        public bool ExplicitContent { get; set; }
        public string CoverUrl { get; set; } = string.Empty;
        public string CoverResolution { get; set; } = string.Empty;


        // Properties from YoutubeSearchResults
        public int Bitrate { get; set; }
        public long Duration { get; set; }

        /// <summary>
        /// Converts AlbumData into a ReleaseInfo object.
        /// </summary>
        public ReleaseInfo ToReleaseInfo()
        {
            return new ReleaseInfo
            {
                Guid = $"Tubifarry-{AlbumId}-{Bitrate}",
                Artist = ArtistName,
                Album = AlbumName,
                DownloadUrl = AlbumId,
                InfoUrl = SpotifyUrl,
                PublishDate = ReleaseDateTime,
                DownloadProtocol = nameof(YoutubeDownloadProtocol),
                Title = ConstructTitle(),
                Codec = "MP3",
                Resolution = CoverResolution,
                Source = CoverUrl,
                Container = Bitrate.ToString(),
                Size = (Duration > 0 ? Duration : TotalTracks * 300) * Bitrate * 1000 / 8
            };
        }

        public void ParseReleaseDate() => ReleaseDateTime = ReleaseDatePrecision switch
        {
            "year" => new DateTime(int.Parse(ReleaseDate), 1, 1),
            "month" => DateTime.ParseExact(ReleaseDate, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            "day" => DateTime.ParseExact(ReleaseDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new FormatException($"Unsupported release_date_precision: {ReleaseDatePrecision}"),
        };

        /// <summary>
        /// Constructs a title string for the album.
        /// </summary>
        private string ConstructTitle()
        {
            string title = $"{ArtistName} - {AlbumName}";

            if (ReleaseDateTime.Year > 0)
                title += $" ({ReleaseDateTime.Year})";
            if (ExplicitContent)
                title += " [Explicit]";
            title += $" [MP3 {Bitrate}] [WEB]";

            return title;
        }
    }
}
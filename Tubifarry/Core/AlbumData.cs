using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using System.Text.RegularExpressions;

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
        public ReleaseInfo ToReleaseInfo() => new()
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

        public void ParseReleaseDate() => ReleaseDateTime = ReleaseDatePrecision switch
        {
            "year" => new DateTime(int.Parse(ReleaseDate), 1, 1),
            "month" => DateTime.ParseExact(ReleaseDate, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
            "day" => DateTime.ParseExact(ReleaseDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new FormatException($"Unsupported release_date_precision: {ReleaseDatePrecision}"),
        };

        /// <summary>
        /// Constructs a title string for the album in a format optimized for parsing.
        /// </summary>
        /// <returns>A formatted title string.</returns>
        private string ConstructTitle()
        {
            string normalizedAlbumName = NormalizeAlbumName(AlbumName);
            // Start with the basic format: Artist - Album
            string title = $"{ArtistName} - {normalizedAlbumName}";

            // Add the release year if available
            if (ReleaseDateTime.Year > 0)
                title += $" - {ReleaseDateTime.Year}";

            // Add the explicit content indicator if applicable
            if (ExplicitContent)
                title += " [Explicit]";

            // Add the bitrate and source type
            title += $" [MP3_{Bitrate}kbps] [WEB]";

            return title;
        }

        /// <summary>
        /// Normalizes the album name to handle featuring artists and other parentheses.
        /// </summary>
        /// <param name="albumName">The album name to normalize.</param>
        /// <returns>The normalized album name.</returns>
        private string NormalizeAlbumName(string albumName)
        {
            // Handle featuring artists (e.g., "feat.", "ft.", "Feat.", "Ft.", etc.)
            Regex featRegex = new(@"(?i)\b(feat\.|ft\.|featuring)\b", RegexOptions.IgnoreCase);
            if (featRegex.IsMatch(albumName))
            {
                // Extract the featuring artist(s)
                Match match = featRegex.Match(albumName);
                string featuringArtist = albumName.Substring(match.Index + match.Length).Trim();

                // Format the featuring artist(s) in a consistent way
                albumName = $"{albumName.Substring(0, match.Index).Trim()} (feat. {featuringArtist})";
            }

            // Replace content inside parentheses (except for featuring artists) with curly braces
            albumName = Regex.Replace(albumName, @"\((?!feat\.)[^)]*\)", match => $"{{{match.Value.Trim('(', ')')}}}");

            return albumName;
        }
    }
}
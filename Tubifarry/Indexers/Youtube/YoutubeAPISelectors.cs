using Newtonsoft.Json.Linq;
using YouTubeMusicAPI.Models;
using YouTubeMusicAPI.Types;

namespace Tubifarry.Indexers.Youtube
{
    internal static class YoutubeAPISelectors
    {
        public static T SelectObject<T>(this JToken value, string path)
        {
            object? result = value.SelectToken(path)?.ToObject(typeof(T));
            return result == null ? throw new ArgumentNullException(path, "Required token is null.") : (T)result;
        }

        public static T? SelectObjectOptional<T>(this JToken value, string path) =>
            (T?)value.SelectToken(path)?.ToObject(typeof(T));

        public static Radio SelectRadio(this JToken value, string playlistIdPath = "menu.menuRenderer.items[0].menuNavigationItemRenderer.navigationEndpoint.watchEndpoint.playlistId", string? videoIdPath = null) =>
            new(value.SelectObject<string>(playlistIdPath), videoIdPath == null ? null : value.SelectObjectOptional<string>(videoIdPath));

        public static Thumbnail[] SelectThumbnails(this JToken value, string path = "thumbnail.musicThumbnailRenderer.thumbnail.thumbnails")
        {
            JToken? thumbnails = value.SelectToken(path);
            if (thumbnails == null) return Array.Empty<Thumbnail>();

            return thumbnails
                .Select(t => new
                {
                    Url = t.SelectToken("url")?.ToString(),
                    Width = t.SelectToken("width")?.ToString(),
                    Height = t.SelectToken("height")?.ToString()
                })
                .Where(t => t.Url != null)
                .Select(t => new Thumbnail(t.Url!, int.Parse(t.Width ?? "0"), int.Parse(t.Height ?? "0")))
                .ToArray();
        }

        public static YouTubeMusicItem[] SelectArtists(this JToken value, string path, int startIndex = 0, int trimBy = 0)
        {
            JToken[] runs = value.SelectObject<JToken[]>(path);
            return runs
                .Skip(startIndex)
                .Take(runs.Length - trimBy - startIndex)
                .Select(run => new
                {
                    Artist = run.SelectToken("text")?.ToString()?.Trim(),
                    ArtistId = run.SelectToken("navigationEndpoint.browseEndpoint.browseId")?.ToString()
                })
                .Where(a => a.Artist != null && a.Artist != "," && a.Artist != "&" && a.Artist != "•")
                .Select(a => new YouTubeMusicItem(a.Artist!, a.ArtistId, YouTubeMusicItemKind.Artists))
                .ToArray();
        }
    }
}
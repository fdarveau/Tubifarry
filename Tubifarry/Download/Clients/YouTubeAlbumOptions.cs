using NLog;
using NzbDrone.Core.Download;
using Requests.Options;
using YouTubeMusicAPI.Client;

namespace Tubifarry.Download.Clients
{
    public record YouTubeAlbumOptions : RequestOptions<string, string>
    {
        public YouTubeMusicClient? YouTubeMusicClient { get; set; }

        public Logger? Logger { get; set; }

        public DownloadClientItemClientInfo? ClientInfo { get; set; }

        public bool TryIncludeLrc { get; set; }

        public bool TryIncludeSycLrc { get; set; }

        public string DownloadPath { get; set; } = string.Empty;

        public string LRCLIBInstance { get; set; } = "https://lrclib.net";

        public int Chunks { get; set; } = 2;

        public bool ReEncodeToMP3 { get; set; }

        public YouTubeAlbumOptions() { }

        protected YouTubeAlbumOptions(YouTubeAlbumOptions options) : base(options)
        {
            YouTubeMusicClient = options.YouTubeMusicClient;
            Logger = options.Logger;
            ClientInfo = options.ClientInfo;
            TryIncludeSycLrc = options.TryIncludeSycLrc;
            TryIncludeLrc = options.TryIncludeLrc;
            Chunks = options.Chunks;
            ReEncodeToMP3 = options.ReEncodeToMP3;
            LRCLIBInstance = options.LRCLIBInstance;
            DownloadPath = options.DownloadPath;
        }
    }
}

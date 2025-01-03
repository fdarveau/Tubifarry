using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.YouTube;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using Requests;
using Tubifarry.Core;
using Tubifarry.Download.Clients;
using YouTubeMusicAPI.Client;

namespace NzbDrone.Download.Clients.YouTube
{

    /// <summary>
    /// Represents an interface for a YouTube proxy.
    /// This interface defines the contract for any class that acts as a proxy for handling YouTube requests.
    /// </summary>
    public interface IYoutubeDownloadManager
    {
        public Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer, YoutubeClient provider);
        public IEnumerable<DownloadClientItem> GetItems();
        public void RemoveItem(DownloadClientItem item);
        public void SetCookies(string path);
    }

    public class YoutubeDownloadManager : IYoutubeDownloadManager
    {
        private readonly RequestContainer<YouTubeAlbumRequest> _queue;
        private YouTubeMusicClient _ytClient;
        private Task? _testTask;
        private string? _cookiePath;
        private Logger _logger;


        /// <summary>
        /// Private constructor to prevent external instantiation.
        /// Initializes a new instance of the <see cref="YoutubeDownloadManager"/> class.
        /// </summary>
        public YoutubeDownloadManager(Logger logger)
        {
            _logger = logger;
            _queue = new();
            _ytClient = new YouTubeMusicClient();
            _logger.Debug("Initialized");
        }

        public void SetCookies(string path)
        {
            if (string.IsNullOrEmpty(path) || path == _cookiePath)
                return;
            _cookiePath = path;
            _ytClient = new(cookies: CookieManager.ParseCookieFile(path));
        }

        public Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer, YoutubeClient provider)
        {
            _testTask ??= provider.TestFFmpeg();
            _testTask.Wait();
            YouTubeAlbumRequest request = new(remoteAlbum, new()
            {
                YouTubeMusicClient = _ytClient,
                Handler = RequestHandler.MainRequestHandlers[1],
                TryIncludeSycLrc = provider.Settings.SaveSyncedLyrics,
                TryIncludeLrc = provider.Settings.UseLRCLIB,
                DownloadPath = provider.Settings.DownloadPath,
                Chunks = provider.Settings.Chunks,
                DelayBetweenAttemps = TimeSpan.FromSeconds(5),
                NumberOfAttempts = 2,
                UseID3v2_3 = provider.Settings.UseID3v2_3,
                ReEncodeToMP3 = provider.Settings.ReEncode > 0,
                ClientInfo = DownloadClientItemClientInfo.FromDownloadClient(provider, false),
                Logger = _logger,
            });
            _queue.Add(request);
            _logger.Debug($"Download request added to queue. Request ID: {request.ID}");
            return Task.FromResult(request.ID);
        }

        public IEnumerable<DownloadClientItem> GetItems() => _queue.Select(x => x.ClientItem);

        public void RemoveItem(DownloadClientItem item)
        {
            YouTubeAlbumRequest? req = _queue.ToList().Find(x => x.ID == item.DownloadId);
            if (req == null)
                return;
            req.Dispose();
            _queue.Remove(req);
            _logger.Debug($"Item removed from queue. Download ID: {item.DownloadId}");
        }
    }
}

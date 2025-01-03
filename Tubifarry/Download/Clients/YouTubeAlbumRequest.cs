using DownloadAssistant.Base;
using DownloadAssistant.Options;
using DownloadAssistant.Requests;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using Requests;
using Requests.Options;
using System.Text;
using Tubifarry.Core;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models;
using YouTubeMusicAPI.Models.Info;
using YouTubeMusicAPI.Models.Streaming;

namespace Tubifarry.Download.Clients
{
    internal class YouTubeAlbumRequest : Request<YouTubeAlbumOptions, string, string>
    {
        private readonly OsPath _destinationPath;
        private readonly StringBuilder _message = new();
        private readonly RequestContainer<IRequest> _requestContainer = new();
        private readonly RequestContainer<LoadRequest> _trackContainer = new();
        private readonly ReleaseInfo _releaseInfo;
        private readonly DownloadClientItem _clientItem;

        private DateTime _lastUpdateTime = DateTime.MinValue;
        private long _lastRemainingSize = 0;
        private byte[]? _albumCover;

        private Logger? Logger => Options.Logger;

        public override Task Task => _requestContainer.Task;
        public override RequestState State => _requestContainer.State;
        public string ID { get; } = Guid.NewGuid().ToString();

        public DownloadClientItem ClientItem
        {
            get
            {
                _clientItem.RemainingSize = GetRemainingSize();
                _clientItem.Status = GetDownloadItemStatus();
                _clientItem.RemainingTime = GetRemainingTime();
                _clientItem.Message = GetDistinctMessages();
                _clientItem.CanBeRemoved = HasCompleted();
                _clientItem.CanMoveFiles = HasCompleted();
                return _clientItem;
            }
        }

        public YouTubeAlbumRequest(RemoteAlbum remoteAlbum, YouTubeAlbumOptions? options) : base(options)
        {
            Options.YouTubeMusicClient ??= new YouTubeMusicClient();
            _releaseInfo = remoteAlbum.Release;
            _requestContainer.Add(_trackContainer);
            _destinationPath = new(Path.Combine(Options.DownloadPath, _releaseInfo.Artist, _releaseInfo.Album));
            _clientItem = CreateClientItem();
            ProcessAlbum();
        }

        private void ProcessAlbum()
        {
            _requestContainer.Add(new OwnRequest(async (token) =>
            {
                Directory.CreateDirectory(_destinationPath.FullPath);
                string albumBrowseID = await Options.YouTubeMusicClient!.GetAlbumBrowseIdAsync(_releaseInfo.DownloadUrl, token);
                AlbumInfo albumInfo = await Options.YouTubeMusicClient.GetAlbumInfoAsync(albumBrowseID, token);
                if (albumInfo?.Songs == null || !albumInfo.Songs.Any())
                {
                    _message.AppendLine($"No tracks to download found in the album: {_releaseInfo.Album}");
                    Logger?.Debug($"No tracks to download found in the album: {_releaseInfo.Album}.");
                    return false;
                }

                _albumCover = await TryDownloadCoverAsync(albumInfo, token);

                foreach (AlbumSongInfo trackInfo in albumInfo.Songs)
                {
                    if (trackInfo.Id == null)
                    {
                        _message.AppendLine($"Skipping track '{trackInfo.Name}' in album '{_releaseInfo.Album}' because it has no valid download URL.");
                        Logger?.Debug($"Skipping track '{trackInfo.Name}' in album '{_releaseInfo.Album}' because it has no valid download URL.");
                        continue;
                    }

                    StreamingData streamingData = await Options.YouTubeMusicClient.GetStreamingDataAsync(trackInfo.Id, token);
                    AudioStreamInfo? highestAudioStreamInfo = streamingData.StreamInfo.OfType<AudioStreamInfo>().OrderByDescending(info => info.Bitrate).FirstOrDefault();
                    if (highestAudioStreamInfo == null)
                    {
                        _message.AppendLine($"Skipping track '{trackInfo.Name}' in album '{_releaseInfo.Album}' because no audio stream was found.");
                        Logger?.Debug($"Skipping track '{trackInfo.Name}' in album '{_releaseInfo.Album}' because no audio stream was found.");
                        continue;
                    }

                    AddTrackDownloadRequests(albumInfo, trackInfo, highestAudioStreamInfo);
                }
                return true;
            }, new()
            {
                CancellationToken = Token,
                DelayBetweenAttemps = Options.DelayBetweenAttemps,
                NumberOfAttempts = Options.NumberOfAttempts,
                Priority = RequestPriority.Low,
                Handler = Options.Handler
            }));
        }

        public async Task<byte[]?> TryDownloadCoverAsync(AlbumInfo albumInfo, CancellationToken token)
        {
            Thumbnail? bestThumbnail = albumInfo.Thumbnails.OrderByDescending(x => x.Height * x.Width).FirstOrDefault();
            int[] releaseResolution = _releaseInfo.Resolution.Split('x').Select(int.Parse).ToArray();
            int releaseArea = releaseResolution[0] * releaseResolution[1];
            int albumArea = (bestThumbnail?.Height ?? 0) * (bestThumbnail?.Width ?? 0);

            string coverUrl = albumArea > releaseArea ? bestThumbnail?.Url ?? _releaseInfo.Source : _releaseInfo.Source;

            using HttpResponseMessage response = await HttpGet.HttpClient.GetAsync(coverUrl, token);
            if (!response.IsSuccessStatusCode)
            {
                _message.AppendLine($"Failed to download cover art for album '{albumInfo.Name}'. Status code: {response.StatusCode}.");
                Logger?.Debug($"Failed to download cover art for album '{albumInfo.Name}'. Status code: {response.StatusCode}.");
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(token);
        }

        private void AddTrackDownloadRequests(AlbumInfo albumInfo, AlbumSongInfo trackInfo, AudioStreamInfo audioStreamInfo)
        {
            LoadRequest downloadingReq = new(audioStreamInfo.Url, new LoadRequestOptions()
            {
                CancellationToken = Token,
                CreateSpeedReporter = true,
                SpeedReporterTimeout = 1,
                Priority = RequestPriority.Normal,
                DelayBetweenAttemps = Options.DelayBetweenAttemps,
                Filename = $"{trackInfo.SongNumber} - {trackInfo.Name}.mp3",
                DestinationPath = _destinationPath.FullPath,
                Handler = Options.Handler,
                DeleteFilesOnFailure = true,
                Chunks = Options.Chunks,
                RequestFailed = (req, path) =>
                {
                    _message.AppendLine($"Downloading track '{trackInfo.Name}' in album '{albumInfo.Name}' failed.");
                    Logger?.Debug($"Downloading track '{trackInfo.Name}' in album '{albumInfo.Name}' failed.");
                },
                WriteMode = WriteMode.AppendOrTruncate,
            });

            OwnRequest postProcessReq = new((token) => SongDownloadCompletedAsync(albumInfo, trackInfo, downloadingReq, token), new()
            {
                AutoStart = false,
                Priority = RequestPriority.High,
                DelayBetweenAttemps = Options.DelayBetweenAttemps,
                Handler = Options.Handler,
                RequestFailed = (req, path) =>
                {
                    _message.AppendLine($"Post-processing for track '{trackInfo.Name}' in album '{albumInfo.Name}' failed.");
                    Logger?.Debug($"Post-processing for track '{trackInfo.Name}' in album '{albumInfo.Name}' failed.");
                    try
                    {
                        if (File.Exists(downloadingReq.Destination))
                            File.Delete(downloadingReq.Destination);
                    }
                    catch (Exception) { }
                },
                CancellationToken = Token
            });

            downloadingReq.TrySetSubsequentRequest(postProcessReq);

            _ = postProcessReq.TrySetIdle();

            _trackContainer.Add(downloadingReq);
            _requestContainer.Add(postProcessReq);
        }

        private async Task<bool> SongDownloadCompletedAsync(AlbumInfo albumInfo, AlbumSongInfo trackInfo, LoadRequest req, CancellationToken token)
        {
            string trackPath = req.Destination;
            if (!File.Exists(trackPath))
                return false;

            AudioMetadataHandler audioData = new(trackPath, Options.Logger) { AlbumCover = _albumCover, UseID3v2_3 = Options.UseID3v2_3 };

            if (Options.TryIncludeLrc)
                audioData.Lyric = await Lyric.FetchLyricsFromLRCLIBAsync(Options.LRCLIBInstance, _releaseInfo, trackInfo, token);

            if (Options.ReEncodeToMP3)
                await audioData.TryConvertToMP3();

            if (!audioData.TryEmbedMetadataInTrack(albumInfo, trackInfo, _releaseInfo))
                return false;

            if (Options.TryIncludeSycLrc)
                await audioData.TryCreateLrcFileAsync(token);

            GetRemainingTime();
            return true;
        }

        public DownloadClientItem CreateClientItem() => new()
        {
            DownloadId = ID,
            Title = _releaseInfo.Title,
            TotalSize = _releaseInfo.Size,
            DownloadClientInfo = Options.ClientInfo,
            OutputPath = _destinationPath,
        };

        private TimeSpan? GetRemainingTime()
        {
            long remainingSize = GetRemainingSize();
            if (_lastUpdateTime != DateTime.MinValue && _lastRemainingSize != 0)
            {
                TimeSpan timeElapsed = DateTime.UtcNow - _lastUpdateTime;
                long bytesDownloaded = _lastRemainingSize - remainingSize;

                if (timeElapsed.TotalSeconds > 0 && bytesDownloaded > 0)
                {
                    double bytesPerSecond = bytesDownloaded / timeElapsed.TotalSeconds;

                    double remainingSeconds = remainingSize / bytesPerSecond;

                    if (remainingSeconds < 0)
                        return TimeSpan.FromSeconds(10);
                    return TimeSpan.FromSeconds(remainingSeconds);
                }
            }

            _lastUpdateTime = DateTime.UtcNow;
            _lastRemainingSize = remainingSize;
            return null;
        }

        private string GetDistinctMessages()
        {
            HashSet<string> distinctMessages = new();
            StringBuilder.ChunkEnumerator chunkEnumerator = _message.GetChunks();

            while (chunkEnumerator.MoveNext())
            {
                ReadOnlyMemory<char> chunk = chunkEnumerator.Current;
                distinctMessages.Add(chunk.ToString());
            }

            return string.Join("", distinctMessages);
        }

        private long GetRemainingSize() => Math.Max(_trackContainer.Sum(x => x.ContentLength), _releaseInfo.Size) - _trackContainer.Sum(x => x.BytesDownloaded);

        public DownloadItemStatus GetDownloadItemStatus() => State switch
        {
            RequestState.Idle => DownloadItemStatus.Queued,
            RequestState.Paused => DownloadItemStatus.Paused,
            RequestState.Running => DownloadItemStatus.Downloading,
            RequestState.Compleated => DownloadItemStatus.Completed,
            RequestState.Failed => _requestContainer.Count(x => x.State == RequestState.Failed) >= _requestContainer.Count / 2
                                   ? DownloadItemStatus.Failed
                                   : _requestContainer.All(x => x.HasCompleted()) ? DownloadItemStatus.Completed : DownloadItemStatus.Failed,
            _ => DownloadItemStatus.Warning,
        };

        protected override Task<RequestReturn> RunRequestAsync() => throw new NotImplementedException();
        public override void Start() => throw new NotImplementedException();
        public override void Pause() => throw new NotImplementedException();
    }
}
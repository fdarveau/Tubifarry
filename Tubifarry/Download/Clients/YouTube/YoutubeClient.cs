using DownloadAssistant.Options;
using DownloadAssistant.Requests;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Download.Clients.YouTube;
using Tubifarry.Core;

namespace NzbDrone.Core.Download.Clients.YouTube
{
    public class YoutubeClient : DownloadClientBase<YoutubeProviderSettings>
    {
        private readonly IYoutubeDownloadManager _dlManager;

        public YoutubeClient(IYoutubeDownloadManager dlManager, IConfigService configService, IDiskProvider diskProvider, IRemotePathMappingService remotePathMappingService, Logger logger) : base(configService, diskProvider, remotePathMappingService, logger)
        {
            _dlManager = dlManager;
        }

        public override string Name => "Youtube";

        public override string Protocol => nameof(YoutubeDownloadProtocol);

        public new YoutubeProviderSettings Settings => base.Settings;

        public override Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer) => _dlManager.Download(remoteAlbum, indexer, this);

        public override IEnumerable<DownloadClientItem> GetItems() => _dlManager.GetItems();

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (deleteData)
                DeleteItemData(item);
            _dlManager.RemoveItem(item);
        }

        public override DownloadClientInfo GetStatus() => new()
        {
            IsLocalhost = false,
            OutputRootFolders = new() { new OsPath(Settings.DownloadPath) }
        };

        protected override void Test(List<ValidationFailure> failures)
        {
            _dlManager.SetCookies(Settings.CookiePath);
            StatusRequest req = new("https://music.youtube.com/", new WebRequestOptions<HttpResponseMessage>
            {
                RequestFailed = (req, response) =>
                {
                    failures.Add(new ValidationFailure("Connection", $"Failed to connect to YouTube Music. Status Code: {response?.StatusCode}"));
                }
            });

            failures.AddIfNotNull(TestFFmpeg().Result);
            req.Wait();
        }

        public async Task<ValidationFailure> TestFFmpeg()
        {
            if (Settings.ReEncode == (int)ReEncodeOptions.UseFFmpegOrInstall)
            {
                if (!AudioMetadataHandler.FFmpegIsInstalled)
                {
                    try
                    {
                        await AudioMetadataHandler.InstallFFmpeg(Settings.FFmpegPath);
                    }
                    catch (Exception ex)
                    {
                        return new ValidationFailure("FFmpegInstallation", $"Failed to install FFmpeg: {ex.Message}");
                    }
                }
            }

            if (Settings.ReEncode == (int)ReEncodeOptions.UseCustomFFmpeg && !AudioMetadataHandler.FFmpegIsInstalled)
                return new ValidationFailure("FFmpegPath", $"The specified FFmpeg path does not exist: {Settings.FFmpegPath}");
            return null!;
        }
    }
}

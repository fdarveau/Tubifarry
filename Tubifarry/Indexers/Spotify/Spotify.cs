using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;
using Requests;

namespace NzbDrone.Core.Indexers.Spotify
{
    internal class Spotify : HttpIndexerBase<SpotifyIndexerSettings>
    {
        public override string Name => "Tubifarry";
        public override string Protocol => nameof(YoutubeDownloadProtocol);
        public override bool SupportsRss => false;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override TimeSpan RateLimit => new(3);

        private ISpotifyRequestGenerator _indexerRequestGenerator;

        private ISpotifyToYoutubeParser _parseIndexerResponse;

        public Spotify(ISpotifyToYoutubeParser parser, ISpotifyRequestGenerator generator, IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, IParsingService parsingService, Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _parseIndexerResponse = parser;
            _indexerRequestGenerator = generator;
            if (generator.TokenIsExpired())
                generator.StartTokenRequest();

            RequestHandler.MainRequestHandlers[0].MaxParallelism = 2;
        }

        public override IIndexerRequestGenerator GetRequestGenerator() => _indexerRequestGenerator;

        public override IParseIndexerResponse GetParser() => _parseIndexerResponse;

    }
}
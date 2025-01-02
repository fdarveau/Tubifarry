using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles.Delay;

namespace NzbDrone.Core.Plugins
{
    public class Tubifarry : Plugin
    {
        public override string Name => "Tubifarry";
        public override string Owner => "TypNull";
        public override string GithubUrl => "https://github.com/TypNull/Tubifarry";

        private static Type ProtocolType => typeof(YoutubeDownloadProtocol);

        public Tubifarry(IDelayProfileRepository repo, IEnumerable<IDownloadProtocol> downloadProtocols, Logger logger) => CheckDelayProfiles(repo, downloadProtocols, logger);

        private void CheckDelayProfiles(IDelayProfileRepository repo, IEnumerable<IDownloadProtocol> downloadProtocols, Logger logger)
        {
            IDownloadProtocol? protocol = downloadProtocols.FirstOrDefault(x => x.GetType() == ProtocolType);
            if (protocol == null)
            {
                logger.Error($"{ProtocolType} not found in download protocols.");
                return;
            }

            logger.Debug($"Checking Protokol: {protocol.GetType().Name}");

            foreach (DelayProfile? profile in repo.All())
            {
                if (!profile.Items.Any(x => x.Protocol == protocol.GetType().Name))
                {
                    logger.Debug($"Added protocol to DelayProfile (ID: {profile.Id})");
                    profile.Items.Add(GetProtocolItem(protocol, true));
                    repo.Update(profile);
                }
            }
        }

        private static DelayProfileProtocolItem GetProtocolItem(IDownloadProtocol protocol, bool allowed) => new()
        {
            Name = protocol.GetType().Name.Replace("DownloadProtocol", ""),
            Protocol = protocol.GetType().Name,
            Allowed = allowed
        };
    }
}

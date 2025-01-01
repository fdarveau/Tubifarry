using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Spotify
{
    public class SpotifyIndexerSettingsValidator : AbstractValidator<SpotifyIndexerSettings> { }

    public class SpotifyIndexerSettings : IIndexerSettings
    {
        private static readonly SpotifyIndexerSettingsValidator Validator = new();

        [FieldDefinition(0, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; } = null;

        public string BaseUrl { get; set; } = "";

        public NzbDroneValidationResult Validate() => new(Validator.Validate(this));


    }
}
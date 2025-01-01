using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.YouTube
{
    public class YoutubeProviderSettingsValidator : AbstractValidator<YoutubeProviderSettings>
    {
        public YoutubeProviderSettingsValidator()
        {
            RuleFor(x => x.DownloadPath).IsValidPath();
            RuleFor(x => x.LRCLIBInstance).IsValidUrl();
            RuleFor(x => x.Chunks)
                .Must(chunks => chunks > 0 && chunks < 5)
                .WithMessage("Chunks must be greater than 0 and smaller than 5.");
            RuleFor(x => x.SaveSyncedLyrics)
                .Must((settings, saveSyncedLyrics) => !saveSyncedLyrics || settings.UseLRCLIB)
                .WithMessage("Save Synced Lyrics requires 'Use LRCLIB Lyric Provider' to be enabled.");
            RuleFor(x => x.FFmpegPath)
            .NotEmpty()
            .When(x => x.ReEncode != ReEncodeOptions.Disabled)
            .WithMessage("FFmpeg path is required when re-encoding is enabled.");
            RuleFor(x => x.FFmpegPath)
                .IsValidPath()
                .When(x => x.ReEncode != ReEncodeOptions.Disabled)
                .WithMessage("Invalid FFmpeg path.");
        }
    }

    public class YoutubeProviderSettings : IProviderConfig
    {
        private static readonly YoutubeProviderSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "Download Path", Type = FieldType.Path, HelpText = "Specify the directory where downloaded files will be saved.")]
        public string DownloadPath { get; set; } = "";

        [FieldDefinition(1, Label = "Use LRCLIB Lyric Provider", HelpText = "Enable this option to fetch lyrics from LRCLIB.", Type = FieldType.Checkbox)]
        public bool UseLRCLIB { get; set; } = true;

        [FieldDefinition(2, Label = "Save Synced Lyrics", HelpText = "Enable this option to save synced lyrics to a separate .lrc file, if available. Requires .lrc to be allowed under Import Extra Files.", Type = FieldType.Checkbox)]
        public bool SaveSyncedLyrics { get; set; } = false;

        [FieldDefinition(3, Label = "LRC Lib Instance", Type = FieldType.Url, HelpText = "The URL of a LRC Lib instance to connect to. Default is 'lrclib.net'.", Advanced = true)]
        public string LRCLIBInstance { get; set; } = "https://lrclib.net";

        [FieldDefinition(4, Label = "File Chunk Count", Type = FieldType.Number, HelpText = "Number of chunks to split the download into. Each chunk is its own download. Note: Non-chunked downloads from YouTube are typically much slower.", Advanced = true)]
        public int Chunks { get; set; } = 2;

        [FieldDefinition(5, Label = "ReEncode", Type = FieldType.Select, SelectOptions = typeof(ReEncodeOptions), HelpText = "Specify whether to re-encode audio files and how to handle FFmpeg.")]
        public ReEncodeOptions ReEncode { get; set; } = ReEncodeOptions.Disabled;

        [FieldDefinition(6, Label = "FFmpeg Path", Type = FieldType.Path, HelpText = "Specify the path to the FFmpeg binary. Required if 'Use Custom FFmpeg' is selected.", Advanced = true)]
        public string FFmpegPath { get; set; } = "";

        public NzbDroneValidationResult Validate() => new(Validator.Validate(this));
    }

    public enum ReEncodeOptions
    {
        [FieldOption(Label = "Disabled", Hint = "Do not re-encode files")]
        Disabled = 0,

        [FieldOption(Label = "Use or Install FFmpeg", Hint = "Automatically download and use FFmpeg if not installed")]
        UseFFmpegOrInstall = 1,

        [FieldOption(Label = "Use Custom FFmpeg", Hint = "Specify a custom FFmpeg executable path")]
        UseCustomFFmpeg = 2
    }
}
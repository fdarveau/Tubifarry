using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using Tubifarry.Core.Model;
using Tubifarry.Core.Utilities;
using Xabe.FFmpeg;

namespace Tubifarry.Metadata.Converter
{

    public class AudioConverterSettingsValidator : AbstractValidator<AudioConverterSettings>
    {
        public AudioConverterSettingsValidator()
        {
            // Validate FFmpegPath (if re-encoding is enabled)
            RuleFor(x => x.FFmpegPath)
                .NotEmpty()
                .WithMessage("FFmpeg path is required when re-encoding is enabled.")
                .MustAsync(async (ffmpegPath, cancellationToken) => await TestFFmpeg(ffmpegPath))
                .WithMessage("FFmpeg is not installed or invalid at the specified path.");

            // Validate custom conversion rules
            RuleFor(x => x.CustomConversion)
                .Must(customConversions => customConversions == null || customConversions.All(IsValidConversionRule))
                .WithMessage("Custom conversion rules must be in the format 'source->target' (e.g., 'mp3TOflac').");

            RuleFor(x => x.CustomConversion)
                .Must(customConversions => customConversions == null || customConversions.All(IsValidLossyConversion))
                .WithMessage("Lossy formats cannot be converted to non-lossy formats.");

            RuleFor(x => x)
                .Must(settings => IsValidStaticConversion(settings))
                .WithMessage("Lossy formats cannot be converted to non-lossy formats.");
        }

        private bool IsValidConversionRule(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return false;

            string[] parts = rule.Split(new[] { "_to_", "TO" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                return false;

            bool isValidSource = Enum.TryParse(parts[0], true, out AudioFormat sourceFormat) && sourceFormat != AudioFormat.Unknown;
            bool isValidTarget = Enum.TryParse(parts[1], true, out AudioFormat targetFormat) && targetFormat != AudioFormat.Unknown;

            return isValidSource && isValidTarget;
        }

        private bool IsValidLossyConversion(string rule)
        {
            if (string.IsNullOrWhiteSpace(rule))
                return true;
            string[] parts = rule.Split(new[] { "_to_", "TO" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            if (!Enum.TryParse(parts[0], true, out AudioFormat sourceFormat) || !Enum.TryParse(parts[1], true, out AudioFormat targetFormat))
                return false;

            if (AudioFormatHelper.IsLossyFormat(sourceFormat) && !AudioFormatHelper.IsLossyFormat(targetFormat))
                return false;
            return true;
        }

        private static bool IsValidStaticConversion(AudioConverterSettings settings)
        {
            AudioFormat targetFormat = (AudioFormat)settings.TargetFormat;

            if (!AudioFormatHelper.IsLossyFormat(targetFormat))
                if (settings.ConvertMP3 || settings.ConvertAAC || settings.ConvertOpus || settings.ConvertOther)
                    return false;

            return true;
        }

        private static async Task<bool> TestFFmpeg(string ffmpegPath)
        {
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                return false;

            string oldPath = FFmpeg.ExecutablesPath;
            FFmpeg.SetExecutablesPath(ffmpegPath);
            AudioMetadataHandler.ResetFFmpegInstallationCheck();

            if (!AudioMetadataHandler.CheckFFmpegInstalled())
            {
                try
                {
                    await AudioMetadataHandler.InstallFFmpeg(ffmpegPath);
                }
                catch
                {
                    FFmpeg.SetExecutablesPath(oldPath);
                    return false;
                }
            }

            FFmpeg.SetExecutablesPath(oldPath);
            return true;
        }
    }

    public class AudioConverterSettings : IProviderConfig
    {
        private static readonly AudioConverterSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "FFmpeg Path", Type = FieldType.Path, Section = MetadataSectionType.Metadata, Placeholder = "/downloads/FFmpeg", HelpText = "Specify the path to the FFmpeg binary.")]
        public string FFmpegPath { get; set; } = string.Empty;

        [FieldDefinition(1, Label = "Convert MP3", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert MP3 files.")]
        public bool ConvertMP3 { get; set; }

        [FieldDefinition(2, Label = "Convert AAC", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert AAC files.")]
        public bool ConvertAAC { get; set; }

        [FieldDefinition(3, Label = "Convert FLAC", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert FLAC files.")]
        public bool ConvertFLAC { get; set; }

        [FieldDefinition(4, Label = "Convert WAV", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert WAV files.")]
        public bool ConvertWAV { get; set; }

        [FieldDefinition(5, Label = "Convert Opus", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert Opus files.")]
        public bool ConvertOpus { get; set; }

        [FieldDefinition(7, Label = "Convert Other Formats", Type = FieldType.Checkbox, Section = MetadataSectionType.Metadata, HelpText = "Convert other formats (e.g., WMA).")]
        public bool ConvertOther { get; set; }

        [FieldDefinition(8, Label = "Target Format", Type = FieldType.Select, SelectOptions = typeof(TargetAudioFormat), Section = MetadataSectionType.Metadata, HelpText = "Select the target format to convert audio files into.")]
        public int TargetFormat { get; set; } = (int)TargetAudioFormat.Opus;

        private IEnumerable<string>? _customConversion;

        [FieldDefinition(9, Label = "Custom Conversion Rules", Type = FieldType.Tag, Section = MetadataSectionType.Metadata, Unit = "xTOy", HelpText = "Specify custom conversion rules in the format 'source_to_target'. These rules will override the default settings.")]
        public IEnumerable<string>? CustomConversion
        {
            get => _customConversion;
            set
            {
                if (value == null || !value.Any())
                    _customConversion = null;

                else
                    _customConversion = value;
            }
        }

        public NzbDroneValidationResult Validate() => new(Validator.Validate(this));
    }

    public enum TargetAudioFormat
    {
        [FieldOption(Label = "AAC", Hint = "Convert to AAC format.")]
        AAC = 1,

        [FieldOption(Label = "MP3", Hint = "Convert to MP3 format.")]
        MP3 = 2,

        [FieldOption(Label = "Opus", Hint = "Convert to Opus format.")]
        Opus = 3,

        [FieldOption(Label = "FLAC", Hint = "Convert to FLAC format.")]
        FLAC = 5,
    }
}

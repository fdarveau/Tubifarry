﻿using System.Text.RegularExpressions;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace Tubifarry.Download.Clients.Soulseek
{
    internal class SlskdProviderSettingsValidator : AbstractValidator<SlskdProviderSettings>
    {
        public SlskdProviderSettingsValidator()
        {
            // Base URL validation
            RuleFor(c => c.BaseUrl)
                .ValidRootUrl()
                .Must(url => !url.EndsWith("/"))
                .WithMessage("Base URL must not end with a slash ('/').");

            // API Key validation
            RuleFor(c => c.ApiKey)
                .NotEmpty()
                .WithMessage("API Key is required.");

            // Validate DownloadPath can be null or empty, or a valid directory path
            RuleFor(x => x.DownloadPath)
                .Must(path => string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
                .WithMessage("Download path must be a valid directory path.")
                .When(x => !string.IsNullOrEmpty(x.DownloadPath));

            // Validate LRCLIBInstance URL
            RuleFor(x => x.LRCLIBInstance)
                .IsValidUrl()
                .WithMessage("LRCLIB instance URL must be a valid URL.");

            // Timeout validation (only if it has a value)
            RuleFor(c => c.Timeout)
                .GreaterThanOrEqualTo(0.1)
                .WithMessage("Timeout must be at least 0.1 hours.")
                .When(c => c.Timeout.HasValue);
        }
    }

    public class SlskdProviderSettings : IProviderConfig
    {
        private static readonly SlskdProviderSettingsValidator Validator = new();

        [FieldDefinition(0, Label = "URL", Type = FieldType.Url, Placeholder = "http://localhost:5030", HelpText = "The URL of your Slskd instance.")]
        public string BaseUrl { get; set; } = "http://localhost:5030";

        private string? _host;


        [FieldDefinition(0, Label = "Host", Type = FieldType.Textbox, Hidden = HiddenType.Hidden)]
        public string Host
        {
            get
            {
                if (_host == null)
                {
                    var hostRegex = new Regex("(.+://)?(.+)(:|/)");
                    var hostMatch = hostRegex.Match(BaseUrl);
                    if (!hostMatch.Success)
                    {
                        _host = BaseUrl;
                    }
                    else
                    {
                        _host = hostMatch.Groups[2].Value;
                    }


                }
                return _host;
            }
            set
            {
                // Nothing to do
            }
        }

        [FieldDefinition(2, Label = "API Key", Type = FieldType.Textbox, Privacy = PrivacyLevel.ApiKey, HelpText = "The API key for your Slskd instance. You can find or set this in the Slskd's settings under 'Options'.", Placeholder = "Enter your API key")]
        public string ApiKey { get; set; } = string.Empty;

        [FieldDefinition(3, Label = "Download Path", Type = FieldType.Path, HelpText = "Specify the directory where downloaded files will be saved. If not specified, Slskd's default download path is used.")]
        public string DownloadPath { get; set; } = string.Empty;

        [FieldDefinition(5, Label = "Use LRCLIB for Lyrics", HelpText = "Enable this option to fetch lyrics from LRCLIB after the download is complete.", Type = FieldType.Checkbox)]
        public bool UseLRCLIB { get; set; } = false;

        [FieldDefinition(6, Label = "LRC Lib Instance", Type = FieldType.Url, HelpText = "The URL of a LRC Lib instance to connect to. Default is 'https://lrclib.net'.", Advanced = true)]
        public string LRCLIBInstance { get; set; } = "https://lrclib.net";

        [FieldDefinition(7, Label = "Timeout", Type = FieldType.Textbox, HelpText = "Specify the maximum time to wait for a response from the Slskd instance before timing out. Fractional values are allowed (e.g., 1.5 for 1 hour and 30 minutes). Set leave blank for no timeout.", Unit = "hours", Advanced = true, Placeholder = "Enter timeout in hours")]
        public double? Timeout { get; set; }

        [FieldDefinition(98, Label = "Is Fetched remote", Type = FieldType.Checkbox, Hidden = HiddenType.Hidden)]
        public bool IsRemotePath { get; set; }

        [FieldDefinition(99, Label = "Is Localhost", Type = FieldType.Checkbox, Hidden = HiddenType.Hidden)]
        public bool IsLocalhost { get; set; }


        public TimeSpan? GetTimeout() => Timeout == null ? null : TimeSpan.FromHours(Timeout.Value);

        public NzbDroneValidationResult Validate() => new(Validator.Validate(this));
    }
}

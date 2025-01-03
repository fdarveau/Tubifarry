using NLog;
using NzbDrone.Core.Parser.Model;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using YouTubeMusicAPI.Models.Info;

namespace Tubifarry.Core
{
    internal class AudioMetadataHandler
    {
        private readonly string _trackPath;
        private readonly Logger? _logger;
        public Lyric? Lyric { get; set; }
        public byte[]? AlbumCover { get; set; }
        public string FileType { get; }

        public bool UseID3v2_3 { get; set; }

        public AudioMetadataHandler(string originalPath, Logger? logger)
        {
            _trackPath = originalPath;
            _logger = logger;
            FileType = DetectFileType();
        }


        public async Task<bool> TryCreateLrcFileAsync(CancellationToken token)
        {
            if (Lyric?.SyncedLyrics == null)
                return false;
            try
            {
                string lrcContent = string.Join(Environment.NewLine, Lyric.SyncedLyrics
                    .Where(lyric => !string.IsNullOrEmpty(lyric.LrcTimestamp) && !string.IsNullOrEmpty(lyric.Line))
                    .Select(lyric => $"{lyric.LrcTimestamp} {lyric.Line}"));
                await File.WriteAllTextAsync(Path.ChangeExtension(_trackPath, ".lrc"), lrcContent, token);
            }
            catch (Exception) { return false; }
            return true;
        }

        public bool IsMP3() => FileType == "MP3";

        public bool TryEmbedMetadataInTrack(AlbumInfo albumInfo, AlbumSongInfo trackInfo, ReleaseInfo releaseInfo)
        {
            try
            {
                using TagLib.File file = TagLib.File.Create(_trackPath);

                if (UseID3v2_3)
                {
                    TagLib.Id3v2.Tag.DefaultVersion = 3;
                    TagLib.Id3v2.Tag.ForceDefaultVersion = true;
                }
                else
                {
                    TagLib.Id3v2.Tag.DefaultVersion = 4;
                    TagLib.Id3v2.Tag.ForceDefaultVersion = false;
                }

                if (!string.IsNullOrEmpty(trackInfo.Name))
                    file.Tag.Title = trackInfo.Name;

                if (trackInfo.SongNumber.HasValue)
                    file.Tag.Track = (uint)trackInfo.SongNumber.Value;


                if (albumInfo.SongCount > 0)
                    file.Tag.TrackCount = (uint)albumInfo.SongCount;


                if (!string.IsNullOrEmpty(releaseInfo.Album))
                    file.Tag.Album = releaseInfo.Album;


                if (releaseInfo.PublishDate.Year > 0)
                    file.Tag.Year = (uint)releaseInfo.PublishDate.Year;


                if (!string.IsNullOrEmpty(releaseInfo.Artist))
                {
                    file.Tag.AlbumArtists = albumInfo.Artists.Select(x => x.Name).ToArray();
                    file.Tag.Performers = new[] { releaseInfo.Artist };
                }


                if (trackInfo.IsExplicit)
                    file.Tag.Comment = "EXPLICIT";

                try
                {
                    if (AlbumCover != null)
                        file.Tag.Pictures = new TagLib.IPicture[] { new TagLib.Picture(new TagLib.ByteVector(AlbumCover)) };
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, $"Failed to embed cover in track: {_trackPath}");
                }

                if (!string.IsNullOrEmpty(Lyric?.PlainLyrics))
                    file.Tag.Lyrics = Lyric.PlainLyrics;

                file.Save();
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"Failed to embed metadata in track: {_trackPath}");
                return false;
            }
            return true;
        }

        public async Task<bool> TryConvertToMP3()
        {
            if (IsMP3())
            {
                _logger?.Error($"retrun true");
                return true;
            }

            try
            {
                string? convertedPath = await ConvertToMp3Async();

                if (convertedPath == null)
                {
                    _logger?.Error($"Failed to convert {FileType} file to MP3: {_trackPath}");
                    return false;
                }
                File.Move(convertedPath, _trackPath, true);
                return true;
            }
            catch (Exception) { return false; }
        }

        private string DetectFileType()
        {
            try
            {
                using FileStream stream = new(_trackPath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[12]; // Read the first 12 bytes for most signatures
                stream.Read(buffer, 0, buffer.Length);

                // Check for MP3 (ID3 tag)
                if (buffer[0] == 'I' && buffer[1] == 'D' && buffer[2] == '3')
                    return "MP3";

                // Check for MP3 (MPEG frame)
                if (buffer[0] == 0xFF && (buffer[1] & 0xE0) == 0xE0)
                    return "MP3";

                // Check for WAV (RIFF header)
                if (buffer[0] == 'R' && buffer[1] == 'I' && buffer[2] == 'F' && buffer[3] == 'F')
                    return "WAV";

                // Check for AIFF (FORM header)
                if (buffer[0] == 'F' && buffer[1] == 'O' && buffer[2] == 'R' && buffer[3] == 'M')
                    return "AIFF";

                // Check for FLAC (fLaC header)
                if (buffer[0] == 'f' && buffer[1] == 'L' && buffer[2] == 'a' && buffer[3] == 'C')
                    return "FLAC";

                // Check for AAC (ADTS frame)
                if (buffer[0] == 0xFF && (buffer[1] == 0xF1 || buffer[1] == 0xF9))
                    return "AAC";

                // Check for OGG (OggS header)
                if (buffer[0] == 'O' && buffer[1] == 'g' && buffer[2] == 'g' && buffer[3] == 'S')
                    return "OGG";

                // Check for MP4 (ftyp header)
                if (buffer[4] == 'f' && buffer[5] == 't' && buffer[6] == 'y' && buffer[7] == 'p')
                    return "MP4";

                // Check for MIDI (MThd header)
                if (buffer[0] == 'M' && buffer[1] == 'T' && buffer[2] == 'h' && buffer[3] == 'd')
                    return "MIDI";

                // Check for AMR (#!AMR header)
                if (buffer[0] == '#' && buffer[1] == '!' && buffer[2] == 'A' && buffer[3] == 'M' && buffer[4] == 'R')
                    return "AMR";

                // Check for WMA (ASF header)
                if (buffer[0] == 0x30 && buffer[1] == 0x26 && buffer[2] == 0xB2 && buffer[3] == 0x75 &&
                    buffer[4] == 0x8E && buffer[5] == 0x66 && buffer[6] == 0xCF && buffer[7] == 0x11)
                    return "WMA";
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error reading file: {ex.Message}");
                return "Unknown";
            }

            return "Unknown";
        }

        public static bool FFmpegIsInstalled => !string.IsNullOrEmpty(FFmpeg.ExecutablesPath) && Directory.GetFiles(FFmpeg.ExecutablesPath, "ffmpeg.*").Any();

        public static Task InstallFFmpeg(string path)
        {
            FFmpeg.SetExecutablesPath(path);
            return FFmpegIsInstalled ? Task.CompletedTask : FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, path);

        }

        private async Task<string?> ConvertToMp3Async()
        {
            if (!FFmpegIsInstalled)
                return null;
            string outputPath = Path.ChangeExtension(_trackPath, ".converted.mp3");

            try
            {
                IConversion conversion = await FFmpeg.Conversions.FromSnippet.Convert(_trackPath, outputPath);
                conversion.AddParameter("-codec:a libmp3lame");
                conversion.AddParameter("-q:a 0");
                conversion.AddParameter("-preset insane");

                await conversion.Start();
                _logger?.Debug($"Successfully converted file to MP3: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, $"Failed to convert file to MP3: {_trackPath}");
                return null;
            }
        }
    }
}

# Tubifarry for Lidarr 🎶  
![Downloads](https://img.shields.io/github/downloads/TypNull/Tubifarry/total)  ![GitHub release (latest by date)](https://img.shields.io/github/v/release/TypNull/Tubifarry)  ![GitHub last commit](https://img.shields.io/github/last-commit/TypNull/Tubifarry)  ![License](https://img.shields.io/github/license/TypNull/Tubifarry)  ![GitHub stars](https://img.shields.io/github/stars/TypNull/Tubifarry)  

This plugin fetches metadata from **Spotify** and **YouTube** to download music directly from YouTube. It builds on the groundwork laid by trevTV's projects and utilizes the YouTube API for seamless integration. 🛠️  

---

## Installation 🚀  
To use Tubifarry, your Lidarr setup must be on the `plugins` branch. Below is a detailed guide to get started.  

### Docker Setup (Hotio Image) 🐳  
Here’s the Docker path for Hotio's image:  
```yml  
    image: ghcr.io/hotio/lidarr:pr-plugins  
```  

---

### Prerequisites ⚙️  
1. **FFmpeg**: FFmpeg is **required** for converting downloaded files to MP3 format. Ensure FFmpeg is installed and available on Windows system PATH or the specified ffmpeg path. If not, you can attempt to download it automatically during setup. Without FFmpeg, many songs fail to process correctly because they are downloaded as MP4 files, and TagLib cannot add metadata to MP4 formats.  

2. **Max Audio Quality**: Tubifarry supports a maximum audio quality of **128 kb/s** for downloaded files. Ensure your settings align with this for optimal performance.  

---

### Plugin Installation 📥  
1. **Install the Plugin**:  
   - In Lidarr, navigate to `System -> Plugins`.  
   - Paste `https://github.com/TypNull/Tubifarry` into the GitHub URL box and press **Install**.  

2. **Configure the Indexer**:  
   - Go to `Settings -> Indexers` and press **Add**.  
   - In the modal, select `Tubifarry` (found under **Other** at the bottom).  

3. **Set Up the Download Client**:  
   - Go to `Settings -> Download Clients` and press **Add**.  
   - In the modal, choose `Youtube` (under **Other** at the bottom).  
   - Set the download path and adjust other settings to your preference.  

4. **Enable Tubifarry in Delay Profiles**:  
   - Go to `Settings -> Profiles -> Delay Profiles`.  
   - For each profile, click the **wrench icon** on the right and enable Tubifarry.  

---

### Troubleshooting 🛠️  
- **FFmpeg Issues**: If songs fail to process, ensure FFmpeg is correctly installed and accessible in your system's PATH. If not, try reinstalling or downloading it manually.  
- **MP4 Metadata**: If metadata is not being added to downloaded files, verify that FFmpeg is converting files to MP3 format. MP4 files are not supported for metadata tagging by TagLib.  
- **Audio Quality**: Ensure the maximum audio quality is set to **128 kb/s** in your settings to avoid compatibility issues.  

---

## Acknowledgments 🙌  
Special thanks to **trevTV** for the groundwork with [Lidarr.Plugin.Tidal](https://github.com/TrevTV/Lidarr.Plugin.Tidal), [Lidarr.Plugin.Deezer](https://github.com/TrevTV/Lidarr.Plugin.Deezer), and [Lidarr.Plugin.Qobuz](https://github.com/TrevTV/Lidarr.Plugin.Qobuz). Additionally, thanks to [IcySnex/YouTubeMusicAPI](https://github.com/IcySnex/YouTubeMusicAPI) for providing the YouTube API. 🎉  


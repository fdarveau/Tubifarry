# Tubifarry for Lidarr 🎶  
![Downloads](https://img.shields.io/github/downloads/TypNull/Tubifarry/total)  ![GitHub release (latest by date)](https://img.shields.io/github/v/release/TypNull/Tubifarry)  ![GitHub last commit](https://img.shields.io/github/last-commit/TypNull/Tubifarry)  ![License](https://img.shields.io/github/license/TypNull/Tubifarry)  ![GitHub stars](https://img.shields.io/github/stars/TypNull/Tubifarry)  

Tubifarry is a plugin for **Lidarr** that fetches metadata from **Spotify** and **YouTube**, enabling direct music downloads from YouTube. Built on the foundation of trevTV's projects, it leverages the YouTube API for smooth integration. 🛠️  

---

## Installation 🚀  
To use Tubifarry, ensure your Lidarr setup is on the `plugins` branch. Follow the steps below to get started.  

### Docker Setup (Hotio Image) 🐳  
For Docker users using Hotio's image, use the following path:  
```yml  
    image: ghcr.io/hotio/lidarr:pr-plugins  
```  

### Non-Docker Installation  
To switch to the Plugins Branch:  
1. Open Lidarr and navigate to `System -> General`.  
2. Scroll down to the **Branch** section.  
3. Replace "master" with "plugins".  
4. Force an update check to update Lidarr to the plugins branch.  

---

### Prerequisites ⚙️  
1. **FFmpeg**: FFmpeg is **essential** for converting downloaded files to MP3 format. Ensure FFmpeg is installed and accessible in your system's PATH or the specified FFmpeg path. If not, you can attempt to download it automatically during setup. Without FFmpeg, many songs may fail to process correctly, as they are downloaded as MP4 files, and TagLib cannot add metadata to MP4 formats.  

2. **Max Audio Quality**: Tubifarry supports a maximum audio quality of **256kb/s** for downloaded files. However, most files are in **MP3-VBR-V0** format.  

   **What is MP3-VBR-V0?**  
   MP3-VBR-V0 is a high-quality audio format that uses **Variable Bitrate (VBR)** to optimize both sound quality and file size. Unlike a fixed bitrate (e.g., 128kb/s), VBR adjusts the bitrate dynamically based on the complexity of the audio. For example, it uses a higher bitrate for detailed parts of a song (like a chorus) and a lower bitrate for simpler parts (like silence). 

---

### Plugin Installation 📥  

#### **For Docker Users**:  
1. **Install the Plugin**:  
   - In Lidarr, go to `System -> Plugins`.  
   - Paste `https://github.com/TypNull/Tubifarry` into the GitHub URL box and click **Install**.  

2. **Configure the Indexer**:  
   - Navigate to `Settings -> Indexers` and click **Add**.  
   - In the modal, select `Tubifarry` (located under **Other** at the bottom).  

3. **Set Up the Download Client**:  
   - Go to `Settings -> Download Clients` and click **Add**.  
   - In the modal, choose `Youtube` (under **Other** at the bottom).  
   - Set the download path and adjust other settings as needed.  
   - **Important**: Ensure the FFmpeg path is correctly configured!  

---

### Troubleshooting 🛠️  
- **FFmpeg Issues**: If songs fail to process, verify that FFmpeg is correctly installed and accessible in your system's PATH. If not, try reinstalling or downloading it manually.  
- **MP4 Metadata**: If metadata is not being added to downloaded files, confirm that FFmpeg is converting files to MP3 format (check debug logs). MP4 files are not supported for metadata tagging by TagLib.  
- **Audio Quality**: Ensure the maximum audio quality is set to **128 kb/s** in your settings to avoid compatibility issues.  

---

## Acknowledgments 🙌  
Special thanks to **trevTV** for laying the groundwork with [Lidarr.Plugin.Tidal](https://github.com/TrevTV/Lidarr.Plugin.Tidal), [Lidarr.Plugin.Deezer](https://github.com/TrevTV/Lidarr.Plugin.Deezer), and [Lidarr.Plugin.Qobuz](https://github.com/TrevTV/Lidarr.Plugin.Qobuz). Additionally, thanks to [IcySnex/YouTubeMusicAPI](https://github.com/IcySnex/YouTubeMusicAPI) for providing the YouTube API. 🎉  

---

## Contributing 🤝  
If you'd like to contribute to Tubifarry, feel free to open issues or submit pull requests on the [GitHub repository](https://github.com/TypNull/Tubifarry). Your feedback and contributions are highly appreciated!  

---

## License 📄  
Tubifarry is licensed under the MIT License. See the [LICENSE](https://github.com/TypNull/Tubifarry/blob/main/LICENSE) file for more details.  

---

Enjoy seamless music downloads with Tubifarry! 🎧
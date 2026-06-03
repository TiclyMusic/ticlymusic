# Ticly Music

A modern, user-friendly SpotiFLAC music player for Windows built with WPF and .NET 8.

## 🚀 Features

- **SpotiFLAC Integration**: Search Spotify and stream via your SpotiFLAC backend
- **Modern UI**: Clean, intuitive interface with custom window controls
- **Keyboard Shortcuts**: Full keyboard control for efficient usage
- **Persistent Settings**: Remembers window position, size, and volume
- **Loop Mode**: Continuous playback with visual feedback
- **Volume Control**: Visual volume slider with percentage display
- **Configuration**: Easy Spotify/SpotiFLAC setup through settings window
- **Synced Lyrics**: Letter-by-letter lyric highlighting (LRC or JSON)

## 🎯 Getting Started

### Prerequisites
- Windows 10 or later
- .NET 8.0 Runtime
- Spotify Developer Client ID and Client Secret
- SpotiFLAC backend base URL (stream + lyrics endpoints)

### Installation

1. **Download**: Get the latest release from the [Releases](https://github.com/TiclyMusic/ticlymusic/releases) section
2. **Extract**: Unzip to your preferred location
3. **Run**: Launch `TiclyMusic.exe`

### First-Time Setup

1. **Create a Spotify App**:
   - Visit [Spotify Developer Dashboard](https://developer.spotify.com/dashboard)
   - Create a new app
   - Copy the Client ID and Client Secret

2. **Configure SpotiFLAC**:
   - Ensure you have a SpotiFLAC backend that exposes `/stream` and `/lyrics` endpoints
   - Copy the base URL (and API key if required)

3. **Configure in App**:
   - Open Ticly Music
   - Click the settings button (⚙) in the title bar
   - Enter your Spotify Client ID/Secret and SpotiFLAC base URL
   - Click "Save"

## 🎮 Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Space` | Play/Pause toggle |
| `Ctrl+S` | Stop playback |
| `Ctrl+L` | Toggle loop mode |
| `Ctrl+F` | Focus search box |
| `Ctrl+,` | Open settings |
| `Enter` | Search (when in search box) |

## 🔧 Development

### Building from Source

```bash
# Clone the repository
git clone https://github.com/TiclyMusic/ticlymusic.git
cd ticlymusic

# Build the project
dotnet build

# Run the application  
dotnet run
```

### Requirements for Development
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- Windows (WPF requirement)

## 🎨 UI/UX Improvements

Recent enhancements include:
- [x] Modern color scheme with intuitive button colors
- [x] Draggable custom title bar
- [x] Visual feedback for loop mode (ON/OFF)
- [x] Volume percentage display
- [x] Keyboard shortcut help text
- [x] Clear search button
- [x] Settings window for easy configuration
- [x] Persistent window state and preferences
- [x] Improved error messages and user guidance

## 🔒 Privacy & Security

- Your Spotify/SpotiFLAC credentials are stored locally in your user profile
- Data is sent to Spotify for search and to your SpotiFLAC backend for streaming/lyrics
- Configuration files are stored in `%APPDATA%/TiclyMusic/`

## 🚧 Roadmap

- [ ] Playlist support
- [ ] Search history
- [ ] Audio equalizer
- [ ] Previous/Next track functionality
- [ ] macOS and Linux versions
- [ ] Mobile app (Android)

## 📝 License

This project is open source. Please check the license file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

---

**Note**: This application requires Spotify client credentials and a SpotiFLAC backend endpoint to function.

# Ticly Music

A modern, user-friendly YouTube music player for Windows built with WPF and .NET 8.

## 🚀 Features

- **YouTube Integration**: Search and play music directly from YouTube
- **Modern UI**: Clean, intuitive interface with custom window controls
- **Keyboard Shortcuts**: Full keyboard control for efficient usage
- **Persistent Settings**: Remembers window position, size, and volume
- **Loop Mode**: Continuous playback with visual feedback
- **Volume Control**: Visual volume slider with percentage display
- **Configuration**: Easy API key setup through settings window

## 🎯 Getting Started

### Prerequisites
- Windows 10 or later
- .NET 8.0 Runtime
- YouTube Data API v3 key

### Installation

1. **Download**: Get the latest release from the [Releases](https://github.com/TiclyMusic/ticlymusic/releases) section
2. **Extract**: Unzip to your preferred location
3. **Run**: Launch `TiclyMusic.exe`

### First-Time Setup

1. **Get YouTube API Key**:
   - Visit [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing one
   - Enable YouTube Data API v3
   - Create credentials (API Key)
   - Restrict the key to YouTube Data API v3 (recommended)

2. **Configure API Key**:
   - Open Ticly Music
   - Click the settings button (⚙) in the title bar
   - Enter your YouTube API key
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
- ✅ Modern color scheme with intuitive button colors
- ✅ Draggable custom title bar
- ✅ Visual feedback for loop mode (ON/OFF)
- ✅ Volume percentage display
- ✅ Keyboard shortcut help text
- ✅ Clear search button
- ✅ Settings window for easy configuration
- ✅ Persistent window state and preferences
- ✅ Improved error messages and user guidance

## 🔒 Privacy & Security

- Your YouTube API key is stored locally in your user profile
- No data is sent to third parties except YouTube for music search
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

**Note**: This application requires a YouTube Data API v3 key to function. The key is free but has usage quotas. For personal use, the default quota should be sufficient.

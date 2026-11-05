<p align="center"><img src="windows/GTool.Windows/Assets/source/wordmark-dark.webp" width="520" alt="G-Tool"></p>

<p align="center"><b>English</b> | <a href="README.tr.md">Türkçe</a></p>

# G-Tool

A local file conversion and editing tool for Windows. Drag your files in, open the wheel and choose what to do. Your files stay on your computer.

## Features

- **Conversion:** between image, document, audio, video, archive and spreadsheet formats
- **Photos:** crop, annotate, redact, frame, collage and colour adjustments
- **Video and audio:** trim, split, merge, mute and change speed
- **PDF:** merge, split, organise pages and compress
- **Web downloads:** media from public links
- **Design:** glass interface that shows your wallpaper, dark and light themes, English and Turkish

## Installation

1. Download `G-Tool-Windows-x64.zip` from the [latest release](https://github.com/Sinan0011/G-Tool/releases/latest).
2. Extract the whole ZIP into a folder of your own that you can write to, such as Documents. Do not use `Program Files` or the top level of a drive (such as `C:\`), otherwise updates cannot be installed.
3. Open `G-Tool.exe`. You do not need to install .NET.

The app does not have a code signing certificate yet, so Windows SmartScreen may show a warning. Choose **More info**, then **Run anyway**.

On first start G-Tool offers to download the tools it needs, such as FFmpeg, yt-dlp, Deno and LibreOffice, from their official sources and checks their fingerprints. It also asks once whether it should start when you sign in.

The **Help** page inside the app explains every feature, the file buttons and the keyboard shortcuts.

## Updates

G-Tool checks for a new version when it opens and every hour while it runs. When one is available, Windows shows a notice and you install it with one click. Every update is signed, and a file whose signature cannot be verified is not installed. The old version is removed after the new one starts. Without internet the app keeps working normally. Automatic checks can be turned off in Settings.

## Source and license

Built on the [Nashi](https://github.com/orkunisitmak/nashi-app) (MIT) project by [Orkun Işıtmak](https://github.com/orkunisitmak).

Released under the MIT License, see [LICENSE](LICENSE). Bundled components keep their own licenses, see the [Windows third-party notices](windows/THIRD-PARTY-NOTICES.txt) and [Resources/licenses](Resources/licenses).

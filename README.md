<div align="center">

Tor Multi-Proxy Launcher 🚀
A modern WPF application for launching and managing multiple instances of Tor, creating a pool of SOCKS5 proxies on different local ports.

</div>

<p align="center">
  <a href="https://github.com/MoGLCL/TorProxy/blob/main/LICENSE"><img src="https://img.shields.io/github/license/MoGLCL/TorProxy" alt="License"></a>
  <a href="https://github.com/MoGLCL/TorProxy/commits/main"><img src="https://img.shields.io/github/last-commit/MoGLCL/TorProxy" alt="Last Commit"></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-Framework%204.7.2-blueviolet.svg" alt=".NET Version"></a>
  <a href="https://github.com/MoGLCL/TorProxy/issues"><img src="https://img.shields.io/github/issues/MoGLCL/TorProxy" alt="Issues"></a>
  <a href="#"><img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg" alt="PRs Welcome"></a>
</p>

<details>
<summary><strong>Table of Contents</strong></summary>

✨ Key Features

📋 Requirements

🚀 How to Use

🛠️ Building from Source

📄 License

</details>

✨ Key Features
⚡ Dynamic Instance Creation: Launch any number of Tor instances as needed.

🔄 Automatic Port Management: Automatically assigns incremental SOCKS and Control ports starting from 9050.

📁 Isolated Data Directories: Creates separate TorData folders for each instance to prevent state sharing.

⚙️ Advanced Process Management:

Automatically terminates all running Tor processes on application exit.

Forcefully kills all existing tor.exe processes on the system before starting new ones to prevent port conflicts.

🎨 Modern UI:

Sleek, chromeless window with acrylic blur effects.

SVG icons for a clean, resolution-independent look.

Interactive log viewer for real-time feedback.

🖱️ User-Friendly Input:

Drag & Drop support for the tor.exe file.

A "Browse" button for easy file selection.

📋 Proxy List Export:

Automatically opens a new, beautifully designed window with the list of generated socks5 proxies.

"Copy to Clipboard" functionality for both individual proxies and the entire list.

💾 Persistent Settings: The application saves the last used tor.exe path and instance count for convenience.

📋 Requirements
Operating System: Windows 8 or later.

Tor Executable: A standalone tor.exe file (can be extracted from the Tor Browser bundle).

.NET Framework: Version 4.7.2 or later (usually pre-installed on modern Windows systems).

🚀 How to Use
Download: Grab the latest .exe file from the Releases page.

Locate tor.exe:

Use the Browse button to locate your tor.exe file.

Or, simply drag and drop the tor.exe file onto the application window.

Set Instance Count: Enter the number of proxies you want to create (e.g., 10).

Launch: Click the "تشغيل" (Start) button.

The application will terminate any old Tor processes and start new ones.

A new window will appear containing your list of ready-to-use SOCKS5 proxies.

Stop: Click the "إيقاف" (Stop) button at any time to terminate all running Tor instances.

🛠️ Building from Source
To build this project from the source code, you will need:

Visual Studio 2019 or later.

.NET Framework 4.7.2 Developer Pack.

Simply open the .sln file in Visual Studio and build the project in Release mode.

📄 License
This project is licensed under the MIT License. See the LICENSE file for more details.

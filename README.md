<p align="center">
  <img alt="Bookworm Enhanced logo" src="./logo.png" width="350"/>
</p>

# Bookworm Enhanced

By aidrum and TheAwesomeBlue

Bookworm Enhanced is a Bookworm iOS decompilation ported to MonoGame, available for Android, iOS, Windows, macOS and Linux. It features the entire original game in English, plus a new Spanish translation based on the PC version. Also includes landscape support for a completely modernized PC Bookworm experience!

This project is intended for both PC and mobile, although most UIs are made with phones in mind (except the landscape gameplay mode).

## How was this made?

Using [Peggle Enhanced's](https://github.com/aidrum/peggle-enhanced) SexyAppFramework, which used [PlantsVsZombies.NET](https://github.com/Mewnojs/PlantsVsZombies.NET) by Mewnojs and [Bejeweled Plus](https://github.com/ariidesu/BejeweledPlus) by ariidesu as a base. The Objective C iOS code was decompiled with Ghidra, migrated to C# and fixed/expanded from there. This is a fully native port, there are no weird emulation layers here. Please note that the code is not as readable as Peggle's, I might fix that soon.

While it was vibecoded, it has been tested properly and all modes should be playable. Please let us know if you encounter any bugs or inconsistencies.

## Why was this made?

This was made due to the Bookworm iOS release never having released on Android, but it has been enhanced to be a nice experience on computers too. This version also has ultrawide support on both landscape and portrait.

## Download

The latest release for each platform. Newer automatic builds are on the [Releases](../../releases) page as pre-releases.

| Platform | Link | Notes |
|---|---|---|
| Android | [Download](../../releases/latest/download/Bookworm-android.apk) | Android 6.0 or later. |
| iOS / iPadOS | [Download](../../releases/latest/download/Bookworm-unsigned.ipa) | iOS 15 or later. Install with AltStore, SideStore, Sideloadly or TrollStore, or add the [AltStore / SideStore source](https://aidrum.github.io/bookworm-enhanced/ios/) to get updates. |
| Windows (x64) | [Download](../../releases/latest/download/Bookworm-windows-x64.zip) | Extract, then run `Bookworm.exe`. |
| macOS (Apple Silicon) | [Download](../../releases/latest/download/Bookworm-macOS-osx-arm64.zip) | The first time, right-click the app and choose **Open**. |
| macOS (Intel) | [Download](../../releases/latest/download/Bookworm-macOS-osx-x64.zip) | The first time, right-click the app and choose **Open**. |
| Linux (x64, AppImage) | [Download](../../releases/latest/download/Bookworm-x86_64.AppImage) | Make it executable, then run it. |
| Linux (x64, .tar.gz) | [Download](../../releases/latest/download/Bookworm-linux-x64.tar.gz) | Extract, then run `Bookworm`. |


## FAQ

- **Will this get any more updates?**

  If we think of cool stuff to add, likely. If we have to fix bugs, definitely. More languages coming soon?

- **Will any other PopCap Games get the same treatment?**

We already did Peggle, we'll probably not do more. Most other PopCap Games already have great ways to play them on modern mobile devices. Check More Games in-game to see some of them!
  
- **What devices are supported?**

  Any Android device with an armv7, arm64 or x64 processor running Android 6.0 or later.

- **What devices was this tested on?**

  - Redmi Note 13 Pro 5G (Android 13): Great performance with high resolution (iPad Retina) texture settings.
  - Huawei MediaPad T3 10 (Android 7): Great performance with medium resolution (iPhone Retina/iPad) texture settings.
  - Samsung Galaxy J2 Prime (Android 6): Great performance with medium resolution (iPhone Retina/iPad) texture settings.

- **Does it support controllers?**

  Not yet, might be implemented in the future if there's enough demand. There's the GBA version as a reference for how it would control.


- **Dynamic music??**

  Maybe in the future, but unlikely. If it does get added, it will be a setting due to performance concerns on lower end devices.

- **Was AI used to generate graphics or music?**

  Nope. AI was purely used for the game code. Graphical edits, app icon remakes and music additions were all done manually. This readme was written by hand too.

- **Why was AI used for the code?**

  We do not have the time or resources needed to do a full analysis of the iOS code manually. We playtested it for a while, even comparing it with real hardware to ensure the game was working right/accurately. (Compared to iPad 3 and iPhone 4S)

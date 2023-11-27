ShinraMeter
==============
ShinraMeter is a DPS Meter for Menma's TERA based off https://github.com/gothos-folly/TeraDamageMeter. 

Download: https://emilia.menmastera.com/shinrameter/ShinraMeterV2.93-MT.zip

ShinraMeter is originally developed by Gl0 and Yukikoo/Neowutran and this version is currently maintained by the Menma's TERA team.

## Prerequisites

* [.NET Core Desktop Runtime 3.1 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-3.1.32-windows-x64-installer)
* Up to date Windows 10 recommended.

## How to Install

1. Download **the latest version of ShinraMeter** from the link above, and extract it.
2. Download and Install [npcap](https://nmap.org/npcap/) ([source of the project](https://github.com/nmap/npcap)) or [win10pcap](http://www.win10pcap.org) ([source of the project](https://github.com/SoftEtherVPN/Win10Pcap))
3. Start ShinraMeter.exe, login to Menma's TERA. ShinraMeter should now detect the server's name, change the tray icon to guild icon of your logged in character, and count damage.

**If it doesn't work in this setup (no server name in tray icon tooltip/no guild icon/no damage counted)**:

4. Enable Raw Sockets Mode:

    4.1. Uninstall npcap/win10pcap, as it might not support your network connection type.

    4.2. Run the add_firewall_exception.bat file as administrator from the ShinraMeter directory.

    4.3. Click the ShinraMeter icon on your tray to open settings, go to Detection Settings and disable the "Use npcap" option.

    4.4. Restart ShinraMeter.

**ShinraMeter should now be working - if not, you might have done something wrong, or you are using an outdated/modified version of the meter.**

If the meter still can't find your game connection, disable/uninstall any "network booster" applications and try again from step 2. Such applications may include:
* SmartByte Network services - preinstalled on dell notebooks
* killer network service - coming with Killer gaming NICs


## IMPORTANT NOTE
Do not put the config files in read-only mode, as this will prevent ShinraMeter from loading properly and make it crash on exit. It needs access to modify these files in order to save your settings.

## Support
Join our [discord server](https://menmastera.com/shop) for additional support.

<p align="center">
  <h3 align="center">AstroModLoader Classic</h3>
</p>
<p align="center"><img src="https://i.imgur.com/ZCtHnZ1.png"></p>

AstroModLoader Classic is a modern port of the original AstroModLoader, an open-source mod manager in C# for Astroneer .pak mods on Steam and the Microsoft Store, for modern versions of Astroneer (as of 2024). It includes support for mod profiles, automatic mod integration, and the ability to easily swap between multiple mod versions so that you can worry less about setup and more about playing.

## Features
AstroModLoader Classic includes the following features and more:
* A simple GUI to enable, disable, and switch the version of Astroneer .pak mods
* Mod metadata analysis to provide additional information about compliant mods, such as name, description, and compatible Astroneer version
* Automatic updating of supporting mods
* A profile system, to allow switching between sets of mods for different playthroughs
* Customizable appearance, with both light and dark themes as well as nine accent color presets
* Customizable mod load order by holding SHIFT and using the arrow keys in the mod list
* Easy drag-and-drop functionality to install mods
* Syncing of mods with modded [AstroLauncher](https://github.com/ricky-davis/AstroLauncher) servers
* [Built-in mod integration](https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator) to help prevent mod conflict and detect mod conflicts with servers

## Usage
To run a local copy of AstroModLoader Classic, visit the [Releases tab](https://github.com/atenfyr/AstroModLoader-Classic/releases) to download the executable, or clone the repository and build AstroModLoader Classic yourself within Visual Studio.

### Mod Installation
To install mods, drag and drop the .zip or the .pak file of your mod onto the AstroModLoader Classic window while it is running.

Alternatively, on Steam, you can also manually add mods for use with AstroModLoader Classic by placing them into the `%localappdata%\Astro\Saved\Mods` directory.
On the Windows Store, you can place them into the `%localappdata%\Packages\SystemEraSoftworks.29415440E1269_ftk5pbg2rayv2\LocalState\Astro\Saved\Mods` directory.

### Usage Notes
AstroModLoader Classic features a fully-functional set of hotkeys to fully control your mods list. Below is a list of keyboard commands to manipulate the list of mods:
* DEL deletes all versions of the currently selected mod.
* ALT+DEL deletes all versions of the currently selected mod except for the newest from disk.
* SHIFT+UP and SHIFT+DOWN adjust the position of the currently selected mod. Mods at the top of the list (low priority) are loaded by the game before mods at the bottom of the list (high priority).
* ESC de-selects the current row in the mod list.

Additionally, the following keyboard commands can be used within the profile selector:
* DEL deletes the current profile.
* ENTER loads the current profile.
* X exports the current profile as a .zip file, which other AstroModLoader Classic users can import by dragging and dropping the .zip file onto the AstroModLoader Classic window while it is running.

Additionally, the following keyboard commands can be used within popup windows:
* ENTER and ESC can be used within confirmation windows to select "Yes" or "No" respectively, and "OK" or "Cancel" respectively in text input windows.
* TAB can be used to switch selection between buttons. ENTER can then be used to press the currently selected button.

### Server Mode
AstroModLoader Classic can be used to install mods on dedicated servers. To do this, place the executable file into the root folder of your server installation directory, and execute it as normal to start in server mode. You can also simply pass in the `--server` command line parameter.

### Command Line Parameters
AstroModLoader Classic has support for the following command line parameters:
* `--server`: Forces AstroModLoader Classic to operate as if it is being ran for a server.
* `--client`: Forces AstroModLoader Classic to operate as if it is being ran for a client.
* `--data <path>`: Specifies the %localappdata% folder or the local equivalent of it.
* `--next_launch_path <path>`: Specifies a path to a file to store as the launch script.

## Prerequisites
* .NET Desktop Runtime 8.0
* A copy of Astroneer

## Licensing
AstroModLoader Classic is licensed under the MIT license, which can be found in [the LICENSE.md file.](https://github.com/atenfyr/AstroModLoader-Classic/blob/master/LICENSE.md) In addition, necessary licenses for the third-party material used in this software can be found in [the NOTICE.md file.](https://github.com/atenfyr/AstroModLoader-Classic/blob/master/NOTICE.md)

## Blueprint API
AstroModLoader Classic performs integration with AstroModIntegrator Classic, which supports the same fundamental integration features that [astro_modloader](https://github.com/AstroTechies/astro_modloader) (the Rust re-write) does, but notably has a different and more fully-featured blueprint API. You can find more information about the AstroModIntegrator Classic blueprint API here: [https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator/BlueprintAssets](https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator/BlueprintAssets)


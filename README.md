# Multigrid Projector

Multigrid Projector for the Space Engineers game.

- Build and repair PDCs, mechs and more
- Works both in survival and creative
- Works both in single player and multiplayer

## Install the client plugin

1. Subscribe to this mod
2. Install the [Plugin Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=2407984968)
3. Start the game
4. Open the Plugins menu (should be in the Main Menu)
5. Enable the Multigrid Projector plugin
6. Click on Save, then Restart (below the plugin list)

After enabling the plugin it will be active for all single player worlds you load and in case of multiplayer if the server or the other client you connect to also has the plugin loaded. Do not add the plugin as a mod to the worlds themselves, it is not required and will not load anyway.

*Enjoy!*

## Server plugins

- [Torch Server](https://torchapi.net/plugins/item/d9359ba0-9a69-41c3-971d-eb5170adb97e)
- [Dedicated Server](https://www.ferenczi.eu/se/multigrid-projector/)

**If you play on a multiplayer (Dedicated or Torch) server, then both the server plugin (on server side) and the client plugin (on the player's machine) must be installed! Using only the client plugin will not work properly.**

## Want to know more?
- [SE Mods Discord](https://discord.gg/PYPFPGf3Ca)
- [YouTube Channel](https://www.youtube.com/channel/UCc5ar3cW9qoOgdBb1FM_rxQ)
- [Patreon](https://www.patreon.com/semods)
- [Torch plugin](https://torchapi.net/plugins/item/d9359ba0-9a69-41c3-971d-eb5170adb97e)
- [Test world](https://steamcommunity.com/sharedfiles/filedetails/?id=2420963329)
- [Source code](https://github.com/viktor-ferenczi/multigrid-projector)
- [Mod API Test](https://steamcommunity.com/sharedfiles/filedetails/?id=2433810091)
- [Programmable block API](https://steamcommunity.com/sharedfiles/filedetails/?id=2471605159)
- [Bug reports](https://discord.gg/x3Z8Ug5YkQ)

Please support me if you would like to receive regular updates to this client plugin as new game versions are released.

Please vote on the bug ticket to get multigrid welding into the vanilla game, eventually:
https://support.keenswh.com/spaceengineers/pc/topic/multigrid-support-for-projectors

Thank you and enjoy!

## Credits

### Patreon Supporters

#### Admiral level
- BetaMark
- Mordith - Guardians SE
- Robot10
- Casinost
- wafoxxx

#### Captain level
- Diggz
- lazul
- jiringgot
- Kam Solastor
- NeonDrip
- NeVaR
- opesoorry
- NeVaR
- Jimbo

#### Testers
- Robot10 - Test Lead
- Radar5k
- LTP
- Mike Dude
- CMDR DarkSeraphim88
- ced
- Precorus
- opesoorry
- Spitfyre.pjs
- Random000
- gamemasterellison

### Creators
- avaness - Plugin Loader, Racing Display
- SwiftyTech - Stargate Dimensions
- Mike Dude - Guardians SE
- Fred XVI - Racing maps
- Kamikaze - M&M mod
- LTP

**Thank you very much for all your support and hard work on testing!**

## Development

### Projects

- MultigridProjector: Shared project with the general data model and logic of MGP
- MultigridProjectorClient: Client plugin "loader"
- MultigridProjectorServer: Torch server plugin "loader"
- MultigridProjectorModApiTest: Test mod for the MGP API

### Prerequisites

- .NET Framework 4.8
- [Space Engineers](https://spaceengineersgame.com)
- [Torch](https://torchapi.net) extracted and executed once
- [JetBrains Rider](https://jetbrains.com) or Visual Studio
- [Plugin Loader](https://steamcommunity.com/sharedfiles/filedetails/?id=2407984968)

### Steps

1. Clone the repository to have a local working copy
2. Edit paths in batch file, the run it: `Edit-and-run-before-opening-solution.bat`
3. Open the solution
4. Build the solution
5. Deploy locally and run
  - JetBrains: Select a run configuration for local deployment
  - Visual Studio: See the batch files in the projects

### Debugging

- Torch: Use the `Torch Debug` run configuration
- Space Engineers: Use the `Space Engineers Debug` run configuration, then attach the debugger to the game before loading the world
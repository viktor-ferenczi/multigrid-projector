# Multigrid Projector

Multigrid Projector for the Space Engineers game.

- Build and repair PDCs, mechs and more
- Works both in survival and creative
- Works both in single player and multiplayer

## Install the client plugin

1. Subscribe to this mod
2. Install [Plugin Loader](https://github.com/sepluginloader/SpaceEngineersLauncher)
3. Start the game
4. Open the Plugins menu (should be in the Main Menu)
5. Enable the Multigrid Projector plugin
6. Click on Save, then Restart (below the plugin list)

After enabling the plugin it will be active for all single player worlds you load.
In case of multiplayer full functionality is supported if the server also has the
MGP plugin installed, but most of the multigrid welding will still work if not.

Please consider supporting my work on [Patreon](https://www.patreon.com/semods) or one time via [PayPal](https://www.paypal.com/paypalme/vferenczi/).

*Thank you and enjoy!*

## Server plugins

In case of problems join the [SE Mods Discord](https://discord.gg/PYPFPGf3Ca) to get help.

**If you are using a 3rd party game server hosting provider**, then please follow their documentation on how to install the server plugin or contanct their support with your questions.

### Torch server plugin installation

[Torch plugin](https://torchapi.com/plugins/view/?guid=d9359ba0-9a69-41c3-971d-eb5170adb97e) (updated automatically by Torch)

Add the plugin on Torch's UI, then restart the server. Make sure your players are aware of the Plugin Loader, so they can install the client plugin.

### Dedicated Server plugin installation

[Plugin download](https://github.com/viktor-ferenczi/multigrid-projector/releases/) (requires manual updating)

- Open: https://github.com/viktor-ferenczi/multigrid-projector/releases/
- Download and extract the latest release: `MultigridProjectorDedicated-*.zip` 
- Keep `0Harmony.dll` and `MultigridProjectorDedicated.dll` in the same folder.
- Right click on each of the DLLs, select **Unblock** in the **Properties** dialog if you have such a button (Windows protection). 
- Start the Dedicated Server and continue to the configuration.
- Under the Plugins tab add the `MultigridProjectorDedicated.dll` file as a plugin.
- Start your server.

### Special case on Linux (Proton)

Please define the `SE_PLUGIN_DISABLE_METHOD_VERIFICATION` environment variable
to disable IL code verification.

Reason for the IL code verification is to detect potentially breaking code changes 
introduced over game updates. In such a cases the verification blocks the plugin
from loading instead of crashing later.

You can detect this case by looking in the game's log file for this text: 
`Refusing to load the plugin due to potentially incompatible code changes`

## Building from source

The top level plugin projects have dependencies on their respective game files.
The game assemblies (DLLs) are referenced from the directories defined in the
`Directory.Build.props` file. This file is not part of the repository, 
you need to create it yourself from a template:

- Copy: `Directory.Build.template.props` => `Directory.Build.props`
- Edit `Directory.Build.props` to match your local setup

You need to do this only once per working copy. 
You may also want to save this file for later reuse.

## Want to know more?

- [SE Mods Discord](https://discord.gg/PYPFPGf3Ca) FAQ, Troubleshooting, Support, Bug Reports, Discussion
- [Plugin Loader Discord](https://discord.gg/6ETGRU3CzR) Everything about plugins
- [Torch plugin](https://torchapi.com/plugins/view/?guid=d9359ba0-9a69-41c3-971d-eb5170adb97e)
- [Dedicated Server plugin](https://github.com/viktor-ferenczi/multigrid-projector/releases)
- [Test world (Rings)](https://steamcommunity.com/sharedfiles/filedetails/?id=2420963329)
- [Source code](https://github.com/viktor-ferenczi/multigrid-projector)
- [Mod API](https://github.com/viktor-ferenczi/multigrid-projector/tree/main/ModApiTest)
- [PB API](https://github.com/viktor-ferenczi/multigrid-projector/tree/main/IngameApiTest)

## Credits

### Contributors
- Viktor Ferenczi
- @SpaceGT
  * Client side welding without server plugin
  * Enqueue missing parts into assemblers (Assemble Projection)
  * Highlighting weldable or incomplete projected blocks (Highlight Blocks)
  * Build system fixes, better linking of dependencies
- @mkaito
  * Crash fix
  * Copy BoM 
- @Pas2704
  * Bug fixes

### Patreon Supporters

#### Admiral level
- BetaMark
- Mordith - Guardians SE
- Robot10
- Casinost
- wafoxxx

#### Captain level
- CaptFacepalm
- Diggz
- lazul
- jiringgot
- Kam Solastor
- NeonDrip
- NeVaR
- opesoorry
- jiringgot
- N CG
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
- Babyboarder
- LordJ

### Creators
- avaness - Plugin Loader, Racing Display
- SwiftyTech - Stargate Dimensions
- Mike Dude - Guardians SE
- Fred XVI - Racing maps
- Kamikaze - M&M mod
- Keleios
- LTP

**Thank you very much for all your support and hard work on testing!**

## Development

### Projects

- MultigridProjector: Shared project with the general data model and logic of MGP
- MultigridProjectorClient: Client plugin "loader"
- MultigridProjectorServer: Torch server plugin "loader"
- API: Examples to access MGP from mods and ingame scripts

### Prerequisites

- .NET Framework 4.8.1
- [Space Engineers](https://spaceengineersgame.com)
- [Torch](https://torchapi.com) extracted and executed once
- [JetBrains Rider](https://jetbrains.com) or Visual Studio
- [Plugin Loader](https://github.com/sepluginloader/SpaceEngineersLauncher)

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
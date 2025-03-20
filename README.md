# Macrorouting Tool
A mod by aqmlyn for the game [Celeste](https://www.celestegame.com/). It provides a UI for users to create and modify a [weighted graph](https://en.wikipedia.org/wiki/Graph_(discrete_mathematics)#Weighted_graph) detailing possible routes through a map, then choose and compare specific routes based on the info in the graph.

## Installation
Like most Celeste mods, this mod is designed for use with the Everest modloader. If you're unfamiliar, get started by reading [this page](https://github.com/EverestAPI/Resources/wiki/FAQ#playing-mods) on the Everest wiki.

***This mod is a work in progress that has not yet been released.*** Once it has been released, there will be three easy ways to install it:
* If you have [Olympus](https://everestapi.github.io/#installing-everest):
  * Open Olympus and click "Download Mods". In the mod browser, find this mod (e.g. by searching for `Macrorouting Tool`) and click the green download button.
  * Head to this mod's GameBanana page and scroll down to the Files section. There should be an Olympus button that downloads and installs this mod in a single click.
* If you don't have Olympus:
  * Download the ZIP from either the latest GitHub release or the GameBanana page's Files section. Place the ZIP in the `Mods` directory inside the directory where Celeste is installed.

## Credits
Macrorouting Tool relies heavily on the [Everest modloader](https://github.com/EverestAPI/Everest) ([licensed under MIT](https://github.com/EverestAPI/Everest/tree/stable-1.5416.0)) as well as the code from Celeste itself.
In addition, Macrorouting Tool uses [YamlDotNet v16.3.0](https://github.com/aaubry/YamlDotNet/tree/v16.3.0) (also [licensed under MIT](https://github.com/aaubry/YamlDotNet/blob/v16.3.0/LICENSE.txt)) to save and load graphs and routes to/from YAML files.

It's worth noting that Celeste is officially closed-source. I'm comfortable making this mod open source, despite it containing references to the vanilla code, because many other mods have done the same and the vanilla developers support the modding ecosystem. They sometimes play and often chat about mods (usually custom maps) on social media, and the vanilla game even has some design decisions indicating the expectation of decompiling its code, such as the winged golden berry being stored and searched for by the name `memorialTextController`.

## Setup
When this mod is first installed, there are some things that still need to be set up by opening the game and heading to this mod's section in Mod Options.
* Binds for the various actions need to be chosen in Keyboard Config and/or Controller Config.

## Usage

### Developing a Graph

### Choosing Routes

### Comparing Routes
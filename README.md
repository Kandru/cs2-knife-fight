> [!CAUTION]
> Work in progress. Might not work as expected.

# CounterstrikeSharp - Knife Fight

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-knife-fight?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-knife-fight/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-map-modifier](https://img.shields.io/github/issues/Kandru/cs2-knife-fight)](https://github.com/Kandru/cs2-knife-fight/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

A simple plug-in that allows players to have a knife fight when only one player from each team is alive. This works standalone as well with our Panorama Vote Manager plug-in. This integration will automatically start a vote between the two players to ask for a knife fight. Otherwise a command can be used to start a knife fight.

HINT: currently NOT READY

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-knife-fight/releases/).
2. Move the "KnifeFight" folder to the `/addons/counterstrikesharp/plugins/` directory.
3. Download and install the [CS2 Panorama Vote Manager](https://github.com/Kandru/cs2-panorama-vote-manager).
4. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically. To automate updates please use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/).


## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/KnifeFight/KnifeFight.json`.

```json

```

## Commands

TODO

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-knife-fight.git
```

Go to the project directory

```bash
  cd cs2-knife-fight
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

Additionally add the dependencies (if not added already for the panorama-vote-manager):

```bash
git submodule add https://github.com/Kandru/cs2-panorama-vote-manager.git
git commit -m "added panorama-vote-manager as a submodule"
git push
```

## FAQ

TODO

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
- [@jmgraeffe](https://www.github.com/jmgraeffe)

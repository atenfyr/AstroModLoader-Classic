# AstroModIntegrator Classic
Integrates Astroneer mods based on their metadata.json files in order to avoid mod conflict.

## Features
This is a fork of the original C# AstroTechies AstroModIntegrator software. It has been updated for the newest versions of the game (as of December 2024). It includes:
- support for metadata schema versions 1 and 2
- integrator operations: persistent_actors, mission_trailheads, linked_actor_components, item_list_entries, biome_placement_modifiers
- mod mismatch verification with servers at runtime
- ~70% average integration time decrease compared to [astro_modloader](https://github.com/AstroTechies/astro_modloader)
- [custom Blueprint API](https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator/BlueprintAssets)

## Licensing
AstroModIntegrator Classic is licensed under the MIT license, which can be found in [the LICENSE.md file.](https://github.com/atenfyr/AstroModIntegrator/blob/master/LICENSE.md) In addition, necessary licenses for the third-party material used in this software can be found in [the NOTICE.md file.](https://github.com/atenfyr/AstroModIntegrator/blob/master/NOTICE.md)

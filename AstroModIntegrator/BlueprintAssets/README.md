# AstroModIntegrator Classic Blueprint API
To use the AstroModIntegrator Classic blueprint API, first install the [AstroTechies ModdingKit](https://github.com/AstroTechies/ModdingKit), delete the Content/Integrator directory, and replace it with the Content/Integrator directory provided here in this directory. 

You may refer to the IntegratorAPI blueprint function library to find methods that can be used when AstroModIntegrator Classic was used by the client. Methods supplied by this library are not compatible with the Rust astro_modloader. It is possible to retrieve integrator information and a list of installed mods in a way that is compatible with both AstroModIntegrator Classic and astro_modloader. Some sample blueprint code is provided below with the AstroModIntegrator Classic blueprint API installed.

## Methods

### Get Integrator Statics
![](https://i.imgur.com/jBXQ5jM.png)

This code allows you to retrieve basic integrator information. If the user is using AstroModIntegrator Classic, you can simply use the "Get Integrator Statics" API node instead of this. You must reference `IntegratorStatics_BP` in the construct node. AstroModIntegrator Classic will return a version like "Classic 1.6.2.0", while astro_modloader will return a version like "0.1.12". "Refuse Mismatched Connections" is meaningless in astro_modloader as this feature is unimplemented in that mod loader, but it does carry meaning with AstroModIntegrator Classic.

### Get All Mods
![](https://i.imgur.com/peQ6Cq3.png)

This code allows you to retrieve a list of mods. If the user is using AstroModIntegrator Classic, you can simply use the "Get All Mods" API node instead of this. This code is fully functional with both mod loaders, and the Mod struct has the same fields in both mod loaders, although "Sync" and "IsOptional" carry no real meaning in astro_modloader.

## ModConfig

In AMLC v1.8.2.0+, it is possible to create custom configuration menus for users to use when interfacing with your mod. Custom configuration menus should be created as widgets inheriting from UserWidgetBlueprintDesignable in the Unreal Editor. The root element of the custom widget should be a VerticalBox containing Astroneer UI component widgets (GameMenuEntryDoubleText, GameMenuEntrySlider, GameMenuEntryCheckbox, etc.).

You may use the ModConfigExample widget located at Content/Integrator/ModConfig/ModConfigExample as a reference. This widget contains example implementations for a slider, a checkbox, and a simple option select, along with reference implementations for saving and loading configuration values from disk. In-game, this widget appears as follows:

![](https://i.imgur.com/IVoleBX.png)

![](https://i.imgur.com/HzONNEA.png)

Please note that the current view within the editor for these component widgets is not accurate, so widgets should be tested in-game.

Once you have created your widget, add an entry to your metadata.json within the "integrator" field containing the following text (adjusted for your mod's specific path):

```
"path_to_mod_config": "/Game/Mods/path/to/your/ModConfig"
```

Then, cook and package your mod, and select your mod under the Mod Options tab in-game to view the widget. To access the user's selected configuration variables, it is recommended to use the "Load Game from Slot" method within your blueprint of choice, just like the "Load" method does in ModConfigExample.

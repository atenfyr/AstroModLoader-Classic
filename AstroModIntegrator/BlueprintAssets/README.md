## AstroModIntegrator Classic Blueprint API
To use the AstroModIntegrator Classic blueprint API, first install the [AstroTechies ModdingKit](https://github.com/AstroTechies/ModdingKit), delete the Content/Integrator directory, and replace it with the Content/Integrator directory provided here in this directory. 

You may refer to the IntegratorAPI blueprint function library to find methods that can be used when AstroModIntegrator Classic was used by the client. Methods supplied by this library are not compatible with the Rust astro_modloader. It is possible to retrieve integrator information and a list of installed mods in a way that is compatible with both AstroModIntegrator Classic and astro_modloader. Some sample blueprint code is provided below with the AstroModIntegrator Classic blueprint API installed.

### Get Integrator Statics
![](https://i.imgur.com/jBXQ5jM.png)

This code allows you to retrieve basic integrator information. If the user is using AstroModIntegrator Classic, you can simply use the "Get Integrator Statics" API node instead of this. You must reference `IntegratorStatics_BP` in the construct node. AstroModIntegrator Classic will return a version like "Classic 1.6.2.0", while astro_modloader will return a version like "0.1.12". "Refuse Mismatched Connections" is meaningless in astro_modloader as this feature is unimplemented in that mod loader, but it does carry meaning with AstroModIntegrator Classic.

### Get All Mods
![](https://i.imgur.com/peQ6Cq3.png)

This code allows you to retrieve a list of mods. If the user is using AstroModIntegrator Classic, you can simply use the "Get All Mods" API node instead of this. This code is fully functional with both mod loaders, and the Mod struct has the same fields in both mod loaders, although "Sync" and "IsOptional" carry no real meaning in astro_modloader.

# MycoModConfig

Adds a Mods tab to the settings menu with every mod's config.

## Requirements

- [BepInEx 5](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/)
- [MycoMenuLib](https://thunderstore.io/c/mycopunk/p/funlennysub/MycoMenuLib/)
- [MonoDetour](https://thunderstore.io/c/mycopunk/p/MonoDetour/MonoDetour_BepInEx_5/)

## Features

- Per mod configuration tab
- Nothing is saved until you save
- Each option has small buttons next to its name:
  - **↺** revert to saved value
  - **wrench** reset to default
- Passwords and other secret values are shown as dots while typing, with a Show/Hide button

## For mod authors

```csharp
using BepInEx.Configuration;

// Boolean binds become a toggle
mute = Config.Bind("Audio", "Mute in missions", false, "Silence mission chatter");

// Numbers are sliders, with an optional range 
volume = Config.Bind("Audio", "Volume", 1f,
    new ConfigDescription("Overall volume", new AcceptableValueRange<float>(0f, 1f)));

// Enums and acceptable value lists become a "dropdown"
mode = Config.Bind("Gameplay", "Mode", Mode.Normal, "Pick a mode");
quality = Config.Bind("Video", "Quality", 2,
    new ConfigDescription("", new AcceptableValueList<int>(0, 1, 2, 3)));

// Text field, shown as dots with a Show/Hide button
apiKey = Config.Bind("Network", "Api Key", "",
    new ConfigDescription("Secret", null, "Password"));

// Hidden from the Mods tab
debug = Config.Bind("Debug", "MigrationDone", false,
    new ConfigDescription("", null, "Hidden"));

// Same effect via the Tags class
cache = Config.Bind("Debug", "CacheVersion", 1,
    new ConfigDescription("", null, new Tags { Browsable = false }));

// Nicer display name
tips = Config.Bind("Gameplay", "showStartupTips", true,
    new ConfigDescription("", null, new Tags { DispName = "Show tips" }));

class Tags
{
    public string DispName;
    public bool Browsable = true;
}
```

## Known issues

- Controller support is very bad in the current release, you can use it but you have to fight your way through
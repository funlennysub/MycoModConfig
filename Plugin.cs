using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using MonoDetour;
using MycoMenuLib;

namespace MycoModConfig;

[MycoMod(null, ModFlags.IsClientSide)]
[BepInDependency("dev.funlennysub.MycoMenuLib")]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;
    internal static ConfigEntry<bool> ReverseStripScroll;

    private void Awake()
    {
        Logger = base.Logger;

        ReverseStripScroll = Config.Bind("Interface", "Reverse Scroll", false,
            "Flip the horizontal scrolling direction of the mod card strip.");

        MenuEscape.RegisterPendingBlocker(() => ModsLeaveGuard.TryBlock(delegate { }));

        InitAllHooks();
    }

    private static void InitAllHooks()
    {
        MonoDetourManager.InvokeHookInitializers(Assembly.GetExecutingAssembly());
        Logger.LogInfo("Init'd all hooks");
    }
}
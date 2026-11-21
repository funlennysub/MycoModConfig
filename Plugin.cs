using System.Reflection;
using BepInEx;
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

    private void Awake()
    {
        Logger = base.Logger;

        MenuEscape.RegisterPendingBlocker(() => ModsLeaveGuard.TryBlock(delegate { }));

        InitAllHooks();
    }

    private static void InitAllHooks()
    {
        MonoDetourManager.InvokeHookInitializers(Assembly.GetExecutingAssembly());
        Logger.LogInfo("Init'd all hooks");
    }
}
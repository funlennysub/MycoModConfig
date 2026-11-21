using Md.PlayerOptions;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;

namespace MycoModConfig;

[MonoDetourTargets(typeof(PlayerOptions), GenerateControlFlowVariants = true)]
internal static class PlayerOptionsHooks
{
    [MonoDetourHookInitialize]
    internal static void InitHooks()
    {
        Start.Postfix(ModSettings.CreateOptions);

        SetSection.ControlFlowPrefix(SetSectionPrefix);
        TryClose.ControlFlowPrefix(TryClosePrefix);
    }

    private static ReturnFlow SetSectionPrefix(PlayerOptions self, ref int index)
    {
        var target = index;

        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.SetSection(target)))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }

    private static ReturnFlow TryClosePrefix(PlayerOptions self, ref bool returnValue)
    {
        if (!ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.CloseSettings(self)))
            return ReturnFlow.None;

        returnValue = false;
        return ReturnFlow.SkipOriginal;
    }
}
using System;
using Md.Menu;
using Md.WindowSystem;
using MonoDetour;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using UnityEngine;
using Update = Md.StartMenu.Update;

namespace MycoModConfig;

internal static class ModsLeaveGuard
{
    private static bool _bypassed;

    public static bool TryBlock(Action action)
    {
        if (_bypassed) return false;

        var view = ModsView.BlockingView;
        if (view == null) return false;
        if (ModsConfirmPopup.IsOpen) return true;

        view.PromptLeave(action);
        return true;
    }

    public static void Run(Action action)
    {
        if (action == null) return;

        _bypassed = true;
        try
        {
            action();
        }
        finally
        {
            _bypassed = false;
        }
    }

    public static void CloseSettings(PlayerOptions options)
    {
        var system = options != null ? options.WindowSystem : null;
        if (system == null) return;

        var count = system.Count;
        var closed = false;

        Run(() => closed = system.CloseTop());

        if (closed && count > 1 && system.Count > 0)
            return;

        if (Menu.Instance != null)
            Run(() => Menu.Instance.Close());
    }
}

[MonoDetourTargets(typeof(StartMenu), GenerateControlFlowVariants = true)]
internal static class StartMenuHooks
{
    [MonoDetourHookInitialize]
    internal static void InitHooks()
    {
        Update.ControlFlowPrefix(UpdatePrefix);
    }

    private static ReturnFlow UpdatePrefix(StartMenu self)
    {
        if (self.settingsWindow is not PlayerOptions { sections: { } sections })
            return ReturnFlow.None;

        var mods = Array.Find(sections, s => s != null && s.name == "Mods");
        if (mods == null || !mods.gameObject.activeInHierarchy)
            return ReturnFlow.None;

        self.lastYInput = PlayerInput.Device == PlayerInput.DeviceType.KeyboardMouse
            ? PlayerInput.Controls.Menu.Move.ReadValue<Vector2>().y
            : 0f;

        return ReturnFlow.None;
    }
}

[MonoDetourTargets(typeof(WindowSystem), GenerateControlFlowVariants = true)]
internal static class WindowSystemHooks
{
    [MonoDetourHookInitialize]
    internal static void InitHooks()
    {
        OpenWindow.ControlFlowPrefix(OpenWindowPrefix);
        OpenOverlayWindow.ControlFlowPrefix(OpenOverlayWindowPrefix);
        CloseWindow.ControlFlowPrefix(CloseWindowPrefix);
        CloseTop.ControlFlowPrefix(CloseTopPrefix);

        // CloseAll loops on CloseTop until the stack empties; blocking CloseTop alone would hang that loop.
        CloseAll.ControlFlowPrefix(CloseAllPrefix);
    }

    private static ReturnFlow OpenWindowPrefix(WindowSystem self, ref Window window)
    {
        var target = window;

        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.OpenWindow(target)))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }

    private static ReturnFlow OpenOverlayWindowPrefix(WindowSystem self, ref Window window)
    {
        var target = window;

        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.OpenOverlayWindow(target)))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }

    private static ReturnFlow CloseWindowPrefix(WindowSystem self, ref Window window)
    {
        var target = window;

        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.CloseWindow(target)))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }

    private static ReturnFlow CloseTopPrefix(WindowSystem self, ref bool returnValue)
    {
        if (!ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.CloseTop())))
            return ReturnFlow.None;

        returnValue = false;
        return ReturnFlow.SkipOriginal;
    }

    private static ReturnFlow CloseAllPrefix(WindowSystem self, ref bool forceClose)
    {
        var force = forceClose;

        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.CloseAll(force)))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }
}

[MonoDetourTargets(typeof(Menu), GenerateControlFlowVariants = true)]
internal static class MenuHooks
{
    [MonoDetourHookInitialize]
    internal static void InitHooks()
    {
        Close.ControlFlowPrefix(ClosePrefix);
    }

    private static ReturnFlow ClosePrefix(Menu self)
    {
        return ModsLeaveGuard.TryBlock(() => ModsLeaveGuard.Run(() => self.Close()))
            ? ReturnFlow.SkipOriginal
            : ReturnFlow.None;
    }
}
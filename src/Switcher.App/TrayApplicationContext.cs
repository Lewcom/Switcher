using System.Drawing;
using Switcher.App.Services;

namespace Switcher.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly HotkeyService _hotkeyService;

    public TrayApplicationContext()
    {
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();

        var menu = new ContextMenuStrip();
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Text = "Switcher (v1 skeleton)",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _hotkeyService = new HotkeyService(Keys.L, control: true, alt: true);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyService.IsRegistered)
        {
            _notifyIcon.ShowBalloonTip(
                2000,
                "Switcher",
                "Hotkey Ctrl+Alt+L is busy or unavailable.",
                ToolTipIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        _notifyIcon.ShowBalloonTip(
            1000,
            "Switcher",
            "Hotkey pressed: Ctrl+Alt+L",
            ToolTipIcon.Info);
    }
}

using System.Drawing;

namespace Switcher.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;

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
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}

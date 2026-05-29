using System.Drawing;
using Switcher.App.Services;
using Switcher.Core;

namespace Switcher.App;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly HotkeyService _hotkeyService;
    private readonly LayoutConverter _layoutConverter;
    private readonly TextCaptureService _textCaptureService;
    private readonly TextInjector _textInjector;

    public TrayApplicationContext()
    {
        try
        {
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => ExitThread();

            var menu = new ContextMenuStrip();
            menu.Items.Add(exitItem);

            _notifyIcon = new NotifyIcon
            {
                Text = "Switcher",
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };

            _layoutConverter = new LayoutConverter();
            _textCaptureService = new TextCaptureService();
            _textInjector = new TextInjector();

            _hotkeyService = new HotkeyService(Keys.L, control: true, alt: true);
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;

            if (!_hotkeyService.IsRegistered)
            {
                AppLogger.Error("Hotkey Ctrl+Alt+L is busy or unavailable.");
                _notifyIcon.ShowBalloonTip(
                    2000,
                    "Switcher",
                    "Hotkey Ctrl+Alt+L is busy or unavailable.",
                    ToolTipIcon.Warning);
            }
            else
            {
                AppLogger.Info("Hotkey Ctrl+Alt+L registered successfully.");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Tray application initialization failed.", ex);
            throw;
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
        try
        {
            var selectedText = _textCaptureService.TryGetSelectedText();
            if (!string.IsNullOrEmpty(selectedText))
            {
                var converted = _layoutConverter.Convert(selectedText);
                _textInjector.ReplaceSelection(converted);
                AppLogger.Info("Converted currently selected text.");
                return;
            }

            var lastWord = _textCaptureService.TryGetLastWordBySelection();
            if (!string.IsNullOrEmpty(lastWord))
            {
                var converted = _layoutConverter.Convert(lastWord);
                _textInjector.ReplaceSelection(converted);
                AppLogger.Info("Converted previous word near caret.");
                return;
            }

            AppLogger.Info("Hotkey pressed but nothing to convert.");
            _notifyIcon.ShowBalloonTip(1200, "Switcher", "Nothing to convert.", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Hotkey processing failed.", ex);
        }
    }
}

using System.Drawing;
using System.Runtime.InteropServices;
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

    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var targetWindow = GetForegroundWindow();
        try
        {
            AppLogger.Step(operationId, "hotkey_start", "combo=Ctrl+Alt+L");
            KeyboardStateService.WaitForModifierRelease();
            await Task.Delay(40);
            KeyboardStateService.NormalizeAfterHotkey();
            if (targetWindow != IntPtr.Zero)
            {
                SetForegroundWindow(targetWindow);
            }

            var selectedText = _textCaptureService.TryGetSelectedText(operationId, targetWindow);
            if (!string.IsNullOrEmpty(selectedText))
            {
                var converted = _layoutConverter.Convert(selectedText);
                AppLogger.Step(
                    operationId,
                    "convert_selected",
                    $"in_len={selectedText.Length} out_len={converted.Length} out_preview=\"{AppLogger.Preview(converted)}\"");

                var replaced = _textInjector.ReplaceSelection(converted, operationId, targetWindow);
                AppLogger.Step(operationId, "done", $"path=selected replaced={replaced}");
                return;
            }

            var lastWord = _textCaptureService.TryGetLastWordBySelection(operationId, targetWindow);
            if (!string.IsNullOrEmpty(lastWord))
            {
                var converted = _layoutConverter.Convert(lastWord);
                AppLogger.Step(
                    operationId,
                    "convert_last_word",
                    $"in_len={lastWord.Length} out_len={converted.Length} out_preview=\"{AppLogger.Preview(converted)}\"");

                var replaced = _textInjector.ReplaceSelection(converted, operationId, targetWindow);
                AppLogger.Step(operationId, "done", $"path=last_word replaced={replaced}");
                return;
            }

            AppLogger.Step(operationId, "done", "path=none reason=nothing_to_convert");
            _notifyIcon.ShowBalloonTip(1200, "Switcher", "Nothing to convert.", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Hotkey processing failed for op={operationId}.", ex);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}

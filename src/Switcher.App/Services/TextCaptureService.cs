using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal sealed class TextCaptureService
{
    private const uint WmCopy = 0x0301;
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    public string? TryGetSelectedText(string operationId, IntPtr targetWindow)
    {
        return CaptureCopiedText(prepareSelection: null, operationId, "capture_selected", targetWindow);
    }

    public string? TryGetLastWordBySelection(string operationId, IntPtr targetWindow)
    {
        var captured = CaptureCopiedText(prepareSelection: SelectPreviousWord, operationId, "capture_last_word", targetWindow);
        if (captured is null)
        {
            CollapseSelectionToCaret();
            AppLogger.Step(operationId, "capture_last_word", "result=null collapsed_selection=true");
        }

        return captured;
    }

    private static string? CaptureCopiedText(Action? prepareSelection, string operationId, string stepName, IntPtr targetWindow)
    {
        IDataObject? previousClipboard = null;
        var sentinel = "__sw_capture_" + Guid.NewGuid().ToString("N");
        try
        {
            KeyboardStateService.NormalizeAfterHotkey();
            FocusTargetWindow(targetWindow);
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetText(sentinel);

            prepareSelection?.Invoke();
            Thread.Sleep(40);

            if (TryCaptureFromClipboard(sentinel, operationId, stepName, targetWindow))
            {
                return Clipboard.GetText();
            }

            var finalText = Clipboard.ContainsText() ? Clipboard.GetText() : "<non-text>";
            AppLogger.Step(
                operationId,
                stepName,
                $"result=null reason=clipboard_timeout clipboard_preview=\"{AppLogger.Preview(finalText)}\"");
            return null;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"{stepName} failed for op={operationId}", ex);
            return null;
        }
        finally
        {
            if (previousClipboard is not null)
            {
                try
                {
                    Clipboard.SetDataObject(previousClipboard, true);
                }
                catch
                {
                    // Best effort restore only.
                }
            }
        }
    }

    private static bool TryCaptureFromClipboard(string sentinel, string operationId, string stepName, IntPtr targetWindow)
    {
        FocusTargetWindow(targetWindow);
        // Strategy 1: SendInput Ctrl+Insert
        SendCopyViaInput(useInsert: true);
        if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
        {
            AppLogger.Step(operationId, stepName, "copy_strategy=sendinput_ctrl_insert");
            return true;
        }

        FocusTargetWindow(targetWindow);
        // Strategy 2: SendInput Ctrl+C
        SendCopyViaInput(useInsert: false);
        if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
        {
            AppLogger.Step(operationId, stepName, "copy_strategy=sendinput_ctrl_c");
            return true;
        }

        // Strategy 3: WM_COPY to foreground window
        var foreground = GetForegroundWindow();
        if (foreground != IntPtr.Zero)
        {
            SendMessage(foreground, WmCopy, IntPtr.Zero, IntPtr.Zero);
            if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
            {
                AppLogger.Step(operationId, stepName, "copy_strategy=wm_copy_foreground");
                return true;
            }
        }

        return false;
    }

    private static bool WaitForClipboardTextDifferentFrom(string sentinel, int timeoutMs)
    {
        var started = Environment.TickCount;
        while (Environment.TickCount - started < timeoutMs)
        {
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text) && !string.Equals(text, sentinel, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            Thread.Sleep(35);
        }

        return false;
    }

    private static void SendCopyViaInput(bool useInsert)
    {
        var key = useInsert ? Keys.Insert : Keys.C;
        var inputs = new[]
        {
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: true),
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void CollapseSelectionToCaret()
    {
        SendKeys.SendWait("{RIGHT}");
    }

    private static void SelectPreviousWord()
    {
        SendKeys.SendWait("^+{LEFT}");
    }

    private static void FocusTargetWindow(IntPtr targetWindow)
    {
        if (targetWindow != IntPtr.Zero)
        {
            SetForegroundWindow(targetWindow);
            Thread.Sleep(15);
        }
    }

    private static INPUT CreateVirtualKeyInput(Keys key, bool keyUp)
    {
        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = (ushort)key,
                    dwFlags = keyUp ? KeyeventfKeyup : 0
                }
            }
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}

using System.Runtime.InteropServices;
using System.Text;

namespace Switcher.App.Services;

internal sealed class TextCaptureService
{
    private const uint WmCopy = 0x0301;
    private const uint WmCut = 0x0300;
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
        var focused = GetFocusedHandle(targetWindow);
        foreach (var handle in EnumerateWindowChain(focused, targetWindow))
        {
            SendMessage(handle, WmCopy, IntPtr.Zero, IntPtr.Zero);
            if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 450))
            {
                AppLogger.Step(
                    operationId,
                    stepName,
                    $"copy_strategy=wm_copy_chain handle=0x{handle.ToInt64():X} class={GetWindowClassName(handle)}");
                return true;
            }
        }

        FocusTargetWindow(targetWindow);
        // Strategy 1: SendInput Ctrl+Insert
        SendModifiedKeyViaInput(Keys.ControlKey, Keys.Insert);
        if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
        {
            AppLogger.Step(operationId, stepName, "copy_strategy=sendinput_ctrl_insert");
            return true;
        }

        FocusTargetWindow(targetWindow);
        // Strategy 2: SendInput Ctrl+C
        SendModifiedKeyViaInput(Keys.ControlKey, Keys.C);
        if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
        {
            AppLogger.Step(operationId, stepName, "copy_strategy=sendinput_ctrl_c");
            return true;
        }

        FocusTargetWindow(targetWindow);
        // Strategy 3: SendInput Ctrl+X (cut fallback when copy is blocked)
        SendModifiedKeyViaInput(Keys.ControlKey, Keys.X);
        if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
        {
            AppLogger.Info($"Cut fallback used for op={operationId} ({stepName}).");
            AppLogger.Step(operationId, stepName, "copy_strategy=sendinput_ctrl_x");
            return true;
        }

        foreach (var handle in EnumerateWindowChain(focused, targetWindow))
        {
            SendMessage(handle, WmCut, IntPtr.Zero, IntPtr.Zero);
            if (WaitForClipboardTextDifferentFrom(sentinel, timeoutMs: 500))
            {
                AppLogger.Step(
                    operationId,
                    stepName,
                    $"copy_strategy=wm_cut_chain handle=0x{handle.ToInt64():X} class={GetWindowClassName(handle)}");
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

    private static void SendModifiedKeyViaInput(Keys modifier, Keys key)
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(modifier, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: true),
            CreateVirtualKeyInput(modifier, keyUp: true)
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

    private static IntPtr GetFocusedHandle(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        _ = GetWindowThreadProcessId(targetWindow, out _);
        var threadId = GetWindowThreadProcessId(targetWindow, out _);
        var info = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
        if (threadId != 0 && GetGUIThreadInfo(threadId, ref info))
        {
            return info.hwndFocus;
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<IntPtr> EnumerateWindowChain(IntPtr focused, IntPtr targetWindow)
    {
        var seen = new HashSet<IntPtr>();
        var current = focused;
        while (current != IntPtr.Zero && seen.Add(current))
        {
            yield return current;
            if (current == targetWindow)
            {
                yield break;
            }

            current = GetParent(current);
        }

        if (targetWindow != IntPtr.Zero && seen.Add(targetWindow))
        {
            yield return targetWindow;
        }
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var buffer = new StringBuilder(256);
        return GetClassName(hWnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : "<unknown>";
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
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

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

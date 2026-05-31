using System.Runtime.InteropServices;
using System.Text;

namespace Switcher.App.Services;

internal sealed class TextInjector
{
    private const uint WmPaste = 0x0302;
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const uint KeyeventfUnicode = 0x0004;

    public bool ReplaceSelection(string replacement, string operationId, IntPtr targetWindow)
    {
        if (replacement is null)
        {
            throw new ArgumentNullException(nameof(replacement));
        }

        FocusTargetWindow(targetWindow);

        if (TryPasteViaFocusedWindow(replacement, targetWindow))
        {
            AppLogger.Step(
                operationId,
                "inject_wm_paste",
                $"result=ok length={replacement.Length} preview=\"{AppLogger.Preview(replacement)}\"");
            return true;
        }

        if (TryPasteViaClipboard(replacement))
        {
            AppLogger.Step(
                operationId,
                "inject_clipboard",
                $"result=ok length={replacement.Length} preview=\"{AppLogger.Preview(replacement)}\"");
            return true;
        }

        var unicodeResult = TryTypeUnicode(replacement);
        AppLogger.Step(
            operationId,
            "inject_unicode",
            unicodeResult
                ? $"result=ok length={replacement.Length} preview=\"{AppLogger.Preview(replacement)}\""
                : "result=fail");
        return unicodeResult;
    }

    private static void FocusTargetWindow(IntPtr targetWindow)
    {
        if (targetWindow != IntPtr.Zero)
        {
            SetForegroundWindow(targetWindow);
            Thread.Sleep(15);
        }
    }

    private static bool TryTypeUnicode(string text)
    {
        if (text.Length == 0)
        {
            return true;
        }

        var inputs = new INPUT[text.Length * 2];
        var index = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value > ushort.MaxValue)
            {
                return false;
            }

            var codePoint = (ushort)rune.Value;

            inputs[index++] = new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = codePoint,
                        dwFlags = KeyeventfUnicode
                    }
                }
            };

            inputs[index++] = new INPUT
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = codePoint,
                        dwFlags = KeyeventfUnicode | KeyeventfKeyup
                    }
                }
            };
        }

        var sent = SendInput((uint)index, inputs, Marshal.SizeOf<INPUT>());
        return sent == index;
    }

    private static bool TryPasteViaClipboard(string text)
    {
        IDataObject? previousClipboard = null;
        try
        {
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetDataObject(text, true);
            Thread.Sleep(20);
            SendKeys.SendWait("^v");
            Thread.Sleep(20);
            return true;
        }
        catch
        {
            return false;
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

    private static bool TryPasteViaFocusedWindow(string text, IntPtr targetWindow)
    {
        IDataObject? previousClipboard = null;
        try
        {
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetDataObject(text, true);
            Thread.Sleep(15);

            var focused = GetFocusedHandle(targetWindow);
            var sent = false;
            foreach (var handle in EnumerateWindowChain(focused, targetWindow))
            {
                SendMessage(handle, WmPaste, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(12);
                sent = true;
            }

            return sent;
        }
        catch
        {
            return false;
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

    private static IntPtr GetFocusedHandle(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

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

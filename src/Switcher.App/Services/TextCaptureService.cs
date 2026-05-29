using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal sealed class TextCaptureService
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    public string? TryGetSelectedText()
    {
        return CaptureCopiedText(prepareSelection: null);
    }

    public string? TryGetLastWordBySelection()
    {
        var captured = CaptureCopiedText(prepareSelection: SelectPreviousWord);
        if (captured is null)
        {
            CollapseSelectionToCaret();
        }

        return captured;
    }

    private static string? CaptureCopiedText(Action? prepareSelection)
    {
        IDataObject? previousClipboard = null;
        var initialClipboardSeq = GetClipboardSequenceNumber();
        try
        {
            KeyboardStateService.NormalizeAfterHotkey();
            previousClipboard = Clipboard.GetDataObject();

            prepareSelection?.Invoke();
            Thread.Sleep(30);

            SendCtrlKey(Keys.C);
            if (!WaitForClipboardChange(initialClipboardSeq, 250))
            {
                return null;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }

                Thread.Sleep(30);
            }

            return null;
        }
        catch
        {
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

    private static bool WaitForClipboardChange(uint initialSequence, int timeoutMs)
    {
        var started = Environment.TickCount;
        while (Environment.TickCount - started < timeoutMs)
        {
            if (GetClipboardSequenceNumber() != initialSequence)
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    private static void CollapseSelectionToCaret()
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(Keys.Right, keyUp: false),
            CreateVirtualKeyInput(Keys.Right, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SelectPreviousWord()
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: false),
            CreateVirtualKeyInput(Keys.ShiftKey, keyUp: false),
            CreateVirtualKeyInput(Keys.Left, keyUp: false),
            CreateVirtualKeyInput(Keys.Left, keyUp: true),
            CreateVirtualKeyInput(Keys.ShiftKey, keyUp: true),
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendCtrlKey(Keys key)
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: true),
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
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
    private static extern uint GetClipboardSequenceNumber();

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

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
        return CaptureCopiedText(prepareSelection: SelectPreviousWord);
    }

    private static string? CaptureCopiedText(Action? prepareSelection)
    {
        IDataObject? previousClipboard = null;
        try
        {
            KeyboardStateService.NormalizeAfterHotkey();
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.Clear();

            prepareSelection?.Invoke();
            Thread.Sleep(30);

            SendCtrlKey(Keys.C);
            Thread.Sleep(80);

            if (!Clipboard.ContainsText())
            {
                return null;
            }

            var text = Clipboard.GetText();
            return string.IsNullOrEmpty(text) ? null : text;
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

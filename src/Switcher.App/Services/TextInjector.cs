using System.Runtime.InteropServices;
using System.Text;

namespace Switcher.App.Services;

internal sealed class TextInjector
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;
    private const uint KeyeventfUnicode = 0x0004;

    public bool ReplaceSelection(string replacement)
    {
        if (replacement is null)
        {
            throw new ArgumentNullException(nameof(replacement));
        }

        KeyboardStateService.NormalizeAfterHotkey();

        if (TryTypeUnicode(replacement))
        {
            return true;
        }

        return TryPasteViaClipboard(replacement);
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
            Clipboard.SetText(text);
            SendCtrlKey(Keys.V);
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

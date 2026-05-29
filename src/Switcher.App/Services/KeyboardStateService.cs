using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal static class KeyboardStateService
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    public static void NormalizeAfterHotkey()
    {
        // Release possible stuck modifiers from global hotkey and close menu mode.
        SendKeyUp(Keys.ControlKey);
        SendKeyUp(Keys.Menu);
        SendKeyUp(Keys.ShiftKey);
        SendKeyUp(Keys.LWin);
        SendKeyUp(Keys.RWin);
        SendPress(Keys.Escape);
    }

    private static void SendPress(Keys key)
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(key, keyUp: false),
            CreateVirtualKeyInput(key, keyUp: true)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendKeyUp(Keys key)
    {
        var inputs = new[] { CreateVirtualKeyInput(key, keyUp: true) };
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

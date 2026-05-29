using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal sealed class TextCaptureService
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    public string? TryGetSelectedText(string operationId)
    {
        return CaptureCopiedText(prepareSelection: null, operationId, "capture_selected");
    }

    public string? TryGetLastWordBySelection(string operationId)
    {
        var captured = CaptureCopiedText(prepareSelection: SelectPreviousWord, operationId, "capture_last_word");
        if (captured is null)
        {
            CollapseSelectionToCaret();
            AppLogger.Step(operationId, "capture_last_word", "result=null collapsed_selection=true");
        }

        return captured;
    }

    private static string? CaptureCopiedText(Action? prepareSelection, string operationId, string stepName)
    {
        IDataObject? previousClipboard = null;
        var sentinel = "__sw_capture_" + Guid.NewGuid().ToString("N");
        try
        {
            KeyboardStateService.NormalizeAfterHotkey();
            previousClipboard = Clipboard.GetDataObject();
            Clipboard.SetText(sentinel);

            prepareSelection?.Invoke();
            Thread.Sleep(40);

            SendCtrlCViaInput();
            Thread.Sleep(60);

            for (var attempt = 0; attempt < 8; attempt++)
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text) && !string.Equals(text, sentinel, StringComparison.Ordinal))
                    {
                        AppLogger.Step(
                            operationId,
                            stepName,
                            $"result=ok length={text.Length} preview=\"{AppLogger.Preview(text)}\"");
                        return text;
                    }
                }

                Thread.Sleep(35);
            }

            AppLogger.Step(operationId, stepName, "result=null reason=clipboard_timeout");
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

    private static void SendCtrlCViaInput()
    {
        var inputs = new[]
        {
            CreateVirtualKeyInput(Keys.ControlKey, keyUp: false),
            CreateVirtualKeyInput(Keys.C, keyUp: false),
            CreateVirtualKeyInput(Keys.C, keyUp: true),
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

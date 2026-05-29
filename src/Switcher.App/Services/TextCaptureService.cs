using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal sealed class TextCaptureService
{
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
                AppLogger.Step(operationId, stepName, "result=null reason=clipboard_timeout");
                return null;
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        AppLogger.Step(
                            operationId,
                            stepName,
                            $"result=ok length={text.Length} preview=\"{AppLogger.Preview(text)}\"");
                        return text;
                    }
                }

                Thread.Sleep(30);
            }

            AppLogger.Step(operationId, stepName, "result=null reason=no_text_after_retries");
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
        SendKeys.SendWait("{RIGHT}");
    }

    private static void SelectPreviousWord()
    {
        SendKeys.SendWait("^+{LEFT}");
    }

    private static void SendCtrlKey(Keys key)
    {
        if (key == Keys.C)
        {
            SendKeys.SendWait("^c");
        }
        else
        {
            SendKeys.SendWait("^" + key.ToString().ToLowerInvariant());
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}

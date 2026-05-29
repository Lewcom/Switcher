using System.Runtime.InteropServices;

namespace Switcher.App.Services;

internal sealed class HotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModNoRepeat = 0x4000;

    private readonly HotkeyMessageWindow _window;
    private readonly int _hotkeyId;
    private bool _isRegistered;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public HotkeyService(Keys key, bool control, bool alt, int hotkeyId = 1)
    {
        _window = new HotkeyMessageWindow(this);
        _hotkeyId = hotkeyId;

        var modifiers = 0u;
        if (control)
        {
            modifiers |= ModControl;
        }

        if (alt)
        {
            modifiers |= ModAlt;
        }

        _isRegistered = RegisterHotKey(_window.Handle, _hotkeyId, modifiers | ModNoRepeat, (uint)key);
    }

    public bool IsRegistered => _isRegistered;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_isRegistered)
        {
            UnregisterHotKey(_window.Handle, _hotkeyId);
            _isRegistered = false;
        }

        _window.ReleaseHandle();
        _disposed = true;
    }

    private void OnWindowMessage(ref Message message)
    {
        if (message.Msg == WmHotkey && message.WParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class HotkeyMessageWindow : NativeWindow
    {
        private readonly HotkeyService _owner;

        public HotkeyMessageWindow(HotkeyService owner)
        {
            _owner = owner;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            _owner.OnWindowMessage(ref m);
            base.WndProc(ref m);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

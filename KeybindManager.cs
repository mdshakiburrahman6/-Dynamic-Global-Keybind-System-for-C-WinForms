using Guna.UI2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Xml;

public class KeybindManager : IDisposable
{
    // ─── Singleton ───────────────────────────────────────────────────────────────
    private static KeybindManager _instance;
    public static KeybindManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new KeybindManager();
            return _instance;
        }
    }

    // ─── P/Invoke ────────────────────────────────────────────────────────────────
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc fn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    // ─── Internal Entry ──────────────────────────────────────────────────────────
    private class KeybindEntry
    {
        public string Id { get; }
        public Guna2Button BindButton { get; }
        public Keys BoundKey { get; set; }
        public Guna2ToggleSwitch Toggle { get; }
        public Action Callback { get; }

        public KeybindEntry(string id, Guna2Button btn, Keys defaultKey, Guna2ToggleSwitch toggle, Action callback)
        {
            Id = id;
            BindButton = btn;
            BoundKey = defaultKey;
            Toggle = toggle;
            Callback = callback;
        }
    }

    // ─── State ───────────────────────────────────────────────────────────────────
    private readonly List<KeybindEntry> _entries = new List<KeybindEntry>();
    private KeybindEntry _waitingEntry = null;
    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc _hookProc;
    private bool _initialized = false;
    private string _saveFile = "keybinds.json";

    public string SaveFilePath
    {
        get { return _saveFile; }
        set { _saveFile = value; }
    }

    // ─── Initialize ──────────────────────────────────────────────────────────────
    public void Initialize()
    {
        if (_initialized) return;
        _hookProc = HookCallback;
        _hookId = SetHook(_hookProc);
        Application.ApplicationExit += Application_ApplicationExit;
        _initialized = true;
    }

    private void Application_ApplicationExit(object sender, EventArgs e)
    {
        Dispose();
    }

    // ─── Register ────────────────────────────────────────────────────────────────
    public void Register(
        Guna2Button bindButton,
        string id,
        Keys defaultKey = Keys.None,
        Guna2ToggleSwitch toggle = null,
        Action callback = null)
    {
        var entry = new KeybindEntry(id, bindButton, defaultKey, toggle, callback);
        _entries.Add(entry);

        // capture entry for lambda
        var captured = entry;
        bindButton.Click += (s, e) => StartBinding(captured);

        RefreshButtonText(entry);
    }

    // ─── Start Binding ───────────────────────────────────────────────────────────
    public void StartBinding(Guna2Button button)
    {
        var entry = _entries.Find(e => e.BindButton == button);
        if (entry != null) StartBinding(entry);
    }

    private void StartBinding(KeybindEntry entry)
    {
        if (_waitingEntry != null)
            RefreshButtonText(_waitingEntry);

        _waitingEntry = entry;
        entry.BindButton.Text = "Press key...";
    }

    // ─── Hook Callback ───────────────────────────────────────────────────────────
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var key = (Keys)Marshal.ReadInt32(lParam);

            if (_waitingEntry != null)
            {
                _waitingEntry.BoundKey = (key == Keys.Escape) ? Keys.None : key;
                RefreshButtonText(_waitingEntry);
                _waitingEntry = null;
                SaveBindings();
            }
            else
            {
                foreach (var entry in _entries)
                {
                    if (entry.BoundKey != Keys.None && entry.BoundKey == key)
                    {
                        if (entry.Toggle != null)
                            entry.Toggle.Checked = !entry.Toggle.Checked;

                        if (entry.Callback != null)
                            entry.Callback();
                    }
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    // ─── Save / Load ─────────────────────────────────────────────────────────────
    public void SaveBindings()
    {
        var dict = new Dictionary<string, string>();
        foreach (var e in _entries)
            dict[e.Id] = e.BoundKey.ToString();

        File.WriteAllText(_saveFile, JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented));

    }

    public void LoadBindings()
    {
        if (!File.Exists(_saveFile)) return;

        try
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_saveFile));
            foreach (var e in _entries)
            {
                string keyStr;
                if (dict.TryGetValue(e.Id, out keyStr))
                {
                    Keys key;
                    if (Enum.TryParse<Keys>(keyStr, out key))
                    {
                        e.BoundKey = key;
                        RefreshButtonText(e);
                    }
                }
            }
        }
        catch { }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────
    private void RefreshButtonText(KeybindEntry entry)
    {
        entry.BindButton.Text = entry.BoundKey == Keys.None ? "None" : entry.BoundKey.ToString();
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // ─── Dispose ─────────────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }
}
# KeybindManager — Dynamic Global Keybind System for C# WinForms

A lightweight, fully dynamic global keyboard hook manager for **C# WinForms** projects using **Guna UI2** controls. Supports multiple independent keybinds, toggle switches, custom callbacks, and persistent save/load — all from a single singleton class.

---

## Features

- **Global keyboard hook** — works even when the app is not in focus
- **Dynamic registration** — add as many keybinds as you want with one line each
- **Toggle support** — automatically flips a `Guna2ToggleSwitch` on keypress
- **Callback support** — invoke any custom method on keypress
- **Rebind at runtime** — user clicks a button, presses a key, done
- **Escape to unbind** — press `Escape` while rebinding to clear the key
- **Auto save/load** — bindings persist across sessions via `keybinds.json`
- **C# 7.3 compatible** — works with .NET Framework 4.x projects

---

## Requirements

| Requirement | Version |
|---|---|
| .NET Framework | 4.x (4.7.2 / 4.8 recommended) |
| C# | 7.3+ |
| Guna UI2 | Any version with `Guna2Button`, `Guna2ToggleSwitch` |
| Newtonsoft.Json | Any recent version (via NuGet) |

### Install Newtonsoft.Json

In **Package Manager Console**:

```
Install-Package Newtonsoft.Json
```

Or via **NuGet Package Manager** → search `Newtonsoft.Json` → Install.

---

## Setup

### 1. Add `KeybindManager.cs` to your project

Copy `KeybindManager.cs` into your project. No namespace changes needed — it works at the project root level.

### 2. Add using (in your Form)

```csharp
using System.Windows.Forms;
```

No extra using needed for `KeybindManager` itself — it's a global class.

---

## Usage

### Basic Setup in `Form_Load`

```csharp
private void Main_Load(object sender, EventArgs e)
{
    // Step 1: Initialize the global hook (call once)
    KeybindManager.Instance.Initialize();

    // Step 2: Register your keybinds
    KeybindManager.Instance.Register(
        bindButton: btnAimbotBind,      // Guna2Button — user clicks to rebind
        id: "Aimbot",                   // Unique ID for save/load
        defaultKey: Keys.F1,            // Starting key (Keys.None = unbound)
        toggle: toggleAimbot            // Guna2ToggleSwitch to flip on keypress
    );

    // Step 3: Load saved bindings from last session
    KeybindManager.Instance.LoadBindings();
}
```

### Save on Close

```csharp
private void Main_FormClosing(object sender, FormClosingEventArgs e)
{
    KeybindManager.Instance.SaveBindings();
}
```

---

## Register() Parameter Reference

```csharp
KeybindManager.Instance.Register(
    bindButton: Guna2Button,        // Required — button user clicks to rebind
    id: string,                     // Required — unique ID (used for save/load)
    defaultKey: Keys,               // Optional — default: Keys.None (unbound)
    toggle: Guna2ToggleSwitch,      // Optional — toggle to flip on keypress
    callback: Action                // Optional — method to call on keypress
);
```

---

## Examples

### Toggle only

```csharp
KeybindManager.Instance.Register(
    bindButton: btnESPBind,
    id: "ESP",
    defaultKey: Keys.F2,
    toggle: toggleESP
);
```

### Custom callback only (no toggle)

```csharp
KeybindManager.Instance.Register(
    bindButton: btnSpeedBind,
    id: "Speed",
    defaultKey: Keys.F3,
    toggle: null,
    callback: () => ToggleSpeed()
);
```

### Both toggle + callback

```csharp
KeybindManager.Instance.Register(
    bindButton: btnNoRecoilBind,
    id: "NoRecoil",
    defaultKey: Keys.F4,
    toggle: toggleNoRecoil,
    callback: () => OnNoRecoilChanged()
);
```

### Trigger an existing button click event

```csharp
KeybindManager.Instance.Register(
    bindButton: btnAimbotEnable,
    id: "AimbotEnable",
    defaultKey: Keys.None,
    toggle: null,
    callback: () => aimBtn_Click(sender: this, e: EventArgs.Empty)
);
```

### Unbound by default (user sets their own key)

```csharp
KeybindManager.Instance.Register(
    bindButton: btnCustomBind,
    id: "CustomFeature",
    toggle: toggleCustom
    // No defaultKey → starts as "None"
);
```

---

## How Rebinding Works (User Flow)

1. User clicks the bind button (e.g. `btnAimbotBind`)
2. Button text changes to **"Press key..."**
3. User presses any key → that key gets assigned, button shows the key name
4. If user presses **Escape** → key is cleared, button shows **"None"**
5. Binding is automatically saved to `keybinds.json`

---

## Save File

Bindings are stored in `keybinds.json` next to the `.exe` by default.

```json
{
  "Aimbot": "F1",
  "ESP": "F2",
  "Speed": "None",
  "NoRecoil": "F4"
}
```

### Custom save path

```csharp
KeybindManager.Instance.SaveFilePath = "config/keybinds.json";
// Set this BEFORE calling Register() or LoadBindings()
```

---

## Full Example

```csharp
private void Main_Load(object sender, EventArgs e)
{
    KeybindManager.Instance.Initialize();

    KeybindManager.Instance.Register(
        bindButton: btnAimbotEnable,
        id: "AimbotEnable",
        defaultKey: Keys.None,
        toggle: null,
        callback: () => aimBtn_Click(sender: this, e: EventArgs.Empty)
    );

    KeybindManager.Instance.Register(
        bindButton: btnAimbotOnOff,
        id: "AimbotOnOff",
        defaultKey: Keys.None,
        toggle: null,
        callback: () => btnAimbotOnOff_Click(sender: this, e: EventArgs.Empty)
    );

    KeybindManager.Instance.Register(
        bindButton: btnESPBind,
        id: "ESP",
        defaultKey: Keys.F3,
        toggle: toggleESP
    );

    KeybindManager.Instance.LoadBindings();
}

private void Main_FormClosing(object sender, FormClosingEventArgs e)
{
    KeybindManager.Instance.SaveBindings();
}
```

---

## Adding a New Feature Later

Just add one more `Register()` call — no changes needed inside `KeybindManager.cs`:

```csharp
KeybindManager.Instance.Register(
    bindButton: btnNewFeatureBind,
    id: "NewFeature",
    defaultKey: Keys.F5,
    toggle: toggleNewFeature
);
```

---

## Notes

- The global hook is set up once via `Initialize()` — calling it multiple times is safe (it checks internally)
- If `keybinds.json` is corrupted or missing, the app falls back to default keys silently
- The hook is automatically released on `Application.ApplicationExit`
- Multiple keybinds can share the same key — all of them will trigger simultaneously

---

## License

MIT — free to use and modify.
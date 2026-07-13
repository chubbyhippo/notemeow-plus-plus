# Building and installing the Notepad++ plugin

`Notemeow.Plugin` is the Notepad++ side of notemeow++: a native DLL
(compiled with .NET NativeAOT — no runtime to install) that subclasses both
Scintilla views, feeds keystrokes to the engine in `../core`, and paints
the meow state into the status bar.

## Build

Requirements, Windows side: a .NET 10 SDK and the Visual Studio C++ build
tools (NativeAOT links with MSVC).

```
dotnet publish plugin\Notemeow.Plugin -r win-x64 -c Release
```

The native DLL lands at
`plugin\Notemeow.Plugin\bin\Release\net10.0\win-x64\publish\Notemeow.dll`.

From WSL on the same machine, the repo root's `./setup.sh plugin` runs
this same publish through the Windows .NET SDK (scoop's `dotnet-sdk` or
`C:\Program Files\dotnet` — the requirements above still apply), and
`./setup.sh plugin install` copies the result into a scoop or standard
`plugins\Notemeow\`. Close Notepad++ before installing; a running
instance locks the loaded DLL.

## Install

Notepad++ loads a plugin from a folder **named exactly like its DLL** under
`plugins`:

```
<Notepad++ dir>\plugins\Notemeow\Notemeow.dll
```

For a standard install that's `C:\Program Files\Notepad++\plugins\`
(needs admin); for a scoop install it's
`%USERPROFILE%\scoop\apps\notepadplusplus\current\plugins\` (no admin
needed). Create the `Notemeow` folder, copy `Notemeow.dll` in, restart
Notepad++. You should see **Plugins → notemeow++** in the menu and
`meow: NORMAL` in the status bar; press `i` to type, `ESC` to get back to
NORMAL.

## What v0.1 wires up

- the full NORMAL layout from the bundled `.notemeowrc`
  (`~/.notemeowrc` = `C:\Users\<you>\.notemeowrc` overrides it; *Plugins →
  notemeow++ → Edit / Reload .notemeowrc*)
- INSERT passthrough — in INSERT the engine refuses keys and Scintilla
  types them, IME composition included
- `ESC` back to NORMAL (and collapsing extra selections)
- the **Alt chord layer** in NORMAL: `Alt+f/b` word, `Alt+a/e` sentence,
  `Alt+u/l/c` case, `Alt+d` kill-word, `Alt+Shift+,` / `Alt+Shift+.`
  buffer start/end — consumed before the menu bar sees them, so `Alt+f`
  moves instead of opening the File menu (INSERT gives them back)
- the mode shown two ways, the Emacs+meow way: a **block caret** in
  NORMAL / MOTION / KEYPAD and a **line caret** in INSERT
  (`SCI_SETCARETSTYLE`), plus `MEOW NORMAL` / `MEOW INSERT` in the status
  bar's typing-mode field (the one that normally reads INS/OVR, so it
  doesn't fight the language label)
- **grab + beacon**: `G` grabs the selection and the region is boxed with a
  Scintilla indicator; select inside it (`w`, `x`, `f`…) and a caret lands
  on every match (native multi-selection); `ESC` collapses
- **avy jumps**: `S` (goto-char-timer) — type a few chars, matches box in
  yellow, pause 0.25 s, then jump labels appear; type the label to jump.
  `Q` (goto-line) labels the visible lines. Labels paint in a layered
  overlay window over the editor; a single match jumps without a label
- per-buffer state, the cheatsheet, one-undo edit groups,
  system-clipboard kill ring
- offsets converted between Scintilla's UTF-8 bytes and the engine's
  UTF-16 characters, so multi-byte text (Thai and friends) selects and
  edits correctly

## Known limits (v0.1)

- **The Ctrl chords** (`Ctrl+f/b/n/p/a/e`) are implemented, but Notepad++'s
  own accelerators (Find, brace-match, New, Print, Select All) grab those
  keys before the editor sees them. Clear the conflicting entries in
  *Settings → Shortcut Mapper* and the chords come alive; keep them and
  Notepad++ wins — your call, both are fine.
- **The avy label overlay is load-verified, not eye-verified** — the DLL
  loads and the engine is fully tested, but the label window's on-screen
  painting (position, color-key transparency, DPI) hasn't been watched
  live. If labels don't appear, `S`/`Q` still jump whenever your input
  narrows to a single match (that path needs no label), and the yellow
  match boxes still show where candidates are.
- **No input prompts yet** — `X` (goto line via `meow-goto-line`), `v`
  (visit regexp), and `Q`'s digit→line-number shortcut ask for text; the
  plugin has no minibuffer, so those show a status-bar note and stop.
  `Q`'s label jump and `S` need no prompt. A small modal input box unlocks
  the three text paths; it's the next UI piece.
- Which-key and expand hints are not drawn yet; the keypad works blind
  (`SPC ?` cheatsheet, `SPC /` describe still answer).
- `<action>(...)` targets take a numeric Notepad++ menu command id for
  now (the values from `menuCmdID.h`), e.g.
  `map <leader>xk <action>(41003)` for File → Close. The plugin's own
  named ids work too: `notemeow.editRc` / `notemeow.reloadRc`, bundled as
  `SPC c m` / `SPC c M`; reload saves a dirty `~/.notemeowrc` tab first.

## License

GPL-3.0-or-later. See [LICENSE](../LICENSE) for the full text.

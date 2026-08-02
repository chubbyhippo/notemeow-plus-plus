# notemeow++

[meow](https://github.com/meow-edit/meow)-style modal editing for Notepad++ —
meow's suggested **QWERTY layout** as a native modal engine, with no vim
emulation in the middle. Select first, then act.

## Where this stands

| Layer | State |
|---|---|
| The engine | complete for everything it tests — 339 behavior specs, each cross-checked against meow's source, headless in a fraction of a second |
| The Notepad++ plugin around it | young — see [plugin/BUILD.md](plugin/BUILD.md) for what is wired |

| The engine covers | |
|---|---|
| The meow layout, selection model, editing commands | ✓ |
| The Emacs point-motion and word commands (`forward-char` through `end-of-buffer`, `upcase/downcase/capitalize-word`, `kill-word`) with meow's extend-an-active-selection behavior | ✓ |
| The `.notemeowrc` two-layer keymap, the keypad and repeat engines, things and blocks, search, grab and beacon | ✓ |
| The avy jumps — `S` (goto-char-timer) and `Q` (goto-line), a native port of avy 0.5.0's label tree, timer and subdivision | ✓ |
| The platform decision logic the plugin maps — windmove between the two Notepad++ views, the panel double-`ESC` pairing, tree motions for the MOTION map, the attach policy | ✓ |

| Still ahead | |
|---|---|
| Live keystroke verification of the newest UI surfaces | see [plugin/BUILD.md](plugin/BUILD.md) |
| Intercepting Notepad++'s panels and trees, so MOTION mode has somewhere to run | see [plugin/BUILD.md](plugin/BUILD.md) |

## States

| State | What |
|---|---|
| **NORMAL** | keys are commands; you start here |
| **INSERT** | keys type text — `i a c I A` enter, `ESC` leaves |
| **KEYPAD** | `SPC` as the leader — digit arguments, the `?` cheatsheet and `/` describe-key all work |
| **MOTION** | meow's reduced state, present in the engine, unused on this platform so far |
| **BEACON** | grab with `G`, select inside it, a selection lands on every similar range; `ESC` collapses |

### The bundled keypad

Whole groups of Notepad++ menu commands by their `menuCmdID.h` names. Add your
own `map <leader>...` lines with `IDM_*` names or raw numeric ids.

| Sequence | Does |
|---|---|
| `SPC x s` | save |
| `SPC w i` | zoom in — `i i i` keeps zooming, a repeat group |
| `SPC . c` | walk the change history |
| `SPC b m` | toggle a bookmark |
| `SPC a u` | focus the Function List |
| `SPC c m` | open `~/.notemeowrc`, seeding it from the bundled copy first |
| `SPC c M` | reload it, saving a dirty rc tab first |

## The layout

### Moving and selecting

| Key | Does |
|---|---|
| `h j k l` | move — a char-selection survives, any other selection is cancelled |
| `H J K L` | extend a char selection |
| `w` / `W` | mark the word / symbol at point, and push it to the search ring, so `n` finds the next occurrence |
| `e` / `E`, `b` / `B` | next / previous word or symbol; after a `w` they extend rather than replace (meow's `(expand . word)` rule) |
| `x` | select the line — repeat or press digits to take more |
| `Q` / `X` | go to a line |
| `f` / `t` | find / till a character |
| `o` / `O` | select the enclosing block / to its end |
| `m` | select the join region |
| `,` `.` `[` `]` | inner / bounds / begin / end of a *thing* |
| `;` | reverse the selection |
| `z` | pop back to the previous selection |
| `v` | visit a regexp |
| `n` | continue the search — backward when the selection is reversed |
| `1`-`9`, `0` | expand by N units (`0` = 10); a count when nothing is selected |
| `-` | negative argument |

| Thing | Char |
|---|---|
| round / square / curly | `r` / `s` / `c` |
| string / symbol | `g` / `e` |
| window / buffer | `w` / `b` |
| paragraph / line / visual line | `p` / `l` / `v` |
| defun / sentence | `d` / `.` |

### Editing

| Key | Does |
|---|---|
| `i` / `a` | insert at the selection's start / end |
| `I` / `A` | open a line above / below |
| `c` | change |
| `s` | kill (cut) |
| `d` / `D` | delete forward / backward |
| `y` | save (copy) |
| `p` | yank (paste) |
| `r` | replace the selection with the clipboard |
| `u` | undo |
| `'` | repeat the last command, counts and all — `'` after `2fa` finds the second `a` again |
| `g` | cancel |
| `q` | close the tab |
| `ESC` | back to NORMAL |

## Emacs chords

| Behavior | Value |
|---|---|
| Bound to | the real Emacs point motions, not meow commands |
| With no selection | the chord moves the caret |
| With one active | it extends it, anchored exactly like meow's own `H J K L` expand — `w` then `Ctrl+f Ctrl+f` grows the marked word one character at a time |
| `;` (reverse) | flips which end subsequent chords grow from |

| Chord | Command |
|---|---|
| `Ctrl+f` / `Ctrl+b` | `forward/backward-char` |
| `Ctrl+n` / `Ctrl+p` | `next/previous-line` |
| `Ctrl+a` / `Ctrl+e` | `move-beginning/end-of-line` |
| `Alt+f` / `Alt+b` | `forward/backward-word` |
| `Alt+a` / `Alt+e` | `backward/forward-sentence` |
| `Alt+Shift+,` / `Alt+Shift+.` | `beginning/end-of-buffer` — a count lands N/10 of the way in, snapping to the next line start |
| `Alt+Shift+[` / `Alt+Shift+]` | `backward/forward-paragraph` — blank-line-delimited; forward lands on the separator line, backward on the paragraph start |
| `Alt+u` / `Alt+l` / `Alt+c` | `upcase/downcase/capitalize-word` — `-` then the chord reaches back without moving the caret |
| `Alt+d` | `kill-word` into the clipboard |
| `Ctrl+/`, `Ctrl+_` | undo |
| `Ctrl+d` | delete |
| `Ctrl+k` / `Ctrl+w` | kill |
| `Alt+w` | save |
| `Ctrl+y` | yank |
| `Ctrl+g` | cancel |
| `Alt+m` | back-to-indentation |
| `Ctrl+o` | open-line |
| `Alt+\`, `Alt+Space` | whitespace |
| `Alt+^` | join |

| Fact | Value |
|---|---|
| Config | rc lines, one `cmap` each, in either spelling — `cmap C-f forward-char` or `cmap control F forward-char` |
| Give a key back to Notepad++ | bind the chord to `ignore` |
| Active in | NORMAL and MOTION; they yield to Notepad++'s own keys in INSERT |

## No keys in code

| Layer | What |
|---|---|
| Bundled [`.notemeowrc`](core/Notemeow.Core/Resources/.notemeowrc) | the entire layout — one `nmap <key> <meow-command>` line per key, so the file is the authoritative reference |
| `~/.notemeowrc` | overrides it entry by entry |

## Build & test

Toolchain pinned in `mise.toml` (.NET SDK 10).

```bash
cd notemeow-plus-plus
./setup.sh                  # lint, run the suite, build the DLL, and install it into Notepad++
./setup.sh --core-only      # the lint gates and the behavior suite (no Notepad++ needed)
./setup.sh --lint-only      # only the analyzer and code-style gates
./setup.sh --build-only     # build the DLL via the Windows .NET SDK, install nothing
./setup.sh --skip-build     # install the already-built DLL
```

| Gate | Detail |
|---|---|
| Every path lints first | `dotnet format --verify-no-changes --severity info` over all three projects, plus a managed build of the adapter |
| `Directory.Build.props` | `TreatWarningsAsErrors`, `EnableNETAnalyzers`, `AnalysisLevel latest`, `EnforceCodeStyleInBuild` — pure defaults, no rule-config file, no baseline, no suppressions, zero findings |

| Host note | Value |
|---|---|
| Machines without libicu | `mise.toml` sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; `apt install libicu-dev` is the clean fix |
| Building natively on Windows | [plugin/BUILD.md](plugin/BUILD.md) |
| From WSL | `./setup.sh --build-only` drives the same build through the Windows .NET SDK, so the Windows-side requirements still apply — a .NET 10 SDK and the Visual Studio C++ build tools |

## ~/.notemeowrc

| Item | Value |
|---|---|
| Path | `~/.notemeowrc` on the machine Notepad++ runs on — `C:\Users\<you>\.notemeowrc` |
| Format | vimrc-style; lines it does not understand are ignored rather than fatal |

| Line | Meaning |
|---|---|
| `" text` or `# text` | comment (also at the end of a line) |
| `nmap <key> <meow-command>` | bind a NORMAL key to a named meow command, e.g. `nmap n meow-mark-word` — this is how you remap the layout itself |
| `nmap <key> <action>(IDM_FILE_SAVE)` | NORMAL key runs a Notepad++ menu command — any `menuCmdID.h` name or raw numeric id |
| `nmap <key> <keys>` | NORMAL key replays a meow key sequence, e.g. `nmap Z ,b` |
| `nnoremap` / `noremap` | like `nmap`/`map`, but the replayed keys resolve through the bundled defaults, ignoring your other mappings |
| `mmap` / `mnoremap` | the same target forms, for MOTION mode |
| `cmap` / `cnoremap` | the Emacs modifier-chord layer (see above) |
| `map <leader><seq> <target>` | keypad entry: `SPC` + sequence |
| `desc <leader><seq> <text>` | which-key label for an entry (exact seq) or a group (prefix) |
| `repeat <group> <key> <target>` | a tap-to-continue group |
| `set timeoutlen=300` | which-key delay in milliseconds |
| `set which-key` / `set nowhich-key` | which-key on/off (default on) |
| `set overlay-color=#2ECC71` | background of the avy / ace jump labels (`#RRGGBB`) |
| `set overlay-text-color=#ffffff` | the jump-label text color |
| `set expand-hint-color=#2B5DB2` | the `0`-`9` expand-hint box color |
| `set grab-color=#33CC33` | the grab / beacon highlight color |

| Item | Value |
|---|---|
| Key notation | plain printable characters, plus `<Space>` and `<lt>` |
| Reserved | keypad `0-9` (digit argument), `?` (cheatsheet), `/` (describe key); `SPC` is always the keypad key |
| Reach | only printable keys reach the modal engine — modifier chords go through `cmap` |

### Relayouting (Dvorak, Colemak, …)

The layout section of the bundled `.notemeowrc` IS the default keymap — an
`nmap` line per key, exactly like a `meow-normal-define-key` block in Emacs.

| Right-hand side | Effect |
|---|---|
| a known command name | binds it |
| `ignore` | disables the key |
| a misspelled `meow-*` name | reported as an error |
| anything else | replayed as keys |
| a key you do not mention | keeps its bundled binding |

### Semantics worth knowing

| Fact | Value |
|---|---|
| Repeat | mapped keys work with `'`; key-replay mappings are recursion-guarded — a self-referencing map stops at depth 8 with a hint |
| `repeat` | itself a bindable command, so even `'` can be reassigned |
| `repeat <group> <key> <target>` | after any binding whose target belongs to a group, the next keypress is looked up in that group first — a member key keeps the run alive, any other key ends it and keeps its normal meaning |

## Known deviations from meow

All deliberate, none accidental.

| Deviation | Detail |
|---|---|
| `U` (meow-undo-in-selection) | plain undo, gated on an active region — the host's undo stack cannot be scoped to a region |
| Beacon | native multiple selections instead of kmacro recording |
| The kill-ring | the system clipboard; `kill-line` does not append consecutive kills |
| Block/string/defun "things" | a text scan (same-line strings skipped), with a hook for the host to supply a smarter defun range |
| The avy jumps (`S` / `Q`) | a native port of avy 0.5.0's goto-char-timer and goto-line — same label keys (`a s d f g h j k l`), same label tree and subdivision, scoped to the visible area of the current editor; labels paint in an overlay |

## Hacking on it

Commands are data: every command registers under its meow name in
`Registry.cs`, and keys only ever resolve through rc bindings.

| Where | What |
|---|---|
| `Engine.cs` | the dispatcher: key → binding → command; repeat (`'`), rc-replay bookkeeping, ESC |
| `Motions.cs` | movement and the selections it creates: hjkl, words, lines, find/till, plus the fifteen Emacs chord motions |
| `Selections.cs` | the selection primitive (meow's expand/select model), reverse/cancel/pop, digit expand |
| `Search.cs` | meow-search / meow-visit and the shared regexp ring |
| `Structures.cs` / `Things.cs` | the char-thing table, blocks, join / what a "thing" is |
| `Grab.cs` | grab / swap / sync and the beacon reaction |
| `Avy.cs` | the `S`/`Q` jumps: label tree, subdivision, the goto-char timer, goto-line |
| `AttachPolicy.cs` / `ToolWindowEscape.cs` / `Windmove.cs` / `TreeMeow.cs` | the platform decision logic the plugin maps: where meow attaches, double-ESC pairing, two-view windmove, tree motions |
| `Edits.cs` | everything that mutates text, including the chord-layer case/kill commands |
| `Rc.cs` / `RcParser.cs` / `RcFileState.cs` | the two rc layers, the line syntax, the parse-hash reload check |
| `Keypad.cs` / `WhichKey.cs` / `Hints.cs` | the SPC leader, the popup rows, the digit-expand hint positions |
| `Ports.cs` | the seam: `IEditorPort` / `IClipboardPort` / `IUiPort` — the engine never touches an editor or OS API, which is why the suite runs in milliseconds |

| Item | Value |
|---|---|
| Specs | `core/Notemeow.Core.Tests`, given/whenKeys/then…, cross-checked against meow's source |
| A red spec means | "you changed meow's semantics", not "update the test" |
| Run | `./setup.sh --core-only` |

## License

GPL-3.0-or-later. See [LICENSE](LICENSE) for the full text.

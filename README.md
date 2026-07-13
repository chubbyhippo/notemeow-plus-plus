# notemeow++ — meow modal editing for Notepad++

If you love [meow](https://github.com/meow-edit/meow) in Emacs and sigh every
time you open Notepad++, this plugin is for you. It implements meow's
suggested **QWERTY layout** as a native modal editing engine — no vim
emulation in the middle. Just meow: select first, then act.

## Where this stands

The engine — the part that decides what every key does — is complete for
everything it tests: 120 behavior specs, every one cross-checked against
meow's own source, running headless in a third of a second. The Notepad++
plugin around it is young: check the notes in
[plugin/BUILD.md](plugin/BUILD.md) for what is wired and what is still
ahead.

What the engine covers today:

- the meow layout, selection model, and editing commands
- the **Emacs point-motion and word commands** (`forward-char` through
  `end-of-buffer`, `upcase/downcase/capitalize-word`, `kill-word`) with
  meow's extend-an-active-selection behavior
- the `.notemeowrc` two-layer keymap, the keypad and repeat engines, things
  and blocks, search, grab and beacon

Not yet: the avy jumps (`S` is unbound, `Q` is a plain goto-line for now)
and window/panel keys.

## What you get

The states you know from meow:

- **NORMAL** — keys are commands. You start here.
- **INSERT** — keys type text. `i a c I A` get you in, `ESC` gets you out.
- **KEYPAD** — `SPC` as the leader. Digit arguments, the `?` cheatsheet and
  `/` describe-key all work; the bundled command table starts empty — add
  your own `map <leader>...` lines with Notepad++ menu command ids.
- **MOTION** — meow's reduced state, present in the engine, unused on this
  platform so far.
- **BEACON** — grab a region with `G`, select something inside it, and a
  selection lands on every similar range. Edit them all at once; `ESC`
  collapses.

**Moving and selecting.** `h j k l` move (a char-selection survives
movement, any other selection is cancelled), and `H J K L` extend a char
selection. `w`/`W` mark the word/symbol at point — and push it to the search
ring, which is why `n` finds the next occurrence right afterwards. `e`/`E`
and `b`/`B` go to the next/previous word or symbol, and after a `w` they
*extend* the selection instead of replacing it (meow's `(expand . word)`
rule). `x` selects the line — repeat it or press digits to take more lines.
`Q`/`X` go to a line, `f`/`t` find/till a character, `o`/`O` select the
enclosing block / to its end, `m` selects the join region, and `,` `.` `[`
`]` select inner/bounds/begin/end of a *thing* (`r` round, `s` square, `c`
curly, `g` string, `e` symbol, `w` window, `b` buffer, `p` paragraph, `l`
line, `v` visual line, `d` defun, `.` sentence). `;` reverses the selection,
`z` pops back to the previous one, `v` visits a regexp, `n` continues the
search (backward when the selection is reversed). Digits expand the
selection by N units (`0` = 10) or act as a count when nothing is selected;
`-` is the negative argument.

**Editing.** `i`/`a` insert at the selection's start/end, `I`/`A` open a
line above/below, `c` change, `s` kill (cut), `d`/`D` delete
forward/backward, `y` save (copy), `p` yank (paste), `r` replace the
selection with the clipboard, `u` undo, `'` repeats the last command —
counts and all, so `'` after `2fa` finds the second `a` again. `g` cancels,
`q` closes the tab, `ESC` always brings you back to NORMAL.

**Emacs chords.** `Ctrl+f/b/n/p/a/e` and `Alt+f/b/a/e` are the real Emacs
point motions (`forward/backward-char`, `next/previous-line`,
`move-beginning/end-of-line`, `forward/backward-word`,
`backward/forward-sentence`), not meow commands: meow itself never rebinds
these chords — its state keymaps hold only single printable keys — and
since a meow selection is an active Emacs mark, the same point motion
stretches an already-active selection for free. notemeow++ ports that: with
no selection the chord just moves the caret, and with one active it extends
it, anchored exactly like meow's own `H J K L` expand — so `w` then
`Ctrl+f Ctrl+f` grows the marked word one character at a time, and `;`
flips which end subsequent chords grow from. `Alt+Shift+,` / `Alt+Shift+.`
are `beginning/end-of-buffer` (a count lands N/10 of the way in, snapping
to the next line start), `Alt+u` / `Alt+l` / `Alt+c` are
`upcase/downcase/capitalize-word` (a negative count — `-` then the chord —
reaches back without moving the caret), and `Alt+d` is `kill-word` (into
the clipboard). The chords answer in NORMAL and yield to Notepad++'s own
keys in INSERT.

And one idea borrowed straight from meow itself: **the plugin binds no keys
in code.** The entire layout lives in a
[`.notemeowrc`](core/Notemeow.Core/Resources/.notemeowrc) bundled inside
the plugin — one `nmap <key> <meow-command>` line per key, so the file
doubles as the authoritative reference — and a `~/.notemeowrc` in your home
directory overrides it entry by entry. Rebind anything; relayout
everything.

## Build & test

Toolchain pinned in `mise.toml` (.NET SDK 10):

```bash
cd notemeow-plus-plus
./setup.sh          # run the behavior suite (about 0.3 s)
```

On machines without libicu, `mise.toml` sets
`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` so the SDK runs anyway;
`apt install libicu-dev` is the clean fix. Building and installing the
Notepad++ plugin itself is covered in [plugin/BUILD.md](plugin/BUILD.md).

## ~/.notemeowrc — configuring everything

notemeow++ reads a vimrc-style file from your home directory:
`~/.notemeowrc` on the machine Notepad++ runs on
(`C:\Users\<you>\.notemeowrc`). Lines it doesn't understand are ignored
rather than fatal.

| Line | Meaning |
|---|---|
| `" text` or `# text` | comment (also at the end of a line) |
| `nmap <key> <meow-command>` | bind a NORMAL key to a named meow command, e.g. `nmap n meow-mark-word` — this is how you remap the layout itself |
| `nmap <key> <action>(command.id)` | NORMAL key runs a Notepad++ menu command |
| `nmap <key> <keys>` | NORMAL key replays a meow key sequence, e.g. `nmap Z ,b` |
| `nnoremap` / `noremap` | like `nmap`/`map`, but the replayed keys resolve through the bundled defaults, ignoring your other mappings |
| `mmap` / `mnoremap` | the same target forms, for MOTION mode |
| `map <leader><seq> <target>` | keypad entry: `SPC` + sequence |
| `desc <leader><seq> <text>` | which-key label for an entry (exact seq) or a group (prefix) |
| `set timeoutlen=300` | which-key delay in milliseconds |
| `set which-key` / `set nowhich-key` | which-key on/off (default on) |

Key notation: plain printable characters, plus `<Space>` and `<lt>`.
Reserved: keypad `0-9` (digit argument), `?` (cheatsheet), `/` (describe
key); `SPC` is always the keypad key. Only printable keys reach the modal
engine — modifier chords belong to the host (that's where the Emacs chords
live too).

**Relayouting (Dvorak, Colemak, …).** The layout section of the bundled
`.notemeowrc` IS the default keymap — an `nmap` line per key, exactly like
a `meow-normal-define-key` block in Emacs. A right-hand side that names a
known command binds it; `ignore` disables a key; a misspelled `meow-*` name
is reported as an error; anything else is replayed as keys. A key you don't
mention keeps its bundled binding.

**A few semantics worth knowing:**

- Mapped keys work with `'` (repeat), and key-replay mappings are
  recursion-guarded — a self-referencing map stops at depth 8 with a hint
  instead of freezing the editor.
- `repeat` is itself a bindable command, so even `'` can be reassigned.
- `repeat <group> <key> <target>` lines define tap-to-continue groups:
  after any binding whose target belongs to a group, the next keypress is
  looked up in that group first — a member key keeps the run alive, any
  other key ends it and keeps its normal meaning.

## Known deviations from meow

All deliberate, none accidental:

- `U` (meow-undo-in-selection) runs plain undo, gated on an active region —
  the host's undo stack cannot be scoped to a region.
- Beacon uses native multiple selections instead of kmacro recording.
- The kill-ring is the system clipboard; `kill-line` does not append
  consecutive kills.
- Block/string/defun "things" use a text scan (same-line strings skipped),
  with a hook for the host to supply a smarter defun range.
- The avy jumps are not in yet — `S` is unbound and `Q` goes to a plain
  goto-line prompt until they land.

## Hacking on it

The code keeps one rule from meow: commands are data. Every command
registers under its meow name in `Registry.cs`, and keys only ever resolve
through rc bindings.

| Where | What |
|---|---|
| `Engine.cs` | the dispatcher: key → binding → command; repeat (`'`), rc-replay bookkeeping, ESC |
| `Motions.cs` | movement and the selections it creates: hjkl, words, lines, find/till, plus the sixteen Emacs chord commands |
| `Selections.cs` | the selection primitive (meow's expand/select model), reverse/cancel/pop, digit expand |
| `Search.cs` | meow-search / meow-visit and the shared regexp ring |
| `Structures.cs` / `Things.cs` | the char-thing table, blocks, join / what a "thing" is |
| `Grab.cs` | grab / swap / sync and the beacon reaction |
| `Edits.cs` | everything that mutates text, including the chord-layer case/kill commands |
| `Rc.cs` / `RcParser.cs` / `RcFileState.cs` | the two rc layers, the line syntax, the parse-hash reload check |
| `Keypad.cs` / `WhichKey.cs` / `Hints.cs` | the SPC leader, the popup rows, the digit-expand hint positions |
| `Ports.cs` | the seam: `IEditorPort` / `IClipboardPort` / `IUiPort` — the engine never touches an editor or OS API, which is why the suite runs in milliseconds |

Behavior is pinned by the specs in `core/Notemeow.Core.Tests`
(given/whenKeys/then…), cross-checked against meow's source. Treat a red
spec as "you changed meow's semantics", not "update the test". Run them
with `./setup.sh`.

## License

GPL-3.0-or-later. See [LICENSE](LICENSE) for the full text.

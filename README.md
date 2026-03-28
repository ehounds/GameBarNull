# GameBarNull

A complete installation package that permanently and silently replaces the
Xbox Game Bar `ms-gamebar://` protocol handler. When any game or Windows
component triggers the scheme, the tiny handler receives it, does nothing,
and exits — no UI, no balloon, no "Get an app to open this" dialog.

## Package contents

```
GameBarNull\
├── dist\                    ← distribute this folder (or zip it)
│   ├── Setup.exe            ← double-click to install
│   ├── Uninstaller.exe      ← also registered in Add / Remove Programs
│   └── GameBarNull.exe      ← the actual silent protocol handler (3.5 KB)
├── src\
│   ├── Setup.cs             ← installer source
│   ├── Uninstaller.cs       ← uninstaller source
│   └── admin-manifest.xml   ← UAC requireAdministrator manifest
├── GameBarNull.cs           ← protocol handler source
├── Build.ps1                ← compiles all three exes (no SDK needed)
└── README.md
```

## Installing

1. Open the `dist\` folder.
2. Double-click **Setup.exe** — Windows will show a UAC prompt; click Yes.
3. The installer copies files to `%ProgramFiles%\GameBarNull\`, registers the
   `ms-gamebar` protocol handler, and adds an entry to Add / Remove Programs.

Setup backs up the original registry handler before overwriting it.

## Uninstalling

**Option A — Add / Remove Programs (recommended)**

Open *Settings → Apps* (or *Control Panel → Programs*), find **GameBarNull**,
and click Uninstall.

**Option B — Run Uninstaller.exe directly**

Run `%ProgramFiles%\GameBarNull\Uninstaller.exe` (or from `dist\` before
installation).

**Option C — Silent / scripted**

```
"%ProgramFiles%\GameBarNull\Uninstaller.exe" /silent
```

On uninstall the original `ms-gamebar` handler is restored (if one existed).
`Uninstaller.exe` itself is scheduled for deletion on the next restart
(Windows `MoveFileEx` / `PendingFileRenameOperations`).

## Rebuilding from source

No .NET SDK is needed — the compiler ships with Windows:

```powershell
.\Build.ps1
```

Output lands in `dist\`.

## How it works

Windows resolves `ms-gamebar://` URIs via
`HKLM\SOFTWARE\Classes\ms-gamebar\shell\open\command`.
Setup.exe points that key to `GameBarNull.exe "%1"`.
GameBarNull.exe starts, ignores the argument, and exits — total runtime ~1 ms,
zero visible effect on the user.

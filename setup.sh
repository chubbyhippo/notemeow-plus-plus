#!/usr/bin/env sh
# Copyright (C) 2026 Chubby Hippo
#
# This program is free software: you can redistribute it and/or modify it
# under the terms of the GNU General Public License as published by the Free
# Software Foundation, either version 3 of the License, or (at your option)
# any later version.
#
# This program is distributed in the hope that it will be useful, but WITHOUT
# ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
# FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
# more details.
#
# You should have received a copy of the GNU General Public License along
# with this program. If not, see <https://www.gnu.org/licenses/>.
#
# SPDX-License-Identifier: GPL-3.0-or-later

set -eu
cd "$(dirname "$0")"

published_dll="plugin/Notemeow.Plugin/bin/Release/net10.0/win-x64/publish/Notemeow.dll"

usage() {
  echo "usage: ./setup.sh                 run the engine behavior suite"
  echo "       ./setup.sh plugin          build the Notepad++ DLL (from WSL, via the Windows .NET SDK)"
  echo "       ./setup.sh plugin install  build, then copy the DLL into Notepad++'s plugins folder"
}

run_suite() {
  mise exec -- dotnet test core/Notemeow.Core.Tests
}

windows_home() {
  raw=$(/mnt/c/Windows/System32/cmd.exe /c "echo %USERPROFILE%" 2>/dev/null | tr -d '\r') || raw=""
  [ -n "$raw" ] || return 1
  wslpath "$raw"
}

windows_dotnet() {
  found=$(command -v dotnet.exe 2>/dev/null) || found=""
  if [ -n "$found" ]; then
    echo "$found"
    return 0
  fi
  home=$(windows_home) || home=""
  for exe in "$home/scoop/apps/dotnet-sdk/current/dotnet.exe" "/mnt/c/Program Files/dotnet/dotnet.exe"; do
    if [ -x "$exe" ]; then
      echo "$exe"
      return 0
    fi
  done
  return 1
}

notepadplusplus_plugins_dir() {
  home=$(windows_home) || home=""
  for dir in "$home/scoop/apps/notepadplusplus/current/plugins" "/mnt/c/Program Files/Notepad++/plugins"; do
    if [ -d "$dir" ]; then
      echo "$dir"
      return 0
    fi
  done
  return 1
}

build_plugin() {
  dotnet_exe=$(windows_dotnet) || {
    echo "error: no Windows .NET SDK reachable from this shell" >&2
    echo "the Notepad++ DLL is NativeAOT and must link on Windows: install a .NET 10 SDK there (scoop install dotnet-sdk) plus the Visual Studio C++ build tools, or build natively per plugin/BUILD.md" >&2
    exit 1
  }
  "$dotnet_exe" publish plugin/Notemeow.Plugin -r win-x64 -c Release
  echo "built $published_dll"
}

install_plugin() {
  plugins_dir=$(notepadplusplus_plugins_dir) || {
    echo "error: no Notepad++ found under scoop or Program Files; copy $published_dll into <Notepad++>/plugins/Notemeow/ yourself" >&2
    exit 1
  }
  if mkdir -p "$plugins_dir/Notemeow" && cp "$published_dll" "$plugins_dir/Notemeow/Notemeow.dll"; then
    echo "installed $plugins_dir/Notemeow/Notemeow.dll"
    echo "restart Notepad++ to load it"
  else
    echo "error: install failed; a running Notepad++ locks the loaded DLL, close it and rerun" >&2
    exit 1
  fi
}

case "${1:-}" in
  "") run_suite ;;
  plugin)
    case "${2:-}" in
      "") build_plugin ;;
      install) build_plugin; install_plugin ;;
      *) usage >&2; exit 1 ;;
    esac
    ;;
  help|-h|--help) usage ;;
  *) usage >&2; exit 1 ;;
esac

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
  echo "usage: ./setup.sh              run the suite, build the Notepad++ DLL, and install it"
  echo "       ./setup.sh --core-only  only the engine behavior suite (no Notepad++ needed)"
  echo "       ./setup.sh --build-only build the DLL, install nothing"
  echo "       ./setup.sh --skip-build install the already-built DLL"
  echo "       ./setup.sh -h           show this help and exit"
}

run_suite() {
  mise exec -- dotnet test core/Notemeow.Core.Tests
}

windows_home() {
  raw=$(/mnt/c/Windows/System32/cmd.exe /c "echo %USERPROFILE%" 2>/dev/null | tr -d '\r') || raw=""
  [ -n "$raw" ] || return 1
  wslpath "$raw"
}

dotnet_has_sdk() {
  [ -x "$1" ] && [ -n "$("$1" --list-sdks 2>/dev/null)" ]
}

windows_dotnet() {
  found=$(command -v dotnet.exe 2>/dev/null) || found=""
  home=$(windows_home) || home=""
  for exe in "$found" "$home/scoop/apps/dotnet-sdk/current/dotnet.exe" "/mnt/c/Program Files/dotnet/dotnet.exe"; do
    if dotnet_has_sdk "$exe"; then
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
  sdk_line=$("$dotnet_exe" --list-sdks 2>/dev/null | tr -d '\r' | tail -1)
  sdk_ver=${sdk_line%% *}
  sdk_base=${sdk_line#*\[}
  sdk_base=${sdk_base%\]*}
  DOTNET_ROOT="${sdk_base%\\sdk}" MSBuildSDKsPath="$sdk_base\\$sdk_ver\\Sdks" \
    WSLENV="${WSLENV:+$WSLENV:}DOTNET_ROOT:MSBuildSDKsPath" \
    "$dotnet_exe" publish plugin/Notemeow.Plugin -r win-x64 -c Release
  echo "built $published_dll"
}

install_plugin() {
  [ -f "$published_dll" ] || {
    echo "error: $published_dll not built — run without --skip-build" >&2
    exit 1
  }
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

core_only=0 do_build=1 do_install=1
while [ $# -gt 0 ]; do
  case "$1" in
    --core-only)  core_only=1 ;;
    --build-only) do_install=0 ;;
    --skip-build) do_build=0 ;;
    -h|--help)    usage; exit 0 ;;
    *) usage >&2; exit 2 ;;
  esac
  shift
done

run_suite

if [ "$core_only" -eq 1 ]; then
  exit 0
fi

if [ "$do_build" -eq 1 ]; then
  build_plugin
fi

if [ "$do_install" -eq 0 ]; then
  echo "install later with: ./setup.sh --skip-build"
  exit 0
fi

install_plugin

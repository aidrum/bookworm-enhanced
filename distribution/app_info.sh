# Shared by the build scripts and the Build workflow (sourced, not run). Run from the repository root.
#
#   port_version   prints the port's version: Directory.Build.props <Version>, plus VERSION_SUFFIX when
#                  set. CI sets it to ".<run number>" for automatic builds, so each one has a new version
#                  (0.1.0.17 for build 17), which AltStore / SideStore need to see an update; a version tag
#                  build leaves it empty, so its version is exactly <Version>.
#
# Also sets APP_NAME (display name), EXE (executable / file name prefix), APP_ID (bundle / application
# id) and VERSION (port_version). Bookworm is one universal app: there are no editions.

port_version() {
  local base
  base="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"
  echo "$base${VERSION_SUFFIX:-}"
}

APP_NAME="Bookworm"
EXE="Bookworm"
APP_ID="com.aidrum.bookworm"
VERSION="$(port_version)"

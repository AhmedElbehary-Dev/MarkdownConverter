#!/bin/bash
set -e

# This script builds the .deb package locally inside WSL's native filesystem
# to avoid NTFS permission issues with dpkg-deb.

BUILD_DIR="$HOME/deb-build"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Use the publish dir relative to the script (when run from packaging/linux)
PUBLISH_SRC="$SCRIPT_DIR/../../src/MarkdownConverter.Desktop/bin/Release/net10.0/linux-x64/publish"

if [ ! -d "$PUBLISH_SRC" ]; then
    echo "ERROR: Publish directory not found at: $PUBLISH_SRC"
    echo "Run 'dotnet publish MarkdownConverter.csproj -c Release -r linux-x64 --self-contained true' first."
    exit 1
fi

echo "==> Cleaning previous build..."
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/release"

echo "==> Copying publish output to WSL filesystem..."
cp -r "$PUBLISH_SRC" "$BUILD_DIR/publish"

echo "==> Copying packaging scripts..."
cp -r "$SCRIPT_DIR" "$BUILD_DIR/linux"
cp -r "$SCRIPT_DIR/../" "$BUILD_DIR/packaging" 2>/dev/null || true

# Patch the PUBLISH_DIR to point to our local copy (from linux/ -> one level up to deb-build/)
sed -i 's|PUBLISH_DIR=.*|PUBLISH_DIR="../publish"|' "$BUILD_DIR/linux/build-deb.sh"

# Also patch the release output path to use absolute path
sed -i "s|mv \"\$PACKAGE_NAME.deb\" ../../release/|mv \"\$PACKAGE_NAME.deb\" $BUILD_DIR/release/|" "$BUILD_DIR/linux/build-deb.sh"

echo "==> Building .deb package..."
cd "$BUILD_DIR/linux"
chmod +x build-deb.sh
./build-deb.sh

echo ""
echo "==> Copying .deb to Windows release folder..."
RELEASE_DIR="$SCRIPT_DIR/../../release"
mkdir -p "$RELEASE_DIR"
cp "$BUILD_DIR"/release/*.deb "$RELEASE_DIR/"

echo ""
echo "==> Done! .deb package(s):"
ls -la "$RELEASE_DIR"/*.deb

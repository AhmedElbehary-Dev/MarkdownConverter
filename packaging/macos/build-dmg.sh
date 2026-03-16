#!/bin/bash
set -e

# Configuration
APP_NAME="MarkdownConverter"
VERSION="2.0.3"
APP_BUNDLE="$APP_NAME.app"
PUBLISH_DIR="../../bin/Release/net10.0/osx-x64/publish"
OUTPUT_DIR="../../release"

echo "=== Building macOS App Bundle ==="

# Clean previous builds
rm -rf "$APP_BUNDLE"
rm -f "$OUTPUT_DIR/$APP_NAME.dmg"
mkdir -p "$OUTPUT_DIR"

# Create App Bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy Info.plist
cp Info.plist "$APP_BUNDLE/Contents/"

# Copy published files
echo "Copying published files from $PUBLISH_DIR..."
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory $PUBLISH_DIR not found!"
    exit 1
fi
cp -r "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"

# Fix executable permissions (important for macOS)
chmod +x "$APP_BUNDLE/Contents/MacOS/MarkdownConverter"

# Handle Icon (Convert PNG to ICNS using sips/iconutil if available)
ICON_SOURCE="../../src/MarkdownConverter.Desktop/Assets/md_converter.png"
if [ -f "$ICON_SOURCE" ]; then
    echo "Processing icon..."
    mkdir -p "$APP_NAME.iconset"
    sips -z 16 16     "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_16x16.png" > /dev/null
    sips -z 32 32     "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_16x16@2x.png" > /dev/null
    sips -z 32 32     "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_32x32.png" > /dev/null
    sips -z 64 64     "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_32x32@2x.png" > /dev/null
    sips -z 128 128   "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_128x128.png" > /dev/null
    sips -z 256 256   "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_128x128@2x.png" > /dev/null
    sips -z 256 256   "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_256x256.png" > /dev/null
    sips -z 512 512   "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_256x256@2x.png" > /dev/null
    sips -z 512 512   "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_512x512.png" > /dev/null
    sips -z 1024 1024 "$ICON_SOURCE" --out "$APP_NAME.iconset/icon_512x512@2x.png" > /dev/null
    
    iconutil -c icns "$APP_NAME.iconset"
    cp "$APP_NAME.icns" "$APP_BUNDLE/Contents/Resources/md_converter.icns"
    rm -rf "$APP_NAME.iconset" "$APP_NAME.icns"
fi

echo "=== Creating Disk Image (DMG) ==="
# We use hdiutil which is standard on macOS
# In a real CI environment, you might want more fancy DMG layouts, 
# but this is the functional equivalent of the .deb/.msi builds.
hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$APP_NAME.dmg"

mv "$APP_NAME.dmg" "$OUTPUT_DIR/"
rm -rf "$APP_BUNDLE"

echo "Done! macOS installer created at $OUTPUT_DIR/$APP_NAME.dmg"

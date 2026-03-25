#!/bin/bash
set -e

# Configuration
APP_NAME="MarkdownConverter"
VERSION="${1:-1.0.0}"
APP_BUNDLE="$APP_NAME.app"
PUBLISH_DIR="../../bin/Release/net10.0/osx-x64/publish"
OUTPUT_DIR="../../release"

echo "=== Building macOS App Bundle (Version $VERSION) ==="

# Clean previous builds
rm -rf "$APP_BUNDLE"
rm -f "$OUTPUT_DIR/$APP_NAME.dmg"
mkdir -p "$OUTPUT_DIR"

# Create App Bundle structure
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copy and update Info.plist
cp Info.plist "$APP_BUNDLE/Contents/"
# Strip 'v' prefix if present for Info.plist version strings
CLEAN_VERSION=$(echo $VERSION | sed 's/^v//')
sed -i '' "s/2.0.3/$CLEAN_VERSION/g" "$APP_BUNDLE/Contents/Info.plist"

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

# Codesigning Step (Optional but recommended for distribution)
if [ ! -z "$CODESIGN_IDENTITY" ]; then
    echo "Signing App Bundle with identity: $CODESIGN_IDENTITY"
    
    # 1. Sign any helper binaries or frameworks (if any exist)
    # We use --options runtime for Hardened Runtime (required for notarization)
    find "$APP_BUNDLE" -name "*.dylib" -or -name "*.so" | xargs codesign --force --options runtime --timestamp --sign "$CODESIGN_IDENTITY" || true
    
    # 2. Sign the main executable
    codesign --force --options runtime --timestamp --sign "$CODESIGN_IDENTITY" "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
    
    # 3. Sign the app bundle itself
    codesign --force --options runtime --timestamp --sign "$CODESIGN_IDENTITY" "$APP_BUNDLE"
    
    echo "Verifying signature..."
    codesign --verify --verbose "$APP_BUNDLE"
else
    echo "Warning: CODESIGN_IDENTITY not set. Skipping codesigning. App will be flagged as 'damaged' on other machines."
fi

# We use hdiutil which is standard on macOS
hdiutil create -volname "$APP_NAME" -srcfolder "$APP_BUNDLE" -ov -format UDZO "$APP_NAME.dmg"

mv "$APP_NAME.dmg" "$OUTPUT_DIR/"
rm -rf "$APP_BUNDLE"

echo "Done! macOS installer created at $OUTPUT_DIR/$APP_NAME.dmg"

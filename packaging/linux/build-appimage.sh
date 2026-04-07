#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}" # Strip leading 'v' if present
APP_NAME="MarkdownConverter"
APP_ID="com.markdownconverter.app"

echo "Building AppImage for $APP_NAME v$VERSION"

# Paths
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net10.0/linux-x64/publish"
APPDIR="$PROJECT_ROOT/build-appimage/${APP_NAME}.AppDir"
OUTPUT_DIR="$PROJECT_ROOT/release"

# Check dependencies
if ! command -v curl &> /dev/null; then
    echo "Error: curl is not installed."
    exit 1
fi

# Check if publish directory exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory not found at $PUBLISH_DIR"
    echo "Run: dotnet publish -c Release -r linux-x64 --self-contained true"
    exit 1
fi

# Clean previous build
rm -rf "$PROJECT_ROOT/build-appimage"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/lib/$APP_NAME"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/metainfo"
mkdir -p "$OUTPUT_DIR"

echo "Copying application files..."
# Copy published application
cp -r "$PUBLISH_DIR"/* "$APPDIR/usr/lib/$APP_NAME/"
chmod +x "$APPDIR/usr/lib/$APP_NAME/MarkdownConverter"

# Create wrapper script in usr/bin
cat <<'WRAPPER' > "$APPDIR/usr/bin/markdownconverter"
#!/bin/bash
SELF_DIR="$(dirname "$(readlink -f "$0")")"
export LD_LIBRARY_PATH="$SELF_DIR/../lib/MarkdownConverter:${LD_LIBRARY_PATH}"
export DOTNET_ROOT="$SELF_DIR/../lib/MarkdownConverter"
exec "$SELF_DIR/../lib/MarkdownConverter/MarkdownConverter" "$@"
WRAPPER
chmod +x "$APPDIR/usr/bin/markdownconverter"

# Create .desktop file with proper AppImage integration
cat <<EOF > "$APPDIR/usr/share/applications/$APP_ID.desktop"
[Desktop Entry]
Version=1.1
Type=Application
Name=Markdown Converter
GenericName=Markdown Converter
Comment=Convert Markdown to PDF, Word, and Excel
Exec=markdownconverter %F
Icon=$APP_ID
Terminal=false
Categories=Utility;Office;TextEditor;Development;
MimeType=text/markdown;text/x-markdown;
Keywords=markdown;pdf;word;excel;converter;
StartupWMClass=MarkdownConverter
StartupNotify=true
X-AppImage-Version=$VERSION
EOF

# Copy desktop file to AppDir root (required by AppImage)
cp "$APPDIR/usr/share/applications/$APP_ID.desktop" "$APPDIR/$APP_ID.desktop"

# Handle icon - prefer PNG
ICON_FOUND=false
ICON_SOURCE="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.png"
if [ ! -f "$ICON_SOURCE" ]; then
    ICON_SOURCE="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.ico"
fi

if [ -f "$ICON_SOURCE" ]; then
    ICON_SOURCE="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.png"
    if [ ! -f "$ICON_SOURCE" ]; then
        ICON_SOURCE="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.ico"
    fi
    
    # Convert .ico to .png if ImageMagick is available and source is .ico
    if [[ "$ICON_SOURCE" == *.ico ]] && command -v convert &> /dev/null; then
        convert "$ICON_SOURCE" -resize 256x256 "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_ID.png"
    elif [[ "$ICON_SOURCE" == *.png ]]; then
        cp "$ICON_SOURCE" "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_ID.png"
    else
        # Copy as-is (fallback)
        cp "$ICON_SOURCE" "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_ID.png"
    fi
    
    # Copy icon to AppDir root (required by AppImage)
    cp "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_ID.png" "$APPDIR/$APP_ID.png"
    ICON_FOUND=true
    echo "✅ Icon installed: $ICON_SOURCE"
else
    echo "⚠️  Warning: No icon found. AppImage may not have an icon."
fi

# Create AppImage metadata (AppStream)
cat <<EOF > "$APPDIR/usr/share/metainfo/$APP_ID.appdata.xml"
<?xml version="1.0" encoding="UTF-8"?>
<component type="desktop-application">
  <id>$APP_ID</id>
  <metadata_license>MIT</metadata_license>
  <project_license>MIT</project_license>
  <name>Markdown Converter</name>
  <summary>Convert Markdown to PDF, Word, and Excel</summary>
  <description>
    <p>
      Markdown Converter Pro is a cross-platform desktop application that converts
      Markdown files into PDF, Word (DOCX), and Excel (XLSX) formats.
    </p>
    <p>Features:</p>
    <ul>
      <li>Drag-and-drop support</li>
      <li>Quick paste functionality</li>
      <li>Offline operation</li>
      <li>Multiple export formats (PDF, DOCX, XLSX)</li>
    </ul>
  </description>
  <launchable type="desktop-id">$APP_ID.desktop</launchable>
  <url type="homepage">https://github.com/AhmedElbehary-Dev/MarkdownConverter</url>
  <url type="bugtracker">https://github.com/AhmedElbehary-Dev/MarkdownConverter/issues</url>
  <provides>
    <binary>MarkdownConverter</binary>
  </provides>
</component>
EOF

# Create AppRun script with proper environment setup
cat <<'APPRUN' > "$APPDIR/AppRun"
#!/bin/bash
SELF="$(readlink -f "$0")
APPDIR="${SELF%/*}"

# Set up environment
export PATH="${APPDIR}/usr/bin:${PATH}"
export LD_LIBRARY_PATH="${APPDIR}/usr/lib/MarkdownConverter:${LD_LIBRARY_PATH}"
export DOTNET_ROOT="${APPDIR}/usr/lib/MarkdownConverter"
export XDG_DATA_DIRS="${APPDIR}/usr/share:${XDG_DATA_DIRS}"
export APPDIR="$APPDIR"

# Execute the application
exec "${APPDIR}/usr/lib/MarkdownConverter/MarkdownConverter" "$@"
APPRUN
chmod +x "$APPDIR/AppRun"

# Download appimagetool if not present
APPIMAGETOOL="$PROJECT_ROOT/build-appimage/appimagetool"
if [ ! -f "$APPIMAGETOOL" ]; then
    echo "Downloading appimagetool..."
    curl -fsSL -o "$APPIMAGETOOL" \
        "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
    chmod +x "$APPIMAGETOOL"
fi

# Build the AppImage
echo "Building AppImage..."
export ARCH=x86_64
"$APPIMAGETOOL" --comp zstd --mksquashfs-opt -Xcompression-level --mksquashfs-opt 18 \
    "$APPDIR" "$OUTPUT_DIR/MarkdownConverter-linux-x64.AppImage"

echo "✅ Successfully created $OUTPUT_DIR/MarkdownConverter-linux-x64.AppImage"
echo ""
echo "File size: $(du -h "$OUTPUT_DIR/MarkdownConverter-linux-x64.AppImage" | cut -f1)"
echo ""
echo "To run:"
echo "  chmod +x $OUTPUT_DIR/MarkdownConverter-linux-x64.AppImage"
echo "  $OUTPUT_DIR/MarkdownConverter-linux-x64.AppImage"

# Cleanup
rm -rf "$PROJECT_ROOT/build-appimage"

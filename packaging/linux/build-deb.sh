#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}" # Strip leading 'v' if present
ARCH="amd64"
APP_NAME="markdownconverter"
PACKAGE_NAME="${APP_NAME}_${VERSION}_${ARCH}"

echo "Building Debian package: $PACKAGE_NAME"

# Clean up any previous build artifacts
rm -rf "$PACKAGE_NAME"

# Create directories
mkdir -p "$PACKAGE_NAME/DEBIAN"
chmod 0755 "$PACKAGE_NAME/DEBIAN"
mkdir -p "$PACKAGE_NAME/usr/bin"
mkdir -p "$PACKAGE_NAME/usr/share/applications"
mkdir -p "$PACKAGE_NAME/usr/share/pixmaps"
mkdir -p "$PACKAGE_NAME/usr/lib/$APP_NAME"

# Create control file
cat <<EOF > "$PACKAGE_NAME/DEBIAN/control"
Package: $APP_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Depends: libc6, libgcc-s1, libstdc++6, libicu74 | libicu72 | libicu70 | libicu69 | libicu68 | libicu67 | libicu66 | libicu65
Maintainer: MarkdownConverter Team
Homepage: https://github.com/AhmedElbehary-Dev/MarkdownConverter
Replaces: $APP_NAME (<< $VERSION)
Conflicts: $APP_NAME (<< $VERSION)
Description: Markdown to PDF, Word, and Excel Converter
 A desktop application to convert Markdown files to PDF, Word (DOCX),
 and Excel (XLSX) using Avalonia UI. Features include drag-and-drop
 support, quick paste functionality, and offline operation.
EOF

# Create postinst script (runs after installation)
cat <<'EOF' > "$PACKAGE_NAME/DEBIAN/postinst"
#!/bin/bash
set -e

# Update desktop database if available
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications || true
fi

# Update icon cache if available
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor || true
fi

# Make the main executable sure it's executable
chmod +x /usr/lib/markdownconverter/MarkdownConverter 2>/dev/null || true

echo "MarkdownConverter $VERSION installed successfully."
EOF
chmod 0755 "$PACKAGE_NAME/DEBIAN/postinst"

# Create prerm script (runs before removal)
cat <<'EOF' > "$PACKAGE_NAME/DEBIAN/prerm"
#!/bin/bash
set -e

# Check if the application is running
if pgrep -f "MarkdownConverter" > /dev/null 2>&1; then
    echo "Warning: MarkdownConverter appears to be running."
    echo "Please close the application before removing the package."
    exit 1
fi
EOF
chmod 0755 "$PACKAGE_NAME/DEBIAN/prerm"

# Create postrm script (runs after removal)
cat <<'EOF' > "$PACKAGE_NAME/DEBIAN/postrm"
#!/bin/bash
set -e

if [ "$1" = "remove" ]; then
    # Package removed but config files may remain
    # Clean up any lingering desktop entries
    rm -f /usr/share/applications/markdown-converter.desktop 2>/dev/null || true
    
    # Update desktop database
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database -q /usr/share/applications 2>/dev/null || true
    fi
    
    # Update icon cache
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor 2>/dev/null || true
    fi
elif [ "$1" = "purge" ]; then
    # Complete purge - remove all config files
    rm -rf /etc/markdownconverter 2>/dev/null || true
    rm -rf /usr/lib/markdownconverter 2>/dev/null || true
    rm -f /usr/bin/markdownconverter 2>/dev/null || true
    rm -f /usr/share/applications/markdown-converter.desktop 2>/dev/null || true
    rm -f /usr/share/pixmaps/markdownconverter.* 2>/dev/null || true
    
    # Update desktop database
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database -q /usr/share/applications 2>/dev/null || true
    fi
    
    # Update icon Cache
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor 2>/dev/null || true
    fi
    
    echo "MarkdownConverter completely removed."
fi
EOF
chmod 0755 "$PACKAGE_NAME/DEBIAN/postrm"

# Copy published application files to /usr/lib/markdownconverter
# Assuming this script is run after "dotnet publish -c Release -r linux-x64 --self-contained true"
PUBLISH_DIR="../../bin/Release/net10.0/linux-x64/publish"
cp -r $PUBLISH_DIR/* "$PACKAGE_NAME/usr/lib/$APP_NAME/"

# Create a symlink in /usr/bin to the executable
ln -s "/usr/lib/$APP_NAME/MarkdownConverter" "$PACKAGE_NAME/usr/bin/$APP_NAME"
chmod +x "$PACKAGE_NAME/usr/lib/$APP_NAME/MarkdownConverter"

# Copy the desktop file (from the existing .github/workflows directory or create one)
cat <<EOF > "$PACKAGE_NAME/usr/share/applications/markdown-converter.desktop"
[Desktop Entry]
Version=1.1
Type=Application
Name=Markdown Converter
GenericName=Markdown Converter
Comment=Convert Markdown to PDF, Word, and Excel
Exec=/usr/bin/$APP_NAME %F
Icon=markdownconverter
Terminal=false
Categories=Utility;Office;TextEditor;Development;
MimeType=text/markdown;text/x-markdown;application/vnd.markdown;
Keywords=markdown;pdf;word;excel;converter;
StartupWMClass=MarkdownConverter
StartupNotify=true
EOF

# Add icon if available
ICON_SOURCE="../../src/MarkdownConverter.Desktop/Assets/md_converter.png"
if [ -f "$ICON_SOURCE" ]; then
    # Copy PNG icon to pixmaps
    cp "$ICON_SOURCE" "$PACKAGE_NAME/usr/share/pixmaps/markdownconverter.png"
    sed -i "s/Icon=$APP_NAME/Icon=markdownconverter.png/g" "$PACKAGE_NAME/usr/share/applications/markdown-converter.desktop"
    
    # Also install to hicolor icon theme directory for better integration
    mkdir -p "$PACKAGE_NAME/usr/share/icons/hicolor/256x256/apps"
    cp "$ICON_SOURCE" "$PACKAGE_NAME/usr/share/icons/hicolor/256x256/apps/markdownconverter.png"
elif [ -f "../../src/MarkdownConverter.Desktop/Assets/md_converter.ico" ]; then
    # Fallback to .ico if PNG not available
    cp "../../src/MarkdownConverter.Desktop/Assets/md_converter.ico" "$PACKAGE_NAME/usr/share/pixmaps/markdownconverter.ico"
    sed -i "s/Icon=$APP_NAME/Icon=markdownconverter.ico/g" "$PACKAGE_NAME/usr/share/applications/markdown-converter.desktop"
fi

# Build the .deb
dpkg-deb --build "$PACKAGE_NAME"
mv "$PACKAGE_NAME.deb" ../../release/
echo "Successfully created ../../release/$PACKAGE_NAME.deb"
rm -rf "$PACKAGE_NAME"

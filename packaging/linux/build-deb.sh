#!/bin/bash
set -e

VERSION="2.0.2"
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
Maintainer: MarkdownConverter Team
Replaces: $APP_NAME (<< $VERSION)
Conflicts: $APP_NAME (<< $VERSION)
Description: Markdown to PDF Converter
 A desktop application to convert Markdown files to PDF using Avalonia.
EOF

# Copy published application files to /usr/lib/markdownconverter
# Assuming this script is run after "dotnet publish -c Release -r linux-x64 --self-contained true"
PUBLISH_DIR="../../src/MarkdownConverter.Desktop/bin/Release/net10.0/linux-x64/publish"
cp -r $PUBLISH_DIR/* "$PACKAGE_NAME/usr/lib/$APP_NAME/"

# Create a symlink in /usr/bin to the executable
ln -s "/usr/lib/$APP_NAME/MarkdownConverter.Desktop" "$PACKAGE_NAME/usr/bin/$APP_NAME"
chmod +x "$PACKAGE_NAME/usr/lib/$APP_NAME/MarkdownConverter.Desktop"

# Copy the desktop file (from the existing .github/workflows directory or create one)
cat <<EOF > "$PACKAGE_NAME/usr/share/applications/markdown-converter.desktop"
[Desktop Entry]
Version=1.0
Type=Application
Name=Markdown Converter
Comment=Convert Markdown to PDF
Exec=/usr/bin/$APP_NAME
Icon=$APP_NAME
Terminal=false
Categories=Utility;Office;
EOF

# Add icon if available
if [ -f "../../src/MarkdownConverter.Desktop/Assets/md_converter.ico" ]; then
    # In a real scenario we'd convert .ico to .png, but for now we just copy it
    cp "../../src/MarkdownConverter.Desktop/Assets/md_converter.ico" "$PACKAGE_NAME/usr/share/pixmaps/$APP_NAME.ico"
    sed -i "s/Icon=$APP_NAME/Icon=$APP_NAME.ico/g" "$PACKAGE_NAME/usr/share/applications/markdown-converter.desktop"
fi

# Build the .deb
dpkg-deb --build "$PACKAGE_NAME"
mv "$PACKAGE_NAME.deb" ../../release/
echo "Successfully created ../../release/$PACKAGE_NAME.deb"
rm -rf "$PACKAGE_NAME"

#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}"

echo "Building RPM package: markdownconverter-$VERSION"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net10.0/linux-x64/publish"
OUTPUT_DIR="$PROJECT_ROOT/release"
SPEC_FILE="$SCRIPT_DIR/markdownconverter.spec"
ICON_PNG="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.png"
ICON_ICO="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.ico"

# Check publish dir exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: Publish directory not found at $PUBLISH_DIR"
    exit 1
fi

# Setup rpmbuild directory structure
RPM_BUILD_DIR="$PROJECT_ROOT/build-rpm"
rm -rf "$RPM_BUILD_DIR"
mkdir -p "$RPM_BUILD_DIR"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
mkdir -p "$OUTPUT_DIR"

# Copy published files to SOURCES as tarball
cp -r "$PUBLISH_DIR" "$RPM_BUILD_DIR/SOURCES/publish"

# Copy icons - ensure PNG is always available for RPM %files section
if [ -f "$ICON_PNG" ]; then
    cp "$ICON_PNG" "$RPM_BUILD_DIR/SOURCES/md_converter.png"
    echo "Copied PNG icon: $ICON_PNG"
elif [ -f "$ICON_ICO" ]; then
    echo "Warning: PNG icon not found, falling back to ICO (RPM will fail without PNG)"
    cp "$ICON_ICO" "$RPM_BUILD_DIR/SOURCES/md_converter.ico"
else
    echo "Error: No icon files found"
    exit 1
fi

# Copy spec file
cp "$SPEC_FILE" "$RPM_BUILD_DIR/SPECS/markdownconverter.spec"

# Build the RPM
echo "Running rpmbuild..."
rpmbuild --define "_topdir $RPM_BUILD_DIR" \
         --define "_version $VERSION" \
         --define "_sourcedir $RPM_BUILD_DIR/SOURCES" \
         -bb "$RPM_BUILD_DIR/SPECS/markdownconverter.spec" 2>&1 || {
    echo "RPM build failed. Checking for errors..."
    exit 1
}

# Copy the built RPM to release directory
if ls "$RPM_BUILD_DIR/RPMS"/*/*.rpm 1> /dev/null 2>&1; then
    cp "$RPM_BUILD_DIR/RPMS"/*/*.rpm "$OUTPUT_DIR/"
    echo "Successfully created RPM in $OUTPUT_DIR/"
else
    echo "Error: No RPM files found after build"
    exit 1
fi

# Cleanup
rm -rf "$RPM_BUILD_DIR"

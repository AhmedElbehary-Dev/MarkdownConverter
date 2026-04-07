#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}" # Strip leading 'v'

echo "Building RPM package: markdownconverter-$VERSION"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net10.0/linux-x64/publish"
OUTPUT_DIR="$PROJECT_ROOT/release"
SPEC_FILE="$SCRIPT_DIR/markdownconverter.spec"
ICON_FILE="$PROJECT_ROOT/src/MarkdownConverter.Desktop/Assets/md_converter.ico"

# Setup rpmbuild directory structure
RPM_BUILD_DIR="$PROJECT_ROOT/build-rpm"
rm -rf "$RPM_BUILD_DIR"
mkdir -p "$RPM_BUILD_DIR"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
mkdir -p "$OUTPUT_DIR"

# Copy published files to SOURCES
cp -r "$PUBLISH_DIR" "$RPM_BUILD_DIR/SOURCES/publish"

# Copy icon if available
if [ -f "$ICON_FILE" ]; then
    cp "$ICON_FILE" "$RPM_BUILD_DIR/SOURCES/md_converter.ico"
fi

# Copy spec file
cp "$SPEC_FILE" "$RPM_BUILD_DIR/SPECS/markdownconverter.spec"

# Build the RPM
rpmbuild --define "_topdir $RPM_BUILD_DIR" \
         --define "_version $VERSION" \
         --define "_sourcedir $RPM_BUILD_DIR/SOURCES" \
         -bb "$RPM_BUILD_DIR/SPECS/markdownconverter.spec"

# Copy the built RPM to release directory
find "$RPM_BUILD_DIR/RPMS" -name "*.rpm" -exec cp {} "$OUTPUT_DIR/" \;

echo "✅ Successfully created RPM package in $OUTPUT_DIR/"

# Cleanup
rm -rf "$RPM_BUILD_DIR"

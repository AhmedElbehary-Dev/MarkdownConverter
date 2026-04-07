#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}" # Strip leading 'v' if present

echo "Building Flatpak package: com.markdownconverter.app v$VERSION"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net10.0/linux-x64/publish"
OUTPUT_DIR="$PROJECT_ROOT/release"
MANIFEST="$SCRIPT_DIR/com.markdownconverter.app.yml"

# Check if flatpak-builder is installed
if ! command -v flatpak-builder &> /dev/null; then
    echo "Error: flatpak-builder is not installed."
    echo "Install it with: sudo apt install flatpak-builder (Debian/Ubuntu)"
    echo "Or: sudo dnf install flatpak-builder (Fedora/RHEL)"
    exit 1
fi

# Check if flatpak is installed
if ! command -v flatpak &> /dev/null; then
    echo "Error: flatpak is not installed."
    echo "Install it with: sudo apt install flatpak (Debian/Ubuntu)"
    echo "Or: sudo dnf install flatpak (Fedora/RHEL)"
    exit 1
fi

# Ensure the Flathub remote is added (for runtime dependencies)
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo 2>/dev/null || true

# Clean previous build
rm -rf "$PROJECT_ROOT/build-flatpak"
mkdir -p "$PROJECT_ROOT/build-flatpak"
mkdir -p "$OUTPUT_DIR"

# Update the manifest with the correct version
sed "s/Version:.*/Version: $VERSION/" "$MANIFEST" > "$PROJECT_ROOT/build-flatpak/manifest.yml"

# Build the Flatpak
echo "Running flatpak-builder..."
flatpak-builder \
    --force-clean \
    --install-deps-from=flathub \
    --repo="$PROJECT_ROOT/build-flatpak/repo" \
    "$PROJECT_ROOT/build-flatpak/app" \
    "$PROJECT_ROOT/build-flatpak/manifest.yml"

# Export the Flatpak to a single file
echo "Exporting Flatpak to .flatpak file..."
flatpak build-bundle \
    "$PROJECT_ROOT/build-flatpak/repo" \
    "$OUTPUT_DIR/MarkdownConverter-linux-x64.flatpak" \
    com.markdownconverter.app \
    --runtime-repo=https://flathub.org/repo/flathub.flatpakrepo

echo "✅ Successfully created $OUTPUT_DIR/MarkdownConverter-linux-x64.flatpak"
echo ""
echo "To install:"
echo "  flatpak install $OUTPUT_DIR/MarkdownConverter-linux-x64.flatpak"
echo ""
echo "To run:"
echo "  flatpak run com.markdownconverter.app"

# Cleanup
rm -rf "$PROJECT_ROOT/build-flatpak"

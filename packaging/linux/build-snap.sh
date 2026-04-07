#!/bin/bash
set -e

VERSION="${1:-1.0.0}"
VERSION="${VERSION#v}" # Strip leading 'v' if present

echo "Building Snap package: markdownconverter v$VERSION"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
OUTPUT_DIR="$PROJECT_ROOT/release"
SNAP_DIR="$SCRIPT_DIR/snap"
SNAPCRAFT_FILE="$SNAP_DIR/snapcraft.yaml"

# Check if snapcraft is installed
if ! command -v snapcraft &> /dev/null; then
    echo "Error: snapcraft is not installed."
    echo "Install it with: sudo snap install snapcraft --classic"
    exit 1
fi

# Clean previous build
rm -rf "$PROJECT_ROOT/build-snap"
mkdir -p "$OUTPUT_DIR"

# Update version in snapcraft.yaml
sed "s/version:.*/version: '$VERSION'/" "$SNAPCRAFT_FILE" > "$PROJECT_ROOT/build-snap/snapcraft.yaml"

# Copy updated snapcraft.yaml to snap directory
cp "$PROJECT_ROOT/build-snap/snapcraft.yaml" "$SNAP_DIR/snapcraft.yaml"

# Build the snap
echo "Running snapcraft..."
cd "$PROJECT_ROOT"
SNAPCRAFT_BUILD_ENVIRONMENT="host" snapcraft --project-dir "$PROJECT_ROOT" --output "$OUTPUT_DIR/MarkdownConverter-linux-x64.snap"

echo "✅ Successfully created $OUTPUT_DIR/MarkdownConverter-linux-x64.snap"
echo ""
echo "To install:"
echo "  sudo snap install $OUTPUT_DIR/MarkdownConverter-linux-x64.snap --dangerous"
echo ""
echo "To run:"
echo "  markdownconverter"

# Cleanup
rm -rf "$PROJECT_ROOT/build-snap"

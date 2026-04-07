#!/bin/bash
set -e

VERSION="${1:-}"
SKIP_DEB=false
SKIP_RPM=false
SKIP_APPIMAGE=false
SKIP_FLATPAK=false
SKIP_SNAP=false
SKIP_ALL=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-deb) SKIP_DEB=true; shift ;;
        --skip-rpm) SKIP_RPM=true; shift ;;
        --skip-appimage) SKIP_APPIMAGE=true; shift ;;
        --skip-flatpak) SKIP_FLATPAK=true; shift ;;
        --skip-snap) SKIP_SNAP=true; shift ;;
        --skip-all) SKIP_ALL=true; shift ;;
        --help)
            echo "Usage: ./build-all-linux.sh [VERSION] [OPTIONS]"
            echo ""
            echo "Arguments:"
            echo "  VERSION          Version number (e.g., v2.0.9 or 2.0.9)"
            echo ""
            echo "Options:"
            echo "  --skip-deb       Skip building .deb package"
            echo "  --skip-rpm       Skip building .rpm package"
            echo "  --skip-appimage  Skip building AppImage"
            echo "  --skip-flatpak   Skip building Flatpak"
            echo "  --skip-snap      Skip building Snap"
            echo "  --skip-all       Skip all builds (use for dry-run)"
            echo "  --help           Show this help message"
            exit 0
            ;;
        *)
            if [ -z "$VERSION" ]; then
                VERSION="$1"
            fi
            shift
            ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net10.0/linux-x64/publish"
OUTPUT_DIR="$PROJECT_ROOT/release"

echo ""
echo "===================================================" 
echo "   Linux All-in-One Build Script"
echo "==================================================="
echo ""

# Resolve version from .csproj if not provided
if [ -z "$VERSION" ]; then
    CSProj_PATH="$PROJECT_ROOT/MarkdownConverter.csproj"
    if [ -f "$CSProj_PATH" ]; then
        VERSION=$(grep -oP '<Version>\K[^<]+' "$CSProj_PATH" | head -1)
        if [ -n "$VERSION" ]; then
            VERSION="v$VERSION"
        fi
    fi
    
    if [ -z "$VERSION" ]; then
        echo "Error: Could not determine version. Pass version as argument or check .csproj file."
        exit 1
    fi
    echo "ℹ️  Auto-detected version: $VERSION"
fi

VERSION_CLEAN="${VERSION#v}"
echo "📦 Building version: $VERSION_CLEAN"
echo ""

# Check if publish output exists
if [ ! -d "$PUBLISH_DIR" ]; then
    echo "❌ Error: Publish directory not found at:"
    echo "   $PUBLISH_DIR"
    echo ""
    echo "Run first:"
    echo "  dotnet publish MarkdownConverter.csproj -c Release -r linux-x64 --self-contained true"
    echo ""
    exit 1
fi

# Check if the main executable exists
if [ ! -f "$PUBLISH_DIR/MarkdownConverter" ]; then
    echo "❌ Error: MarkdownConverter executable not found in publish directory."
    exit 1
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Track successful builds
SUCCESSFUL_BUILDS=()
FAILED_BUILDS=()

# Function to run a build
run_build() {
    local name="$1"
    local script="$2"
    local skip_flag="$3"
    
    if [ "$skip_flag" = true ]; then
        echo "⏭️  Skipping $name build"
        return 0
    fi
    
    echo ""
    echo "─────────────────────────────────────────────"
    echo "🔨 Building $name..."
    echo "─────────────────────────────────────────────"
    
    if [ ! -f "$script" ]; then
        echo "❌ Error: Build script not found: $script"
        FAILED_BUILDS+=("$name")
        return 1
    fi
    
    chmod +x "$script"
    
    if "$script" "$VERSION"; then
        echo "✅ $name build succeeded"
        SUCCESSFUL_BUILDS+=("$name")
        return 0
    else
        echo "❌ $name build failed"
        FAILED_BUILDS+=("$name")
        return 1
    fi
}

# Run builds
run_build "Debian (.deb)" "$SCRIPT_DIR/build-deb.sh" "$SKIP_DEB" || true
run_build "RPM (.rpm)" "$SCRIPT_DIR/build-rpm.sh" "$SKIP_RPM" || true
run_build "AppImage" "$SCRIPT_DIR/build-appimage.sh" "$SKIP_APPIMAGE" || true
run_build "Flatpak (.flatpak)" "$SCRIPT_DIR/build-flatpak.sh" "$SKIP_FLATPAK" || true
run_build "Snap (.snap)" "$SCRIPT_DIR/build-snap.sh" "$SKIP_SNAP" || true

# Create portable tarball
echo ""
echo "─────────────────────────────────────────────"
echo "📦 Creating portable tar.gz..."
echo "─────────────────────────────────────────────"

TAR_NAME="$OUTPUT_DIR/MarkdownConverter-linux-x64-portable.tar.gz"
tar czf "$TAR_NAME" -C "$PUBLISH_DIR" .
echo "✅ Created: $TAR_NAME ($(du -h "$TAR_NAME" | cut -f1))"
SUCCESSFUL_BUILDS+=("Portable tar.gz")

# Summary
echo ""
echo "==================================================="
echo "   Build Complete - $VERSION_CLEAN"
echo "==================================================="
echo ""
echo "✅ Successful builds (${#SUCCESSFUL_BUILDS[@]}):"
for build in "${SUCCESSFUL_BUILDS[@]}"; do
    echo "   ✓ $build"
done

if [ ${#FAILED_BUILDS[@]} -gt 0 ]; then
    echo ""
    echo "❌ Failed builds (${#FAILED_BUILDS[@]}):"
    for build in "${FAILED_BUILDS[@]}"; do
        echo "   ✗ $build"
    done
fi

echo ""
echo "📁 Release files in: $OUTPUT_DIR"
echo ""
echo "Files:"
ls -lh "$OUTPUT_DIR" | grep -E '\.(deb|rpm|AppImage|flatpak|snap|tar\.gz|zip)$' | awk '{print "   " $NF " (" $5 ")"}'

echo ""
if [ ${#FAILED_BUILDS[@]} -gt 0 ]; then
    echo "⚠️  Some builds failed. Check output above for details."
    exit 1
else
    echo "✅ All builds succeeded!"
    exit 0
fi

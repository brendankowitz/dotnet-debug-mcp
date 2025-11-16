#!/bin/bash
set -e

# NetCoreDbg Installation Script
# Downloads and installs the latest NetCoreDbg for .NET debugging

VERSION="3.1.0-1031"
ARCH="amd64"
OS="linux"

echo "Installing NetCoreDbg ${VERSION} for ${OS}-${ARCH}..."

# Determine installation directory
if [ "$EUID" -eq 0 ]; then
    INSTALL_DIR="/usr/local/bin"
    echo "Running as root, will install to ${INSTALL_DIR}"
else
    INSTALL_DIR="$HOME/.local/bin"
    echo "Running as user, will install to ${INSTALL_DIR}"
    mkdir -p "${INSTALL_DIR}"
fi

# Create temporary directory
TMP_DIR=$(mktemp -d)
cd "${TMP_DIR}"

# Download NetCoreDbg
DOWNLOAD_URL="https://github.com/Samsung/netcoredbg/releases/download/${VERSION}/netcoredbg-${OS}-${ARCH}.tar.gz"
echo "Downloading from ${DOWNLOAD_URL}..."
wget -q --show-progress "${DOWNLOAD_URL}" -O netcoredbg.tar.gz

# Extract
echo "Extracting..."
tar -xzf netcoredbg.tar.gz

# Install
echo "Installing to ${INSTALL_DIR}..."
if [ "$EUID" -eq 0 ]; then
    cp -r netcoredbg/* "${INSTALL_DIR}/"
else
    cp -r netcoredbg/* "${INSTALL_DIR}/"
fi

# Cleanup
cd - > /dev/null
rm -rf "${TMP_DIR}"

# Verify installation
if command -v netcoredbg &> /dev/null; then
    echo ""
    echo "✓ NetCoreDbg installed successfully!"
    echo ""
    netcoredbg --version
    echo ""
else
    echo ""
    echo "⚠ NetCoreDbg installed but not in PATH"
    echo "Add ${INSTALL_DIR} to your PATH:"
    echo ""
    echo "    export PATH=\"\${PATH}:${INSTALL_DIR}\""
    echo ""
    echo "Or add this to your ~/.bashrc or ~/.zshrc"
    exit 1
fi

echo "Installation complete!"

#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
# Install Python dependencies
python3 -m pip install --upgrade pip
python3 -m pip install -r python/requirements.txt
# Build the Swift wrapper
cd SwiftApp
swift build -c release
cd ..

echo "MESHMapMac built successfully. Run ./SwiftApp/.build/release/MESHMapMac --help for usage."

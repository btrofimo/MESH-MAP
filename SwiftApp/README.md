# MESHMapMac

A minimal Swift command-line wrapper around the Python processing scripts.
It downloads MRMS data and processes it via the existing pipeline.

Requires **macOS 15**.
The first time it runs, the tool installs the Python dependencies using the
`Resources/install_python_deps.sh` script bundled in the package.

```
Usage: MESHMapMac --date YYYYMMDD [--time HHMMSS] [--product PRODUCT]
```

Build with the Swift Package Manager:

```
cd SwiftApp
swift build -c release
```

Run the tool after building (example):

```
.build/release/MESHMapMac --date 20240507 --time 100000 --product MESH_Max_1440min_00.50
```

# MESH-MAP
Maximum Estimated Size of Hail - Monitoring and Analysis Program

This repository targets **macOS 15**. All usage instructions assume a Mac
environment. The `MESH_MAP` folder contains a historical Windows-only add-in
that is no longer maintained.

### MESH Scan
![](https://github.com/Northern-Tornadoes-Project/MESH-MAP/blob/main/images/mesh.png)

### Denoised Scan
![](https://github.com/Northern-Tornadoes-Project/MESH-MAP/blob/main/images/mesh_denoised.png)

### Detected Swaths
![](https://github.com/Northern-Tornadoes-Project/MESH-MAP/blob/main/images/swaths.png)

### UI
![](https://github.com/Northern-Tornadoes-Project/MESH-MAP/blob/main/images/ui1.png)
![](https://github.com/Northern-Tornadoes-Project/MESH-MAP/blob/main/images/ui2.png)

### GRIB2 Input Directory
Place any GRIB2 files you want to process in the `grib2_files` folder at the repository root.  A helper script `python/download_mesh.py` can fetch a file directly from the NOAA MRMS AWS bucket and convert it to a GeoTIFF.

### Quick Setup
For a fully automated setup on macOS, double‑click the `install.command` file in
the repository root (or run it from the terminal). It installs the Python
dependencies and builds the Swift wrapper so you can immediately run the tool.

### Usage
1. Install GDAL and the Python dependencies (requires [Homebrew](https://brew.sh)):
   ```bash
   brew install gdal

   
1. Install GDAL and the Python dependencies:
   ```bash
   sudo apt-get update && sudo apt-get install -y gdal-bin

   python3 -m pip install --upgrade pip
   python3 -m pip install -r python/requirements.txt
   ```
2. To check which times are available for a date, you can list them with:
   ```bash
   python3 python/download_mesh.py 20240228 --product MESH_00.50 --list-times
   ```
3. To retrieve MESH data for a date (e.g. `20240507`) and convert it to TIFF, run:
   ```bash
   python3 python/download_mesh.py 20240507 --time 100000 \
       --product MESH_Max_1440min_00.50 --out simple_images
   ```
   The resulting TIFF will be written to `simple_images/`.
4. Copy any other MESH `.tif` files into the `simple_images` folder. The main script expects them in this location.
5. (Optional) Place additional GRIB2 data files in `grib2_files` for reference. You can process them directly with:
   ```bash
   python3 python/process_grib2.py
   ```
   This converts each GRIB2 file to TIFF and writes the coloured output to `simple_images/results1/`.
6. To run the pipeline on existing TIFF files only:
   ```bash
   python3 python/main.py
   ```
   Results are saved to `simple_images/results1/`.

Note: The `python/download_mesh.py` helper downloads a single GRIB2 file for the specified product and time and converts it to TIFF. Use `--no-verify` if certificate errors occur when fetching from the NOAA bucket. The main processing script still requires TIFF input files.

### macOS Command-Line Wrapper
A Swift package in `SwiftApp` provides a convenience wrapper on macOS. When run
for the first time, it installs the required Python packages automatically.


A Swift package in `SwiftApp` provides a convenience wrapper on macOS. When run
for the first time, it installs the required Python packages automatically.
A Swift package in `SwiftApp` provides a convenience wrapper on macOS.


Build and run with:
```bash
cd SwiftApp
swift build -c release
.build/release/MESHMapMac --date 20240507 --time 100000 --product MESH_Max_1440min_00.50
```


### Simple GUI
A tiny Tkinter GUI can launch the download and processing pipeline.
Run it with:
```bash
python3 python/gui.py
```
Fill in the date, time, and product then click **Download & Process**.
The resulting TIFF and coloured output appear in `simple_images/`.
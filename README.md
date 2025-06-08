# MESH-MAP
 Maximum Estimated Size of Hail - Monitoring and Analysis Program

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

### Usage
1. Install the required Python packages and GDAL:
   ```bash
   sudo apt-get update && sudo apt-get install -y gdal-bin
   pip install numpy opencv-python networkx tqdm Pillow requests rasterio
   ```
2. To retrieve MESH data for a date (e.g. `20240507`) and convert it to TIFF, run:
   ```bash
   python3 python/download_mesh.py 20240507
   ```
   The resulting TIFF will be written to `simple_images/`.
3. Copy any other MESH `.tif` files into the `simple_images` folder. The main script expects them in this location.
4. (Optional) Place additional GRIB2 data files in `grib2_files` for reference.
5. Run the processing script from the repository root:
   ```bash
   python3 python/main.py
   ```
6. The processed images will be saved to `simple_images/results1/`.

Note: The `python/download_mesh.py` helper downloads a single GRIB2 file for the start of the requested day and converts it to TIFF. The main processing script still requires TIFF input files.

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
Place any GRIB2 files you want to process in the `grib2_files` folder at the repository root.

### Usage
1. Install the required Python packages:
   ```bash
   pip install numpy opencv-python networkx tqdm Pillow
   ```
2. Copy MESH `.tif` files into the `simple_images` folder. The script expects them in this location.
3. (Optional) Place any GRIB2 data files in `grib2_files` for future processing or reference.
4. Run the processing script from the repository root:
   ```bash
   python3 python/main.py
   ```
5. The processed images will be saved to `simple_images/results1/`.

Note: The provided scripts operate on local TIFF files. They do not download data automatically.

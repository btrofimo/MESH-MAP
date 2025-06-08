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
4. (Optional) Place additional GRIB2 data files in `grib2_files` for reference. You can process them directly with:
   ```bash
   python3 python/process_grib2.py
   ```
   This converts each GRIB2 file to TIFF and writes the coloured output to `simple_images/results1/`.
5. To run the pipeline on existing TIFF files only:
   Results are saved to `simple_images/results1/`.

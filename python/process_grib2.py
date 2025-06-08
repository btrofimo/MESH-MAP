#!/usr/bin/env python3
"""Convert all GRIB2 files in grib2_files to GeoTIFF and run the
existing MESH visualization pipeline on them."""

try:
    import requests
except ImportError as e:
    raise RuntimeError("Run `pip install -r python/requirements.txt`") from e

from pathlib import Path
import os
from download_mesh import grib2_to_tif
from main import process_tif


def process_grib2_folder(src_dir="grib2_files", tif_dir="simple_images"):
    src = Path(src_dir)
    tif_out = Path(tif_dir)
    tif_out.mkdir(parents=True, exist_ok=True)
    for grib in src.glob("*.grib2"):
        tif_path = tif_out / (grib.stem + ".tif")
        grib2_to_tif(str(grib), str(tif_path))
        process_tif(str(tif_path))


if __name__ == "__main__":
    process_grib2_folder()

import os
import gzip
import shutil
import subprocess
from datetime import datetime
from pathlib import Path
import requests


def download_mesh_grib2(date_str, dest_dir='grib2_files'):
    """Download MESH grib2 data for the given YYYYMMDD date.

    Returns path to the downloaded grib2 file.
    """
    date = datetime.strptime(date_str, '%Y%m%d')
    base_url = 'https://noaa-mrms-pds.s3.amazonaws.com/CONUS/MESH_00.50'
    prefix = f"{date:%Y%m%d}"  # folder name in bucket
    fname = f"MRMS_MESH_00.50_{date:%Y%m%d}-000000.grib2.gz"
    url = f"{base_url}/{prefix}/{fname}"
    Path(dest_dir).mkdir(parents=True, exist_ok=True)
    gz_path = Path(dest_dir) / fname
    resp = requests.get(url, stream=True)
    resp.raise_for_status()
    with open(gz_path, 'wb') as f:
        for chunk in resp.iter_content(chunk_size=8192):
            if chunk:
                f.write(chunk)
    grib2_path = gz_path.with_suffix('')
    with gzip.open(gz_path, 'rb') as f_in:
        with open(grib2_path, 'wb') as f_out:
            shutil.copyfileobj(f_in, f_out)
    return str(grib2_path)


def grib2_to_tif(grib2_path, tif_path):
    subprocess.check_call(['gdal_translate', grib2_path, tif_path])


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='Download MESH GRIB2 and convert to TIFF')
    parser.add_argument('date', help='Date in YYYYMMDD format')
    parser.add_argument('--out', default='simple_images', help='Directory to store TIFF')
    args = parser.parse_args()
    grib2 = download_mesh_grib2(args.date)
    Path(args.out).mkdir(parents=True, exist_ok=True)
    tif = Path(args.out) / f"{args.date}.tif"
    grib2_to_tif(grib2, tif)
    print('Saved', tif)

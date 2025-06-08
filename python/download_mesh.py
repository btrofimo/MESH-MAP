import os
import gzip
import shutil
import subprocess
from datetime import datetime
from pathlib import Path
import requests

from xml.etree import ElementTree


def download_mesh_grib2(date_str, time_str='000000', product='MESH_00.50',
                        dest_dir='grib2_files', verify=True):
    """Download MESH GRIB2 data from the NOAA MRMS AWS bucket.

    Parameters
    ----------
    date_str : str
        Date in ``YYYYMMDD`` format.
    time_str : str, optional
        Time within the day, default ``"000000"``.
    product : str, optional
        MRMS product name such as ``"MESH_00.50"`` or
        ``"MESH_Max_1440min_00.50"``.
    dest_dir : str, optional
        Directory to save the downloaded file.
    verify : bool, optional
        Set to ``False`` to skip TLS certificate verification.

    Returns
    -------
    str
        Path to the downloaded GRIB2 file.
    """
    date = datetime.strptime(date_str, '%Y%m%d')
    base_url = f'https://noaa-mrms-pds.s3.amazonaws.com/CONUS/{product}'
    prefix = f"{date:%Y%m%d}"
    fname = f"MRMS_{product}_{date:%Y%m%d}-{time_str}.grib2.gz"
    url = f"{base_url}/{prefix}/{fname}"
    Path(dest_dir).mkdir(parents=True, exist_ok=True)
    gz_path = Path(dest_dir) / fname
    resp = requests.get(url, stream=True, verify=verify)
    
def download_mesh_grib2(date_str, dest_dir='grib2_files'):
    """Download MESH grib2 data for the given YYYYMMDD date.mReturns path to the downloaded grib2 file."""
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

def list_available_times(date_str, product='MESH_00.50', verify=True):
    """Return a list of available HHMMSS strings for a date/product."""
    url = (
        "https://noaa-mrms-pds.s3.amazonaws.com?list-type=2&"
        f"prefix=CONUS/{product}/{date_str}/"
    )
    resp = requests.get(url, verify=verify)
    resp.raise_for_status()
    root = ElementTree.fromstring(resp.content)
    ns = "{http://s3.amazonaws.com/doc/2006-03-01/}"
    times = []
    for key in root.iter(ns + "Key"):
        text = key.text
        if text.endswith(".grib2.gz"):
            t = text.split("-")[-1].split(".")[0]
            times.append(t)
    return sorted(times)


def grib2_to_tif(grib2_path, tif_path):
    """Convert a GRIB2 file to an 8-bit GeoTIFF.

    The output is scaled using GDAL's ``-scale`` option so that the
    floating point hail values are mapped to the 0-255 range expected by
    the rest of the pipeline.
    """
    subprocess.check_call([
        'gdal_translate',
        '-ot', 'Byte',
        '-scale',
        grib2_path,
        tif_path,
    ])

def grib2_to_tif(grib2_path, tif_path):
    subprocess.check_call(['gdal_translate', grib2_path, tif_path])

if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='Download MESH GRIB2 and convert to TIFF')
    parser.add_argument('date', help='Date in YYYYMMDD format')
    parser.add_argument('--time', default='000000', help='Time (HHMMSS) within the day')
    parser.add_argument('--product', default='MESH_00.50',
                        help='MRMS product name, e.g. MESH_00.50 or MESH_Max_1440min_00.50')
    parser.add_argument('--out', default='simple_images', help='Directory to store TIFF')
    parser.add_argument('--no-verify', action='store_true', help='Disable TLS certificate verification')
    parser.add_argument('--list-times', action='store_true',
                        help='List available times for the date/product and exit')
    args = parser.parse_args()
    if args.list_times:
        times = list_available_times(args.date, args.product, verify=not args.no_verify)
        for t in times:
            print(t)
    else:
        grib2 = download_mesh_grib2(args.date, args.time, args.product,
                                    dest_dir='grib2_files', verify=not args.no_verify)
        Path(args.out).mkdir(parents=True, exist_ok=True)
        tif_name = f"{args.product}_{args.date}-{args.time}.tif"
        tif = Path(args.out) / tif_name
        grib2_to_tif(grib2, tif)
        print('Saved', tif)
    parser.add_argument('--out', default='simple_images', help='Directory to store TIFF')
    args = parser.parse_args()
    grib2 = download_mesh_grib2(args.date)
    Path(args.out).mkdir(parents=True, exist_ok=True)
    tif = Path(args.out) / f"{args.date}.tif"
    grib2_to_tif(grib2, tif)
    print('Saved', tif)
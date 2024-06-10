import os
from glob import glob

if __name__ == '__main__':
    files = glob('simple_images/*.tif')

    for f in files:
        #print("simple_images/" + f.split('\\')[1].split('_')[0] + ".tif")
        os.rename(f, "simple_images/" + f.split('\\')[1].split('.')[0] + ".tif")


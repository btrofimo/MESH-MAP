import sys

import numpy as np
import cv2
from main import imread_tif32f
from colour_swaths import colour_swath


def remove_artifacts(im):

    artifact_polygons = [[[2084, 1653], [2284, 1634], [2281, 1614]],
                         [[4608, 2175], [4654, 2284], [4623, 2187]],
                         [[4752, 2576], [4769, 2651], [4778, 2648]]]

    for polygon in artifact_polygons:
        cv2.fillPoly(im, [np.array(polygon)], 0)


def denoise_mesh(im):
    img = im.copy()
    remove_artifacts(img)

    mask = cv2.blur(img, (3, 3))

    mask = cv2.inRange(mask, 6, 255)

    mask2 = np.zeros_like(mask)

    num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)

    for i in range(1, num_labels):  # Start from 1 to skip the background component
        if stats[i, cv2.CC_STAT_AREA] >= 16:
            mask2[labels == i] = 255

    return cv2.bitwise_and(img, img, mask=mask2)


if __name__ == '__main__':
    img = imread_tif32f("simple_images/20230715.tif")

    img = denoise_mesh(img)

    cimg = colour_swath(img)

    cv2.imwrite("denoised.png", cimg)


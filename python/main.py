# MESH-MAP, (Maximum Estimated Size of Hail - Monitoring and Analysis Program)

import math
import os

import numpy as np
import cv2
import networkx as nx
from colour_swaths import colour_swath
#from denoise import denoise_mesh
from PIL import Image
from glob import glob
from tqdm import tqdm


def imread_tif32f(path):
    img = Image.open(path)
    return np.array(img, dtype=np.uint8)


def extract_cc_images(img, bimg, connectivity, area_threshold):
    (num_labels, labels_im, stats, centroids) = cv2.connectedComponentsWithStats(bimg, connectivity=connectivity,
                                                                                 ltype=cv2.CV_32S)
    # List to store cropped component images
    cropped_images = []
    cropped_stats = []
    cropped_centroids = []

    # Iterate through the components
    for i in range(1, num_labels):  # Start from 1 to skip the background
        area = stats[i, cv2.CC_STAT_AREA]

        if area >= area_threshold:
            x = stats[i, cv2.CC_STAT_LEFT]
            y = stats[i, cv2.CC_STAT_TOP]
            w = stats[i, cv2.CC_STAT_WIDTH]
            h = stats[i, cv2.CC_STAT_HEIGHT]

            # Crop the bounding box of the component
            component_image = img[y:y + h, x:x + w]

            # Create a mask for the current component
            component_mask = (labels_im[y:y + h, x:x + w] == i).astype(np.uint8) * 255

            # Apply the mask to the cropped image
            filtered_component_image = cv2.bitwise_and(component_image, component_image, mask=component_mask)

            cropped_images.append(filtered_component_image)
            cropped_stats.append(stats[i])
            cropped_centroids.append(centroids[i])

    return cropped_images, cropped_stats, cropped_centroids


def create_cc_graph(img):
    graph = nx.Graph()

    for i in range(img.shape[0]):
        for j in range(img.shape[1]):

            if img[i, j] != 0:

                for y in range(max(0, i - 1), min(img.shape[0], i + 2)):
                    for x in range(max(0, j - 1), min(img.shape[1], j + 2)):

                        if y == i and x == j:
                            continue

                        if img[y, x] != 0:
                            graph.add_edge((i, j), (y, x), weight=math.hypot(x - j, y - i))

    return graph


def path_length(graph, path):
    return sum(graph[u][v]['weight'] for u, v in zip(path[:-1], path[1:]))


def weighted_diameter(G):
    shortest_paths = dict(nx.all_pairs_dijkstra_path(G))

    # Find the longest of the shortest paths
    longest_shortest_path_length = 0
    longest_shortest_path = None

    for source, paths in shortest_paths.items():
        for target, path in paths.items():
            length = path_length(G, path)
            if length > longest_shortest_path_length:
                longest_shortest_path_length = length
                longest_shortest_path = path

    return longest_shortest_path_length, longest_shortest_path


def long_enough_subsection(image, lb, ub, length):
    bimage = cv2.inRange(image, lb, ub)
    subcomp_images, _, _ = extract_cc_images(image, bimage, 8, length)

    for sub_image in subcomp_images:
        g = create_cc_graph(sub_image)

        wd, path = weighted_diameter(g)

        if wd >= length:
            return path

    return None


def draw_referenced_contour(im, comp_im, stats):
    contour = cv2.findContours(comp_im, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)[0][0]
    x = stats[cv2.CC_STAT_LEFT]
    y = stats[cv2.CC_STAT_TOP]

    offset_contours = [np.array(contour, int) + np.array([x, y])]

    cv2.drawContours(im, offset_contours, -1, (0, 0, 0), 1)


def draw_referenced_box(im, stats):
    x = stats[cv2.CC_STAT_LEFT]
    y = stats[cv2.CC_STAT_TOP]
    w = stats[cv2.CC_STAT_WIDTH]
    h = stats[cv2.CC_STAT_HEIGHT]

    cv2.rectangle(im, (x - 1, y - 1), (x + w + 1, y + h + 1), (0, 0, 0), 1)


def draw_referenced_path(im, path, stats):
    x = stats[cv2.CC_STAT_LEFT]
    y = stats[cv2.CC_STAT_TOP]

    # [y, x] -> [x, y]
    path = [[p[1], p[0]] for p in path]

    path = np.array(path) + np.array([x, y])

    cv2.polylines(im, [path], False, (0, 0, 0), 1)


# TODO: Overall length rounded to .5
# TODO: Bounding box coords
# TODO: Max MESH value in swath
# TODO: Max swath width
# TODO: Max swath width path coords
if __name__ == '__main__':

    for file in tqdm(glob("../simple_images/*.tif")[21:]):
        name = os.path.basename(file)
        im = imread_tif32f(file)
        #im = denoise_mesh(im)
        cimg = colour_swath(im)

        binary_im = np.zeros_like(im, dtype=np.uint8)
        cv2.threshold(im, 10, 255, cv2.THRESH_BINARY, binary_im)

        comp_images, stats, centroids = extract_cc_images(im, binary_im, 8, 40)

        swath_idx = []
        swath_paths = []

        for i, image in enumerate(comp_images):

            req1 = long_enough_subsection(image, 10, 255, 40)
            req2 = long_enough_subsection(image, 30, 255, 2)

            if req1 is not None and req2 is not None:
                swath_idx.append(i)
                swath_paths.append([req1, req2])

        for i, s in enumerate(swath_idx):
            #draw_referenced_path(cimg, swath_paths[i][0], stats[s])
            draw_referenced_contour(cimg, comp_images[s], stats[s])
            draw_referenced_box(cimg, stats[s])

        cv2.imwrite("simple_images/results1/" + name, cimg)

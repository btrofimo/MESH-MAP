using ArcGIS.Core.Data.Raster;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static MESH_MAP.FeatureClassBuilder;

namespace MESH_MAP
{
    internal class Swath
    {
        public string name;
        public double length;
        public double width;
        public int maxValue;
        public int[] stats;
        public Mat im;
        public Point2d centroid;
        public Point inflectionPoint;
        public RotatedRect boundingBox;
        public Point[] contour;
        public List<List<Point>> lengthPaths;

        public Swath() { }

        public static async Task Export(Swath[] swaths, string fcName, RasterLayer rasterLayer)
        {
            List<Dictionary<string, object>> shapeDict = [];

            await QueuedTask.Run(async () =>
            {
                Raster raster = rasterLayer.GetRaster();

                foreach (Swath swath in swaths)
                {
                    var centroidMap = raster.PixelToMap((int)swath.centroid.X, (int)swath.centroid.Y);
                    var inflectionMap = raster.PixelToMap(swath.inflectionPoint.X, swath.inflectionPoint.Y);
                    System.Tuple<double, double>[] bbMap = [raster.PixelToMap(swath.stats[0], swath.stats[1]), 
                                                            raster.PixelToMap(swath.stats[0] + swath.stats[2], swath.stats[1] + swath.stats[3])];

                    List<Point> contour = [];

                    foreach (var pt in swath.contour)
                    {
                        contour.Add(new Point(pt.X + swath.stats[0] - 1, pt.Y + swath.stats[1] - 1));
                    }

                    shapeDict.Add(new()
                    {
                        { "Shape",  await CreatePolygon(contour.ToArray(), rasterLayer)},
                        { "Name", swath.name},
                        { "Length", swath.length},
                        { "Width", swath.width},
                        { "MaxValue", (double)swath.maxValue},
                        { "CentroidX", centroidMap.Item1},
                        { "CentroidY", centroidMap.Item2},
                        { "InflectionX", inflectionMap.Item1},
                        { "InflectionY", inflectionMap.Item2},
                        { "Top", bbMap[0].Item2},
                        { "Left", bbMap[0].Item1},
                        { "Bottom", bbMap[1].Item2},
                        { "Right", bbMap[1].Item1},
                    });
                }
            });

            await AddFeatures(fcName, shapeDict);
        }
    }
}

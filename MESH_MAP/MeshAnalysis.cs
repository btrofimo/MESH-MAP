using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.Raster;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using OpenCvSharp;
using static MESH_MAP.FeatureClassBuilder;
using Attribute = MESH_MAP.FeatureClassBuilder.Attribute;
using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Collections;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using OpenCvSharp.Dnn;
using System.Collections.Concurrent;
using ArcGIS.Desktop.Core;

namespace MESH_MAP
{
    static class MeshAnalysis
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        public static bool PreLoadDlls()
        {
            string path;

            #if DEBUG
                path = @"C:\Users\danie\Documents\Experiments\hail_mesh\HailMesh\MESH_MAP\bin\x64\Debug\net8.0-windows";
            #else
                path = Utils.AddinAssemblyLocation();
            #endif

            string[] dlls = ["OpenCvSharp", "OpenCvSharpExtern", "QuikGraph"];

            foreach (string dll in dlls)
            {
                IntPtr hModule = LoadLibrary(path + "\\" + dll + ".dll");

                if (hModule == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    System.Windows.MessageBox.Show("Failed to load DLLs: " + errorCode.ToString());
                    return false;
                }
            }

            return true;
        }

        public static async void RunAnalysis(List<RasterLayer> imageLayers, int[] denoiseParams, FeatureLayer artifactLayer, List<(int, int)> reqsList)
        {
            PrintTitle();
            Console.WriteLine("Initializing...\n");

            if (!PreLoadDlls()) return;

            for (int i = 0; i < imageLayers.Count; i++)
            {
                Console.WriteLine("Loading Image: " + imageLayers[i].Name + " " + (i+1) + "/" + imageLayers.Count + "\n");
                Mat im = await RasterToImage(imageLayers[i]);

                Console.WriteLine("Removing Artifacts\n");
                if (artifactLayer != null)
                {
                    if (!await RemoveArtifacts(im, imageLayers[i], artifactLayer)) return;
                }

                Console.WriteLine("Denoising Image\n");
                Denoise(im, denoiseParams[0], denoiseParams[1], denoiseParams[2]);

                Console.WriteLine("Loading Denoised Image\n");
                RasterLayer layer = await Utils.LoadImageFile(im, "Denoised_" + imageLayers[i].Name.Split('.')[0], imageLayers[0]);

                Console.WriteLine("Finding Swaths\n");
                await FindSwaths(im, imageLayers[i], reqsList);

                Console.WriteLine("Saving\n");
                await QueuedTask.Run(() =>
                {
                    layer.SetColorizer(imageLayers[i].GetColorizer());
                });
            }

                Console.WriteLine("Complete\n");
        }

        public static void PrintTitle()
        {
            Console.WriteLine("      ___           ___           ___           ___           ___           ___           ___     \n" +
                              "     /\\__\\         /\\  \\         /\\  \\         /\\__\\         /\\__\\         /\\  \\         /\\  \\    \n" +
                              "    /::|  |       /::\\  \\       /::\\  \\       /:/  /        /::|  |       /::\\  \\       /::\\  \\   \n" +
                              "   /:|:|  |      /:/\\:\\  \\     /:/\\ \\  \\     /:/__/        /:|:|  |      /:/\\:\\  \\     /:/\\:\\  \\  \n" +
                              "  /:/|:|__|__   /::\\~\\:\\  \\   _\\:\\~\\ \\  \\   /::\\  \\ ___   /:/|:|__|__   /::\\~\\:\\  \\   /::\\~\\:\\  \\ \n" +
                              " /:/ |::::\\__\\ /:/\\:\\ \\:\\__\\ /\\ \\:\\ \\ \\__\\ /:/\\:\\  /\\__\\ /:/ |::::\\__\\ /:/\\:\\ \\:\\__\\ /:/\\:\\ \\:\\__\\\n" +
                              " \\/__/~~/:/  / \\:\\~\\:\\ \\/__/ \\:\\ \\:\\ \\/__/ \\/__\\:\\/:/  / \\/__/~~/:/  / \\/__\\:\\/:/  / \\/__\\:\\/:/  /\n" +
                              "       /:/  /   \\:\\ \\:\\__\\    \\:\\ \\:\\__\\        \\::/  /        /:/  /       \\::/  /       \\::/  / \n" +
                              "      /:/  /     \\:\\ \\/__/     \\:\\/:/  /        /:/  /        /:/  /        /:/  /         \\/__/  \n" +
                              "     /:/  /       \\:\\__\\        \\::/  /        /:/  /        /:/  /        /:/  /                 \n" +
                              "     \\/__/         \\/__/         \\/__/         \\/__/         \\/__/         \\/__/                  \n" +
                              "-------------------------------------------------------------------------------------------------\n");
        }

        public static async Task FindSwaths(Mat im, RasterLayer rasterLayer, List<(int, int)> reqsList)
        {
            
            Mat bim = im.Threshold(reqsList[0].Item1, 255, ThresholdTypes.Binary);
            
            var (subcomps, stats, centroids) = ExtractCCImages(im, bim, PixelConnectivity.Connectivity8, reqsList[0].Item2);

            ConcurrentBag<Swath> swaths = [];

            Parallel.For(0, subcomps.Count, i =>
            {
                Mat subcomp = subcomps[i];
                Mat s = new Mat();

                subcomp.CopyTo(s);

                bool meetsReqs = true;

                List<double> lengths = [];
                List<List<Point>> paths = [];

                foreach (var req in reqsList)
                {
                    (double length, List<Point> path) = CheckLengthReq(subcomp, req);

                    if (length < 0)
                    {
                        meetsReqs = false;
                        break;
                    }

                    lengths.Add(length);
                    paths.Add(path);
                }

                if (meetsReqs)
                {
                    Mat dilated = ExpandAndDilate(subcomp.Threshold(reqsList[0].Item1, 255, ThresholdTypes.Binary));
                    var contour = dilated.FindContoursAsArray(RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    var bb = Cv2.MinAreaRect(contour[0]);
                    Cv2.MinMaxLoc(subcomp, out double _, out double maxVal);

                    swaths.Add(new Swath()
                    {
                        name = "swath" + i.ToString(),
                        length = lengths[0],
                        width = Math.Min(bb.Size.Width, bb.Size.Height),
                        im = subcomp,
                        stats = stats[i],
                        centroid = centroids[i],
                        inflectionPoint = new Point(0, 0),
                        lengthPaths = paths,
                        contour = contour[0],
                        boundingBox = bb,
                        maxValue = (int)maxVal
                    });
                }
            });

            string name = await CreateSwathFeatureLayer(rasterLayer);

            await Swath.Export(swaths.ToArray(), name, rasterLayer);

        }

        public static async Task<string> CreateSwathFeatureLayer(RasterLayer rasterLayer)
        {
            string name = "swaths_" + rasterLayer.Name.Split('.')[0];

            List<Attribute> attributes = [
                new Attribute("Name", "Name", AttributeType.TEXT),
                new Attribute("Length", "Length", AttributeType.DOUBLE),
                new Attribute("Width", "Width", AttributeType.DOUBLE),
                new Attribute("MaxValue", "MaxValue", AttributeType.DOUBLE),
                new Attribute("CentroidX", "CentroidX", AttributeType.DOUBLE),
                new Attribute("CentroidY", "CentroidY", AttributeType.DOUBLE),
                new Attribute("InflectionX", "InflectionX", AttributeType.DOUBLE),
                new Attribute("InflectionY", "InflectionY", AttributeType.DOUBLE),
                new Attribute("Top", "Top", AttributeType.DOUBLE),
                new Attribute("Left", "Left", AttributeType.DOUBLE),
                new Attribute("Bottom", "Bottom", AttributeType.DOUBLE),
                new Attribute("Right", "Right", AttributeType.DOUBLE),
            ];

            var layer = await CreateFcWithAttributes(name, FeatureClassType.POLYGON, attributes);

            await QueuedTask.Run(() =>
            {
                var outline = SymbolFactory.Instance.ConstructStroke(ColorFactory.Instance.CreateRGBColor(0, 0, 0, 100), 3.0, SimpleLineStyle.Solid);
                var fillWithOutline = SymbolFactory.Instance.ConstructPolygonSymbol(ColorFactory.Instance.CreateRGBColor(0, 0, 0, 0), SimpleFillStyle.Solid, outline);

                CIMSimpleRenderer rendererp = layer.GetRenderer() as CIMSimpleRenderer;
                rendererp.Symbol = fillWithOutline.MakeSymbolReference();
                layer.SetRenderer(rendererp);
            });



            return name;
        }

        public static Mat ExpandAndDilate(Mat im)
        {
            Mat eim = im.CopyMakeBorder(1, 1, 1, 1, BorderTypes.Constant, 0);

            return eim.Dilate(new Mat(new Size(3, 3), MatType.CV_8UC1, 1));
        }

        public static (double, List<Point>) CheckLengthReq(Mat im, (int, int) req)
        {
            Mat bim = im.Threshold(req.Item1, 255, ThresholdTypes.Binary);

            var subcompImages = ExtractCCImages(im, bim, PixelConnectivity.Connectivity8, req.Item2).Item1;

            foreach (var subcompImage in subcompImages)
            {
                var g = ImageToGraph(subcompImage);

                (double wd, List<Point> path) = WeightedDiameter(g, im);

                if (wd >= (req.Item2 - 1e-5))
                {
                    return (wd, path);
                }
            }
     
            return (-1.0, null);
        }

        public static (List<Mat>, List<int[]>, List<Point2d>) ExtractCCImages(Mat im, Mat bim, PixelConnectivity connectivity, int areaThres)
        {
            List<Mat> croppedImages = [];
            List<int[]> croppedStats = [];
            List<Point2d> croppedCentroids = [];

            Mat labels = new(), stats = new(), centroids = new();

            int num_labels = bim.ConnectedComponentsWithStats(labels, stats, centroids, connectivity: connectivity);

            for (int i = 1; i < num_labels; i++)
            {
                int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);

                if (area >= areaThres)
                {
                    int x = stats.At<int>(i, (int)ConnectedComponentsTypes.Left);
                    int y = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
                    int w = stats.At<int>(i, (int)ConnectedComponentsTypes.Width);
                    int h = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);

                    Mat componentIm = im.SubMat(y, y + h, x, x + w).Clone();

                    Mat mask = labels.SubMat(y, y + h, x, x + w).InRange(i, i);

                    Cv2.BitwiseAnd(componentIm, mask, componentIm);

                    croppedImages.Add(componentIm);

                    croppedStats.Add([x, y, w, h, area]);
                    croppedCentroids.Add(new Point2d(centroids.At<double>(i, 0), centroids.At<double>(i, 1)));
                }
            }

            return (croppedImages, croppedStats, croppedCentroids);
        }

        public static AdjacencyGraph<Point, Edge<Point>> ImageToGraph(Mat im)
        {
            HashSet<Edge<Point>> edges = [];

            for (int i = 0; i < im.Height; i++)
            {
                for (int j = 0; j < im.Width; j++)
                {
                    if (im.At<byte>(i, j) == 0) continue;

                    for (int y = Math.Max(0, i - 1); y < Math.Min(i + 2, im.Height); y++)
                    {

                        for (int x = Math.Max(0, j - 1); x < Math.Min(j + 2, im.Width); x++)
                        {
                            if (im.At<byte>(y, x) == 0 || (y == i && x == j)) continue;

                            edges.Add(new Edge<Point>(new Point(i, j), new Point(y, x)));
                        }
                    }

                }
            }

            return edges.ToAdjacencyGraph<Point, Edge<Point>>();
        }

        public static (double, List<Point>) WeightedDiameter(AdjacencyGraph<Point, Edge<Point>> graph, Mat im)
        {

            Func<Edge<Point>, double> edgeCost = edge => edge.Source.DistanceTo(edge.Target);

            double longest = 0.0;
            (Point, Point) longestPair = (new Point(0, 0), new Point(0, 0));
            List<Point> longestPath = [];

            var dijkstra = new DijkstraShortestPathAlgorithm<Point, Edge<Point>>(graph, edgeCost);

            foreach (Point node in graph.Vertices.Where(v => graph.OutEdges(v).Count() < 8))
            {
                dijkstra.Compute(node);

                var distances = dijkstra.GetDistances();

                foreach (var dist in distances)
                {
                    if (dist.Value > longest)
                    {
                        longestPair = (node, dist.Key);
                        longest = dist.Value;
                    }
                } 
                
            }

            if (longest > 0.0)
            {
                var tryGetPaths = graph.ShortestPathsDijkstra(edgeCost, longestPair.Item1);

                tryGetPaths(longestPair.Item2, out IEnumerable<Edge<Point>> path);

                longestPath.Add(longestPair.Item1);

                foreach (var p in path)
                {
                    longestPath.Add(p.Target);
                }
            }

            return (longest, longestPath);
        }

        public static async Task<bool> RemoveArtifacts(Mat im, RasterLayer rasterLayer, FeatureLayer artifactLayer)
        {
            var polygons = await ReadPolygons(rasterLayer, artifactLayer);

            if (polygons == null) return false;

            im.FillPoly(polygons, 0);

            return true;
        }

        public static void Denoise(Mat im, int thres, int k, int minArea)
        {
            Mat mask = im.Blur(new Size(2 * k + 1, 2 * k + 1));

            mask = mask.InRange(thres, 255);

            Mat mask2 = new(mask.Size(), MatType.CV_8UC1, 0);

            Mat labels = new(), stats = new(), centroids = new();

            int num_labels = mask.ConnectedComponentsWithStats(labels, stats, centroids, connectivity: PixelConnectivity.Connectivity8);

            Parallel.For(1, num_labels, n =>
            {
                if (stats.At<int>(n, (int)ConnectedComponentsTypes.Area) >= minArea)
                {
                    mask2.SetTo(255, labels.InRange(n, n));
                }
            });

            Cv2.BitwiseAnd(im, mask2, im);
        }

        public static Mat ColourScan(Mat im)
        {
            Mat cim = new(im.Size(), MatType.CV_8UC3);

            ForeachPixel(im, (i, j) =>
            {
                cim.Set(i, j, colourTable[im.At<byte>(i, j)]);
            });

            return cim;
        }

        public async static Task<List<List<Point>>> ReadPolygons(RasterLayer rasterLayer, FeatureLayer polygonLayer)
        {
            List<List<Point>> polygons = [];

            try
            {
                await QueuedTask.Run(() => {

                    Raster raster = rasterLayer.GetRaster();

                    using (ArcGIS.Core.Data.Table shp_table = polygonLayer.GetTable())
                    {
                        using (RowCursor rowCursor = shp_table.Search())
                        {
                            while (rowCursor.MoveNext())
                            {
                                using (Feature f = (Feature)rowCursor.Current)
                                {
                                    ArcGIS.Core.Geometry.Polygon polyShape = (ArcGIS.Core.Geometry.Polygon)f.GetShape();
                                    List<Point> polygon = [];

                                    foreach (var p in polyShape.Points)
                                    {
                                        var pixelP = raster.MapToPixel(p.X, p.Y);
                                        polygon.Add(new Point(pixelP.Item1, pixelP.Item2));
                                    }

                                    polygons.Add(polygon);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Invalid Artifact file, must be a polygon shapefile");
                return null;
            }

            return polygons;
        }

        public async static Task<Mat> RasterToImage(RasterLayer rasterLayer)
        {
            Mat im = new Mat();

            await QueuedTask.Run(() =>
            {
                Raster raster = rasterLayer.GetRaster();
                int width = raster.GetWidth();
                int height = raster.GetHeight();

                var pixelBlock = raster.CreatePixelBlock(width, height);
                raster.Read(0, 0, pixelBlock);

                im = new(height, width, MatType.CV_8UC1);

                if (raster.IsInteger())
                {
                    var rasterData = (byte[,])pixelBlock.GetPixelData(0, false);

                    ForeachPixel(im, (i, j) =>
                    {
                        im.Set(i, j, rasterData[j, i]);
                    });
                }
                else
                {
                    var rasterData = (float[,])pixelBlock.GetPixelData(0, false);

                    ForeachPixel(im, (i, j) =>
                    {
                        im.Set(i, j, (byte)rasterData[j, i]);
                    });
                }
            });

            return im;
        }

        // Parallel for
        public static void ForeachPixel(Mat im, Action<int, int> op)
        {
            Parallel.For(0, im.Height, i =>
            {
                for (int j = 0; j < im.Width; j++)
                {
                    op(i, j);
                }
            });
        }

        public readonly static Vec3b[] colourTable = [new Vec3b(255, 255, 255), new Vec3b(255, 204, 153), new Vec3b(255, 153, 0), new Vec3b(102, 255, 0), new Vec3b(102, 255, 0), new Vec3b(0, 204, 0), new Vec3b(0, 204, 0), new Vec3b(0, 153, 0), new Vec3b(0, 153, 0), new Vec3b(0, 102, 0), new Vec3b(0, 102, 0), new Vec3b(51, 255, 255), new Vec3b(51, 255, 255), new Vec3b(51, 255, 255), new Vec3b(51, 255, 255), new Vec3b(51, 255, 255), new Vec3b(0, 204, 255), new Vec3b(0, 204, 255), new Vec3b(0, 204, 255), new Vec3b(0, 204, 255), new Vec3b(0, 204, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 153, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 102, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(0, 0, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(153, 2, 255), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(204, 51, 153), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(153, 0, 102), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100), new Vec3b(100, 100, 100)];
    }
}

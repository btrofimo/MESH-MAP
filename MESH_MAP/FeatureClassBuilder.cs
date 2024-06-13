using ArcGIS.Desktop.Core.Geoprocessing;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Mapping;
using System.Threading;
using System.Windows;
using ArcGIS.Desktop.Editing;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Core.Internal.Geometry;
using ArcGIS.Core.Data.Raster;

namespace MESH_MAP
{
    internal class FeatureClassBuilder
    {
        public static async Task<FeatureLayer> CreateFcWithAttributes(string fcName, FeatureClassType fcType, List<Attribute> attributes, int spatialReference=3857)
        {
            // Create feature class T1
            await CreateFeatureClass(fcName, fcType, spatialReference);
            // double check to see if the layer was added to the map
            var fcLayer = MapView.Active.Map.GetLayersAsFlattenedList().Where((l) => l.Name == fcName).FirstOrDefault() as FeatureLayer;
            if (fcLayer == null)
            {
                MessageBox.Show($@"Unable to find {fcName} in the active map");
                return null;
            }

            var dataSource = await GetDataSource(fcLayer);

            foreach (var attribute in attributes)
            {
                await ExecuteAddFieldToolAsync(fcLayer, new KeyValuePair<string, string>(attribute.Name, attribute.Description), attribute.Type.ToString(), 50);
            }

            return fcLayer;
        }

        public enum AttributeType
        {
            TEXT,
            DOUBLE,
            LONG
        }

        public struct Attribute
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public AttributeType Type { get; set; }

            public Attribute(string name, string description, AttributeType type)
            {
                Name = name;
                Description = description;
                Type = type;
            }
        }

        public enum FeatureClassType
        {
            POINT,
            MULTIPOINT,
            POLYLINE,
            POLYGON
        }

        public static async Task<Geometry> CreatePolygon (OpenCvSharp.Point[] pts, RasterLayer rasterLayer)
        {
            Polygon p = null;
            List<MapPoint> mapPts = [];

            await QueuedTask.Run(() =>
            {
                Raster raster = rasterLayer.GetRaster();

                foreach (var pt in pts)
                {
                    var coordPt = raster.PixelToMap(pt.X, pt.Y);
                    mapPts.Add(MapPointBuilderEx.CreateMapPoint(coordPt.Item1, coordPt.Item2));
                }

                p = PolygonBuilder.CreatePolygon(mapPts.ToArray());
            });

            return p;
        }

        public static async Task AddFeatures(string featureclassName, List<Dictionary<string, object>> features)
        {            
            var layer = MapView.Active.Map.GetLayersAsFlattenedList().Where((l) => l.Name == featureclassName).FirstOrDefault();

            await QueuedTask.Run(() =>
            {
                var editOp = new EditOperation
                {
                    Name = "edit operation"
                };

                foreach (var feature in features)
                {
                    editOp.Create(layer, feature);
                }
                var result = editOp.Execute();

                if (result != true || editOp.IsSucceeded != true)
                {
                    MessageBox.Show("Error: Could not edit feature layer " + featureclassName);
                }
            });

            await Project.Current.SaveEditsAsync();
        }


        public static async Task CreateFeatureClass(string featureclassName, FeatureClassType featureclassType, int spatialReference=3857)
        {
            List<object> arguments = new List<object>
            {
                // store the results in the default geodatabase
                CoreModule.CurrentProject.DefaultGeodatabasePath,
                // name of the feature class
                featureclassName,
                // type of geometry
                featureclassType.ToString(),
                // no template
                "",
                // no z values
                "DISABLED",
                // no m values
                "DISABLED"
            };
            await QueuedTask.Run(() =>
            {
                // spatial reference
                arguments.Add(SpatialReferenceBuilder.CreateSpatialReference(spatialReference));
            });
            IGPResult result = await Geoprocessing.ExecuteToolAsync("CreateFeatureclass_management", Geoprocessing.MakeValueArray(arguments.ToArray()));
        }

        public static async Task<string> GetDataSource(BasicFeatureLayer theLayer)
        {
            try
            {
                return await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    var inTable = theLayer.Name;
                    var table = theLayer.GetTable();
                    var dataStore = table.GetDatastore();
                    var workspaceNameDef = dataStore.GetConnectionString();
                    var workspaceName = workspaceNameDef.Split('=')[1];
                    var fullSpec = System.IO.Path.Combine(workspaceName, inTable);
                    return fullSpec;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return string.Empty;
            }
        }

        public static async Task<string> ExecuteAddFieldToolAsync(
          BasicFeatureLayer theLayer,
          KeyValuePair<string, string> field,
          string fieldType, int? fieldLength = null,
          bool isNullable = true)
        {
            return await QueuedTask.Run(() =>
            {
                try
                {
                    var inTable = theLayer.Name;
                    var table = theLayer.GetTable();
                    var dataStore = table.GetDatastore();
                    var workspaceNameDef = dataStore.GetConnectionString();
                    var workspaceName = workspaceNameDef.Split('=')[1];

                    var fullSpec = System.IO.Path.Combine(workspaceName, inTable);
                    System.Diagnostics.Debug.WriteLine($@"Add {field.Key} from {fullSpec}");

                    var parameters = Geoprocessing.MakeValueArray(fullSpec, field.Key, fieldType.ToUpper(), null, null, fieldLength, field.Value, isNullable ? "NULABLE" : "NON_NULLABLE");
                    var cts = new CancellationTokenSource();
                    var results = Geoprocessing.ExecuteToolAsync("management.AddField", parameters, null, cts.Token,
                          (eventName, o) =>
                          {
                              System.Diagnostics.Debug.WriteLine($@"GP event: {eventName}");
                          });
                    var isFailure = results.Result.IsFailed || results.Result.IsCanceled;
                    return !isFailure ? "Failed" : "Ok";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return ex.ToString();
                }
            });
        }

        public static Task<bool> FeatureClassExistsAsync(string fcName)
        {
            return QueuedTask.Run(() =>
            {
                try
                {
                    using (Geodatabase projectGDB = new Geodatabase(new FileGeodatabaseConnectionPath(new Uri(Project.Current.DefaultGeodatabasePath))))
                    {
                        using (FeatureClass fc = projectGDB.OpenDataset<FeatureClass>(fcName))
                        {
                            return fc != null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($@"FeatureClassExists Error: {ex.ToString()}");
                    return false;
                }
            });
        }

    }
}

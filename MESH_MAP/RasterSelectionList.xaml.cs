using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MESH_MAP
{
    /// <summary>
    /// Interaction logic for RasterSelectionList.xaml
    /// </summary>
    public partial class RasterSelectionList : UserControl
    {
        private IEnumerable<RasterLayer> rLayers;

        public RasterSelectionList()
        {
            InitializeComponent();

            //get current map
            var map = MapView.Active.Map;

            //get all raster layers on map
            rLayers = map.GetLayersAsFlattenedList().OfType<RasterLayer>();

            foreach (var layer in rLayers)
            {
                listBox.Items.Add(layer.Name);
            }
        }

        public List<RasterLayer> GetSelectedLayers()
        {
   
            List<RasterLayer> selectedRasters = new List<RasterLayer>();

            foreach (string name in listBox.SelectedItems)
            {
                foreach (var raster in rLayers)
                {
                    if (raster.Name.Equals(name))
                    {
                        selectedRasters.Add(raster);
                    }
                }
            }
            
            return selectedRasters;
        }

        public bool IsEmpty()
        {
            return listBox.Items.Count == 0;
        }
    }
}

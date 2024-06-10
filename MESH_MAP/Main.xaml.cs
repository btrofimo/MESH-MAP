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

    public partial class Main : ArcGIS.Desktop.Framework.Controls.ProWindow
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Run(object sender, RoutedEventArgs e)
        {
            if (MissingInput()) return;

            var lengthReqs = lengthReqsBox.GetReqs();
            var scans = scansListBox.GetSelectedLayers();
            var artifact_raster = artifactSelectionBox.GetSelectedLayer();


            // TODO: Add code here to run MESH_MAP


        }

        private bool MissingInput()
        {
            if (scansListBox.IsEmpty())
            {
                MessageBox.Show("Please select at least one scan");
                return true;
            }

            if (lengthReqsBox.IsEmpty())
            {
                MessageBox.Show("Please add at least one length requirement.");
                return true;
            }

            return false;
        }
    }
}

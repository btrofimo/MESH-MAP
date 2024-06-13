using ArcGIS.Desktop.Framework.Controls;
using ArcGIS.Desktop.Framework.Threading.Tasks;
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

            artifactSelectionBox.SelectByName("artifact");
        }

        private TextBoxStreamWriter textBoxstreamWriter;

        private async void Run(object sender, RoutedEventArgs e)
        {
            try
            {
                runButton.IsEnabled = false;

                if (MissingInput()) return;

                var lengthReqs = lengthReqsBox.GetReqs();
                var scans = scansListBox.GetSelectedLayers();
                var artifactRaster = artifactSelectionBox.GetSelectedLayer();
                int[] denoiseParams = [denoiseThreshold.GetNum(), denoiseStrength.GetNum(), denoiseMinArea.GetNum()];

                tabControl.SelectedIndex = 1;

                textBoxstreamWriter = new TextBoxStreamWriter(ConsoleTextBox);
                textBoxstreamWriter.RedirectStandardOutput();

                await QueuedTask.Run(() =>
                {
                    MeshAnalysis.RunAnalysis(scans, denoiseParams, artifactRaster, lengthReqs);
                });

                textBoxstreamWriter.StopSpinning();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Something went wrong... :(\n\nError:\n" + ex.ToString());
            }

            runButton.IsEnabled = true;
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

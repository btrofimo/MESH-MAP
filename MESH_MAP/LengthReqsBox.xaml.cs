using ActiproSoftware.Windows.Data;
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
    /// Interaction logic for LengthReqsBox.xaml
    /// </summary>
    public partial class LengthReqsBox : UserControl
    {
        List<(int, int)> reqsList;
        int[] hailSizeBoxes = [1, 2, 4, 6, 8, 10, 15, 20, 30, 40, 50, 75, 100];

        public LengthReqsBox()
        {
            InitializeComponent();

            reqsList = [(10, 40), (30, 2)];

            foreach ((int hailSize, int length) in reqsList)
            {
                lengthReqsListBox.Items.Add(length.ToString() + " Km of at least " + hailSize.ToString() + " mm hail");
            }
        }

        private void AddItem(object sender, RoutedEventArgs e)
        {
            if (hailSizeComboBox.SelectedIndex == -1) return;

            int hailSize = hailSizeBoxes[hailSizeComboBox.SelectedIndex];
            int length = lengthBox.GetNum();

            if (length == 0) return;

            reqsList.Add((hailSize, length));
            reqsList.Sort();

            lengthReqsListBox.Items.Clear();

            foreach ((int hs, int l) in reqsList)
            {
                lengthReqsListBox.Items.Add(l.ToString() + " Km of at least " + hs.ToString() + " mm hail");
            }

            lengthReqsListBox.SelectedIndex = -1;
        }

        private void DeleteItem(object sender, RoutedEventArgs e)
        {
            if (lengthReqsListBox.SelectedIndex == -1) return;

            reqsList.RemoveAt(lengthReqsListBox.SelectedIndex);
            lengthReqsListBox.Items.RemoveAt(lengthReqsListBox.SelectedIndex);
           
        }

        public List<(int, int)> GetReqs()
        {
            return reqsList;
        }

        public bool IsEmpty()
        {
            return reqsList.Count == 0;
        }
    }
}

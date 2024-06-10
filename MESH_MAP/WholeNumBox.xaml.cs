using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for WholeNumBox.xaml
    /// </summary>
    public partial class WholeNumBox : UserControl
    {
        // Dependency Property for DefaultValue
        public static readonly DependencyProperty DefaultValueProperty =
            DependencyProperty.Register("DefaultValue", typeof(int), typeof(WholeNumBox),
                new PropertyMetadata(0, OnDefaultValueChanged));

        public int DefaultValue
        {
            get { return (int)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        public WholeNumBox()
        {
            InitializeComponent();

            // Set the initial value of the TextBox to the DefaultValue
            textBox.Text = DefaultValue.ToString();
        }

        private static void OnDefaultValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (WholeNumBox)d;
            int newValue = (int)e.NewValue;
            control.textBox.Text = newValue.ToString();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Use a regular expression to check if the input is a digit
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Further ensure only digits are present in the TextBox
            var textBox = sender as TextBox;
            if (!Regex.IsMatch(textBox.Text, @"^\d*$"))
            {
                textBox.Text = Regex.Replace(textBox.Text, @"[^\d]", "");
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private static bool IsTextAllowed(string text)
        {
            // Only allow digits
            return Regex.IsMatch(text, @"^\d+$");
        }

        public int GetNum()
        {
            // Normalize the text by removing leading zeros
            string text = textBox.Text.TrimStart('0');

            // If the result is empty, default to zero
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            // Try to parse the integer value
            if (int.TryParse(text, out int result))
            {
                return result;
            }

            // If parsing fails, return zero as a fallback
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;
using ProjectNavi.Tasks;

namespace ProjectNavi.MarkersGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ProjectNavi.Tasks.NavigationEnvironment environment;

        private Dispatcher _dispatcher;

        SolidColorBrush black = new SolidColorBrush(Colors.Black);
        SolidColorBrush white = new SolidColorBrush(Colors.White);
        
        public MainWindow()
        {
            InitializeComponent();

            _dispatcher = this.Dispatcher;

            foreach (var landmark in environment.Landmarks)
            {
                markerNameComboBox.Items.Add(landmark.Name);
            }
        }

        private void createButton_Click(object sender, RoutedEventArgs e)
        {
            Marker marker = new Marker();

            if (idTextBox.Text != null && idTextBox.Text != "")
            {
                int id = Int16.Parse(idTextBox.Text.ToString());

                if (id >= 0 && id < 1024)
                {
                    marker.EncodeValue(id);
                    Trace.WriteLine("Valor " + marker.Lines);

                    if (marker.Lines[0].LineBits[0] == 1) bitA1.Fill = white; else bitA1.Fill = black;
                    if (marker.Lines[0].LineBits[1] == 1) bitA2.Fill = white; else bitA2.Fill = black;
                    if (marker.Lines[0].LineBits[2] == 1) bitA3.Fill = white; else bitA3.Fill = black;
                    if (marker.Lines[0].LineBits[3] == 1) bitA4.Fill = white; else bitA4.Fill = black;
                    if (marker.Lines[0].LineBits[4] == 1) bitA5.Fill = white; else bitA5.Fill = black;

                    if (marker.Lines[1].LineBits[0] == 1) bitB1.Fill = white; else bitB1.Fill = black;
                    if (marker.Lines[1].LineBits[1] == 1) bitB2.Fill = white; else bitB2.Fill = black;
                    if (marker.Lines[1].LineBits[2] == 1) bitB3.Fill = white; else bitB3.Fill = black;
                    if (marker.Lines[1].LineBits[3] == 1) bitB4.Fill = white; else bitB4.Fill = black;
                    if (marker.Lines[1].LineBits[4] == 1) bitB5.Fill = white; else bitB5.Fill = black;

                    if (marker.Lines[2].LineBits[0] == 1) bitC1.Fill = white; else bitC1.Fill = black;
                    if (marker.Lines[2].LineBits[1] == 1) bitC2.Fill = white; else bitC2.Fill = black;
                    if (marker.Lines[2].LineBits[2] == 1) bitC3.Fill = white; else bitC3.Fill = black;
                    if (marker.Lines[2].LineBits[3] == 1) bitC4.Fill = white; else bitC4.Fill = black;
                    if (marker.Lines[2].LineBits[4] == 1) bitC5.Fill = white; else bitC5.Fill = black;

                    if (marker.Lines[3].LineBits[0] == 1) bitD1.Fill = white; else bitD1.Fill = black;
                    if (marker.Lines[3].LineBits[1] == 1) bitD2.Fill = white; else bitD2.Fill = black;
                    if (marker.Lines[3].LineBits[2] == 1) bitD3.Fill = white; else bitD3.Fill = black;
                    if (marker.Lines[3].LineBits[3] == 1) bitD4.Fill = white; else bitD4.Fill = black;
                    if (marker.Lines[3].LineBits[4] == 1) bitD5.Fill = white; else bitD5.Fill = black;

                    if (marker.Lines[4].LineBits[0] == 1) bitE1.Fill = white; else bitE1.Fill = black;
                    if (marker.Lines[4].LineBits[1] == 1) bitE2.Fill = white; else bitE2.Fill = black;
                    if (marker.Lines[4].LineBits[2] == 1) bitE3.Fill = white; else bitE3.Fill = black;
                    if (marker.Lines[4].LineBits[3] == 1) bitE4.Fill = white; else bitE4.Fill = black;
                    if (marker.Lines[4].LineBits[4] == 1) bitE5.Fill = white; else bitE5.Fill = black;
                }
            }
        }

        private void openImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Image"; // Default file name
            dlg.DefaultExt = ".png"; // Default file extension
            dlg.Filter = "Image files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png|All files (*.*)|*.*"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                try
                {
                    // Open image
                    string filename = dlg.FileName;
                    BitmapImage bmi = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));
                    image.Source = bmi;
                }
                catch
                {
                }
            }
        }

        private void printButton_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new System.Windows.Controls.PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                double zoom;

                double markerWidthInCm = markerBackground.ActualWidth * 0.026458333;

                zoom = markerWidthSlider.Value / markerWidthInCm;

                //markerCanvas.Background = Brushes.LightGray;

                markerCanvas.LayoutTransform = new ScaleTransform(zoom, zoom);

                Size pageSize = new Size(printDlg.PrintableAreaWidth - 20, printDlg.PrintableAreaHeight - 20);

                markerCanvas.Measure(pageSize);

                markerCanvas.Arrange(new Rect(10, 10, pageSize.Width, pageSize.Height));

                printDlg.PrintVisual(markerCanvas, "Marker");

                markerCanvas.Background = null;
                markerCanvas.LayoutTransform = null;
            }
        }

    }
}

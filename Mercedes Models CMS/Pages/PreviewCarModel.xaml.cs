using Mercedes_Models_CMS.Helpers;
using Mercedes_Models_CMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace Mercedes_Models_CMS.Pages
{
    /// <summary>
    /// Interaction logic for PreviewCarModel.xaml
    /// </summary>
    public partial class PreviewCarModel : Window
    {
        public PreviewCarModel(CarModel model)
        {
            InitializeComponent();
            ModelName_TextBlock.Text = model.Name;
            HorsePower_TextBlock.Text = model.HorsePower.ToString();
            try
            {
                ModelImage.Source = ImagePathResolver.LoadBitmapForDisplay(model.ImagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message);
            }
            if (!string.IsNullOrEmpty(model.RtfFilePath))
            {
                try
                {
                    FlowDocument flowDoc = new FlowDocument();
                    TextRange textRange = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd);
                    using (var stream = new FileStream(model.RtfFilePath, FileMode.Open))
                    {
                        textRange.Load(stream, DataFormats.Rtf);
                    }
                    DescriptionViewer.Document = flowDoc;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading RTF document: " + ex.Message);
                }
            }
        }

        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}

using Mercedes_Models_CMS.Helpers;
using Mercedes_Models_CMS.Models;
using Microsoft.Win32;
using SharpVectors.Dom.Svg;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static System.Net.Mime.MediaTypeNames;

namespace Mercedes_Models_CMS.Pages
{
    /// <summary>
    /// Interaction logic for AddCarModel.xaml
    /// </summary>
    public partial class AddCarModel : Window
    {
        private const string RtfFolderPath = @"../../../Data/RTFs";
        private const string StartUpDataFolderPath = @"../../../Data/StartUpData";

        ObservableCollection<CarModel> Models { get; set; }
        DataPage DataPage { get; set; }
        CarModel Model {  get; set; }
        private bool _editMode = false;
        private string? _selectedImagePath;
        public AddCarModel()
        {
            InitializeComponent();
        }
        public AddCarModel(ObservableCollection<CarModel> models, DataPage dataPage ) : this()
        {
            Model = null;
            
            FontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            Models = models;
            DataPage = dataPage;
        }
        public AddCarModel(ObservableCollection<CarModel> models, DataPage dataPage, CarModel model) : this(models, dataPage)
        {
            Model = model;
            _editMode = true;
            Add_Button.Content = "SAVE CHANGES";
            ModelName_TextBox.Text = model.Name;
            HorsePower_TextBox.Text = model.HorsePower.ToString();
            try
            {
                TryLoadModelImage(model.ImagePath);
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
                    EditorRichTextBox.Document = flowDoc;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading RTF document: " + ex.Message);
                }
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        private void Browse_Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Fa5Image.Foreground = Brushes.Black;
        }

        private void Browse_Button_MouseLeave(object sender, MouseEventArgs e)
        {
            Fa5Image.Foreground = (Brush)FindResource("AccentGoldBrush");
        }
        private void EditorRichTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            int numberOfWords = 0;

            string text = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd).Text.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                numberOfWords = text.Split(new char[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Length;
            }

            Words_Label.Content = $"Words: {numberOfWords}";
        }
        private void EditorRichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            object fontWeight = EditorRichTextBox.Selection.GetPropertyValue(Inline.FontWeightProperty);
            BoldToggleButton.IsChecked = (fontWeight != DependencyProperty.UnsetValue) && (fontWeight.Equals(FontWeights.Bold));

            object fontFamily = EditorRichTextBox.Selection.GetPropertyValue(Inline.FontFamilyProperty);
            FontFamilyComboBox.SelectedItem = fontFamily;

            object fontSize = EditorRichTextBox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            foreach(ComboBoxItem item in FontSizeComboBox.Items)
            {
                if(item.Content.ToString() == fontSize.ToString())
                {
                    FontSizeComboBox.SelectedItem = item;
                    break;
                }
            }

            object foregroundColor = EditorRichTextBox.Selection.GetPropertyValue(TextElement.ForegroundProperty);

            if (foregroundColor is SolidColorBrush solidColorBrush)
            {
                ColorPickerText.SelectedColor = solidColorBrush.Color;
            }
            object fontItalic = EditorRichTextBox.Selection.GetPropertyValue(Inline.FontStyleProperty);

            ItalicButton.IsChecked = (fontItalic != DependencyProperty.UnsetValue && fontItalic.Equals(FontStyles.Italic));

            object underLine = EditorRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            UnderlineButton.IsChecked = (underLine != DependencyProperty.UnsetValue) &&
                                     (underLine is TextDecorationCollection decorations) &&
                                     decorations.Contains(TextDecorations.Underline[0]);
        }

        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontFamilyComboBox.SelectedItem == null) return;

            var font = (FontFamily)FontFamilyComboBox.SelectedItem;
            if (!EditorRichTextBox.Selection.IsEmpty)
            {
                EditorRichTextBox.Selection.ApplyPropertyValue(Inline.FontFamilyProperty, FontFamilyComboBox.SelectedItem);
            }
            else
            {
                EditorRichTextBox.Focus();
            }
        }

        private void ColorPickerText_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {

            EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush((Color)ColorPickerText.SelectedColor));

        }
        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeComboBox.SelectedItem != null)
            {
                
                if (!EditorRichTextBox.Selection.IsEmpty)
                {

                    ComboBoxItem selectedFontSize = FontSizeComboBox.SelectedItem as ComboBoxItem;

                    if (double.TryParse(selectedFontSize.Content.ToString(), out double FontSize))
                    {
                        EditorRichTextBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, FontSize);
                    }

                }
            }
        }
        private static string SanitizeFileName(string name)
        {
            string sanitized = name;
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(invalid, '_');
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "model" : sanitized.Trim();
        }

        private (string dataRtfPath, string startupRtfPath) SaveRTFFiles()
        {
            Directory.CreateDirectory(RtfFolderPath);
            Directory.CreateDirectory(StartUpDataFolderPath);

            string fileName = $"{SanitizeFileName(ModelName_TextBox.Text.Trim())}.rtf";

            string dataRtfPath = System.IO.Path.Combine(RtfFolderPath, fileName);
            string startupRtfPath = System.IO.Path.Combine(StartUpDataFolderPath, fileName);

            if (_editMode && Model != null && !string.IsNullOrWhiteSpace(Model.RtfFilePath))
            {
                string oldDataRtfPath = Model.RtfFilePath;
                if (!string.Equals(System.IO.Path.GetFullPath(oldDataRtfPath), System.IO.Path.GetFullPath(dataRtfPath), StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldDataRtfPath))
                {
                    File.Delete(oldDataRtfPath);
                }

                string oldStartUpFilePath = System.IO.Path.Combine(StartUpDataFolderPath, System.IO.Path.GetFileName(oldDataRtfPath));
                if (!string.Equals(System.IO.Path.GetFullPath(oldStartUpFilePath), System.IO.Path.GetFullPath(startupRtfPath), StringComparison.OrdinalIgnoreCase)
                    && File.Exists(oldStartUpFilePath))
                {
                    File.Delete(oldStartUpFilePath);
                }
            }

            TextRange textRange = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd);

            using (FileStream stream = new FileStream(dataRtfPath, FileMode.Create))
            {
                textRange.Save(stream, DataFormats.Rtf);
            }

            File.Copy(dataRtfPath, startupRtfPath, true);

            return (dataRtfPath, startupRtfPath);
        }

        private void Browse_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                ModelImage.Source = ImagePathResolver.LoadBitmapForDisplay(openFileDialog.FileName);
            }
        }

        private void TryLoadModelImage(string? imagePath)
        {
            ModelImage.Source = ImagePathResolver.LoadBitmapForDisplay(imagePath);
        }

        private string? ResolveImagePathForSave()
        {
            if (!string.IsNullOrWhiteSpace(_selectedImagePath) && File.Exists(_selectedImagePath))
            {
                return ImagePathResolver.SaveImageToProject(_selectedImagePath, ModelName_TextBox.Text.Trim());
            }

            if (_editMode && Model != null && !string.IsNullOrWhiteSpace(Model.ImagePath))
            {
                return ImagePathResolver.NormalizeForStorage(Model.ImagePath);
            }

            return null;
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {

            if (_editMode == false && ValidateModel())
            {
                var savedRtfPaths = SaveRTFFiles();
                string? imagePath = ResolveImagePathForSave();
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    Image_Error_Label.Content = "Please choose an image from disk.";
                    return;
                }

                Models.Add(new CarModel
                {
                    Name = ModelName_TextBox.Text.Trim(),
                    ImagePath = imagePath,
                    DateAdded = DateTime.Now,
                    RtfFilePath = savedRtfPaths.dataRtfPath,
                    HorsePower = Int32.Parse(HorsePower_TextBox.Text.Trim()),
                    IsSelected = false
                });
                DataPage.PersistCarModels();
                ToastNotification toastNotification = new ToastNotification("Successfully Added New Model", "", Notification.Wpf.NotificationType.Success);
                DataPage.ShowToastNotification(toastNotification);
                this.Close();
            }
            else if (_editMode == true && ValidateModel()) 
            {
                var savedRtfPaths = SaveRTFFiles();
                string? imagePath = ResolveImagePathForSave();
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    Image_Error_Label.Content = "Please choose an image from disk.";
                    return;
                }

                Model.Name = ModelName_TextBox.Text.Trim();
                Model.ImagePath = imagePath;
                Model.DateAdded = DateTime.Now;
                Model.RtfFilePath = savedRtfPaths.dataRtfPath;
                Model.HorsePower = Int32.Parse(HorsePower_TextBox.Text.Trim());
                DataPage.PersistCarModels();
                ToastNotification toastNotification = new ToastNotification("Successfully Changed Existing Model", "", Notification.Wpf.NotificationType.Success);
                DataPage.ShowToastNotification(toastNotification);
                DataPage.DataGridCarModels.Items.Refresh();
                this.Close();
            }
        }
        private bool ValidateModel()
        {
            bool IsValid = true;
            if( ModelImage.Source == null)
            {
                Image_Error_Label.Content = "You must enter an image.";
                IsValid =  false;
            }
            else if (_selectedImagePath == null && _editMode == false)
            {
                Image_Error_Label.Content = "Please choose an image from disk.";
                IsValid = false;
            }
            else
            {
                Image_Error_Label.Content = "";
            }
            if (string.IsNullOrEmpty(ModelName_TextBox.Text))
            {
                
                ModelName_TextBox.BorderBrush = Brushes.Red;
                ModelName_Error_Label.Content = "Please enter Model Name";
                IsValid = false;
            }
            else
            {
                ModelName_TextBox.BorderBrush = (Brush)FindResource("AccentGoldBrush");
                ModelName_Error_Label.Content = "";
            }
            if (int.TryParse(HorsePower_TextBox.Text, out int hp))
            {
                if (hp <= 0)
                {
                    HorsePower_Error_Label.Content = "Horse Power must be a positive whole number";
                    HorsePower_TextBox.BorderBrush = Brushes.Red;
                    IsValid = false;
                }
                else
                {
                    HorsePower_Error_Label.Content = "";
                    HorsePower_TextBox.BorderBrush = (Brush)FindResource("AccentGoldBrush");
                }
            }
            else
            {
                HorsePower_Error_Label.Content = "Please Enter Horse Power";
                HorsePower_TextBox.BorderBrush = Brushes.Red;
                IsValid = false;
            }
            
            string text = new TextRange(EditorRichTextBox.Document.ContentStart, EditorRichTextBox.Document.ContentEnd).Text.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                RichTextBox_Error_Label.Content = "You must enter description of a model";
                IsValid = false;
            }else
            {
                RichTextBox_Error_Label.Content = "";
            }

                return IsValid;
            
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
    
}

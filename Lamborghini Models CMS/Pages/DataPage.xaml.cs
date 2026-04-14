using Mercedes_Models_CMS.Helpers;
using Mercedes_Models_CMS.Models;
using Notification.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ConstrainedExecution;
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
using System.Threading;

namespace Mercedes_Models_CMS.Pages
{
    /// <summary>
    /// Interaction logic for DataPage.xaml
    /// </summary>
    public partial class DataPage : Window
    {
        private const string CarModelsFilePath = "../../../Data/CarModels.xml";
        private const string StartUpDataFolderPath = "../../../Data/StartUpData";
        private NotificationManager _notificationManager = new NotificationManager();
        private ToastNotification? _notification;
        public ObservableCollection<CarModel> CarModels { get; set; }
        private DataIO _serializer = new DataIO();
        private User _user = null;
        public DataPage()
        {
            CarModels = new ObservableCollection<CarModel>();
            InitializeComponent();
            this.ContentRendered += OnLoad;
            CarModels = _serializer.DeSerializeObject<ObservableCollection<CarModel>>(CarModelsFilePath)
                        ?? new ObservableCollection<CarModel>();
            NormalizeImagePaths();
            DataContext = this;
        }

        private void NormalizeImagePaths()
        {
            bool hasChanges = false;

            foreach (var model in CarModels)
            {
                string? normalizedPath = ImagePathResolver.NormalizeForStorage(model.ImagePath);
                if (!string.IsNullOrWhiteSpace(normalizedPath) &&
                    !string.Equals(model.ImagePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    model.ImagePath = normalizedPath;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                PersistCarModels();
            }
        }

        public void PersistCarModels()
        {
            _serializer.SerializeObject(CarModels, CarModelsFilePath);
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            ShowToastNotification(_notification);
        }
        public DataPage(ToastNotification notification,User user) : this()
        {
            _notification = notification;
            if(user.Role == UserRole.VISITOR)
            {
                Add_Button.Visibility = Visibility.Collapsed;
                Delete_Button.Visibility = Visibility.Collapsed;
                dataGridColumnDelete.Visibility = Visibility.Collapsed;
            }
            _user = user;
        }
        public void ShowToastNotification(ToastNotification toastNotification)
        {
            if(toastNotification == null) return;
            _notificationManager.Show(toastNotification.Title,
            toastNotification.Message, toastNotification.Type,
            "DataNotificationArea");
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Logout_Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(new ToastNotification("Successfully logged out","",NotificationType.Success));
            mainWindow.Show();
            this.Close();
        }

        private void checkBoxSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (var item in CarModels)
            {
                if (checkBox.IsChecked == true)
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }
            DataGridCarModels.Items.Refresh();
        }

        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            AddCarModel addCarModel = new AddCarModel(CarModels,this);
            addCarModel.Owner  = this;
            addCarModel.WindowStartupLocation = WindowStartupLocation.Manual;
            addCarModel.Left = this.Left;                 
            addCarModel.Top = this.Top + 80;    

            addCarModel.ShowDialog();   
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            if(_user.Role == UserRole.ADMIN)
            {
                CarModel model = (CarModel)hl.DataContext;
                AddCarModel addCarModel = new AddCarModel(CarModels, this,model);
                addCarModel.Owner = this;
                addCarModel.WindowStartupLocation = WindowStartupLocation.Manual;
                addCarModel.Left = this.Left;
                addCarModel.Top = this.Top + 80;

                addCarModel.ShowDialog();
            }
            else
            {
                CarModel model = (CarModel)hl.DataContext;
                PreviewCarModel previewModel = new PreviewCarModel(model);
                previewModel.Owner = this;
                previewModel.WindowStartupLocation = WindowStartupLocation.Manual;
                previewModel.Left = this.Left;
                previewModel.Top = this.Top + 80;

                previewModel.ShowDialog();
            }
        }

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            List<CarModel> modelsToDelete = CarModels.Where(c => c.IsSelected).ToList();
            if (modelsToDelete.Count == 0)
            {
                return;
            }

            bool deletingAll = modelsToDelete.Count == CarModels.Count;

            string confirmationText = deletingAll
                ? "Are you sure you want to delete all Models?"
                : $"Are you sure you want to delete {modelsToDelete.Count} selected model(s)?";

            MessageBoxResult confirmation = MessageBox.Show(confirmationText, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var model in modelsToDelete)
            {
                CarModels.Remove(model);
            }

            DataGridCarModels.Items.Refresh();

            foreach (var car in modelsToDelete)
            {
                DeleteModelFiles(car);
            }

            PersistCarModels();

            string successMessage = deletingAll
                ? "All models were deleted"
                : "Successfully deleted selected car model(s)";

            ShowToastNotification(new ToastNotification("Success", successMessage, NotificationType.Success));
        }

        private void DeleteModelFiles(CarModel car)
        {
            TryDeleteFile(car.RtfFilePath);

            string? imagePath = ImagePathResolver.ResolveForDisplay(car.ImagePath);
            TryDeleteFile(imagePath);

            string startupRtfFilePath = System.IO.Path.Combine(StartUpDataFolderPath, System.IO.Path.GetFileName(car.RtfFilePath));
            TryDeleteFile(startupRtfFilePath);
        }

        private void TryDeleteFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    return;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }

        bool AreAllSelected()
        {
            if (CarModels == null || CarModels.Count == 0)
                return false;

            foreach(var car in CarModels) {
                if (!car.IsSelected)
                    return false;
            }
            return true;
        }
        private void CheckBoxRow_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var model = checkBox?.DataContext as CarModel;

            if (model != null)
            {
                model.IsSelected = checkBox?.IsChecked == true;
            }
            if (checkBox.IsChecked == false)
                checkBoxSelectAll.IsChecked = false;
        }
    }
    
}

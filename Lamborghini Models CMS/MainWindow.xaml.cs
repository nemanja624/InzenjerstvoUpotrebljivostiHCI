using Mercedes_Models_CMS.Helpers;
using Mercedes_Models_CMS.Models;
using Mercedes_Models_CMS.Pages;
using Notification.Wpf;
using Notification.Wpf.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Mercedes_Models_CMS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotificationManager _notificationManager = new NotificationManager();
        private string _usernameTextBoxPlaceHolder = "Please enter your username";
        public ObservableCollection<User> Users { get; set; }
        private DataIO _serializer = new DataIO();
        private ToastNotification? notification;
        public MainWindow()
        {
            InitializeComponent();
            
            UserNameTextBox.Text = _usernameTextBoxPlaceHolder;
            UserNameTextBox.Focus();

            Users = _serializer.DeSerializeObject<ObservableCollection<User>>("../../../Data/Users.xml");
            if(Users == null)
            {
                Users = new ObservableCollection<User>();
                Users.Add(new User("admin", "admin", UserRole.ADMIN));
                Users.Add(new User("visitor", "visitor", UserRole.VISITOR));
            }

        }
        public MainWindow(ToastNotification notification) : this()
        {
            this.ContentRendered += OnLoad;
            this.notification = notification;

        }

        private void OnLoad(object? sender, EventArgs e)
        {
            ShowToastNotification(notification);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void UserName_GotFocus(object sender, RoutedEventArgs e)
        {
            if(UserNameTextBox.Text.Equals(_usernameTextBoxPlaceHolder))
            {
                UserNameTextBox.Text = "";
            }
        }
        private void UserName_LostFocus (object sender,RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserNameTextBox.Text))
                UserNameTextBox.Text = _usernameTextBoxPlaceHolder;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveDataAsXML();
        }

        private void SaveDataAsXML()
        {
            _serializer.SerializeObject<ObservableCollection<User>>(Users, "../../../Data/Users.xml");
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            string username = UserNameTextBox.Text.Trim();
            User user = Users.FirstOrDefault(u => u.UserName.Equals(username) && u.Password.Equals(PasswordTextBox.Password));
            if (user != null)
            {
                DataPage data = new DataPage(new ToastNotification("Successfully logged in", $"Welcome {username}", NotificationType.Success),user);
                this.Close();
                data.Show();
                
            }
            else
            {
                ShowToastNotification(new ToastNotification("Failed login attempt", "Username or password is invalid", NotificationType.Error));
                PasswordTextBox.BorderBrush = Brushes.Red;
                UserNameTextBox.BorderBrush = Brushes.Red;
                ErrorPassword_Label.Content = "Username or password are incorrect";
            }
        }
        public void ShowToastNotification(ToastNotification toastNotification)
        {
            if (toastNotification == null) return;
            _notificationManager.Show(toastNotification.Title,
            toastNotification.Message, toastNotification.Type,
            "WindowNotificationArea");
        }
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to exit?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (messageBoxResult == MessageBoxResult.Yes)
            {
                SaveDataAsXML();
                this.Close();
            }
        }
        private void EnterKey(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                Login_Button_Click(sender, e);
        }
    }
}
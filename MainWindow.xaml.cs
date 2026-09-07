using System.ComponentModel;
using System.Windows;

namespace FactorialApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        _vm.PropertyChanged += VmOnPropertyChanged;
        Loaded += async (_,__) => await _vm.InitializeAsync();
        UpdateLoginOverlay();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Login(UsernameBox.Text, PasswordBox.Password))
        {
            LoginError.Text = "";
            UpdateLoginOverlay();
        }
        else
        {
            LoginError.Text = "Invalid username or password";
        }
    }

    private void VmOnPropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLoggedIn))
            UpdateLoginOverlay();
    }

    private void UpdateLoginOverlay()
        => LoginOverlay.Visibility =
            _vm.IsLoggedIn ? Visibility.Collapsed : Visibility.Visible;
}
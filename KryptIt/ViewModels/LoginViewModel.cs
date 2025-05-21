using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using KryptIt.Helpers;
using KryptIt.Models;
using KryptIt.Views;

namespace KryptIt.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private readonly AppDbContext _context;

        private bool _isDefaultViewVisible = true;
        public bool IsDefaultViewVisible
        {
            get => _isDefaultViewVisible;
            set
            {
                _isDefaultViewVisible = value;
                OnPropertyChanged(nameof(IsDefaultViewVisible));
            }
        }

        private bool _isRegistrationWindowVisible = false;
        public bool IsRegistrationWindowVisible
        {
            get => _isRegistrationWindowVisible;
            set
            {
                _isRegistrationWindowVisible = value;
                OnPropertyChanged(nameof(IsRegistrationWindowVisible));
            }
        }

        private RegistrationViewModel _registrationViewModel;
        public RegistrationViewModel RegistrationViewModel
        {
            get => _registrationViewModel;
            set
            {
                _registrationViewModel = value;
                OnPropertyChanged();
            }
        }

        public LoginViewModel()
        {
            _context = new AppDbContext();
            LoginCommand = new RelayCommand(Login, CanLogin);
            OpenRegisterCommand = new RelayCommand(o => OpenRegister());
            RegistrationViewModel = new RegistrationViewModel(this);
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        private bool CanLogin(object parameter)
        {
            return !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);
        }

        private void Login(object parameter)
        {
            var user = _context.User.SingleOrDefault(u => u.Username == Username && u.Password == Password);
            if (user != null)
            {
                // Check if 2FA is enabled for this user
                if (user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    // Open 2FA verification window
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TwoFactorWindow twoFactorWindow = new TwoFactorWindow();
                        var viewModel = twoFactorWindow.DataContext as TwoFactorViewModel;
                        viewModel.SetUser(user);
                        twoFactorWindow.Show();
                    });
                }
                else
                {
                    // No 2FA required, proceed to main window
                    SessionManager.CurrentUser = user;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        Application.Current.MainWindow.Close();
                        Application.Current.MainWindow = mainWindow;
                    });
                }
            }
            else
            {
                MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.");
            }
        }

        private void OpenRegister()
        {
            IsDefaultViewVisible = false;
            IsRegistrationWindowVisible = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
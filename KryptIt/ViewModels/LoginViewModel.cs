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

        public LoginViewModel()
        {
            _context = new AppDbContext();
            LoginCommand = new RelayCommand(Login, CanLogin);
            OpenRegisterCommand = new RelayCommand(OpenRegister);
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
            var user = _context.Users.SingleOrDefault(u => u.Username == Username && u.Password == Password);
            if (user != null)
            {
                SessionManager.CurrentUser = user;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Application.Current.MainWindow.Close();
                    Application.Current.MainWindow = mainWindow;
                });
            }
            else
            {
                MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.");
            }
        }

        private void OpenRegister(object parameter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                RegistrationWindow regWindow = new RegistrationWindow();
                regWindow.Show();
                // Optionnel : fermer la fenêtre de login
                Application.Current.MainWindow.Close();
                Application.Current.MainWindow = regWindow;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

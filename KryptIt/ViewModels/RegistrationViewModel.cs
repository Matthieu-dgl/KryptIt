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
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private string _confirmPassword;
        private string _email;
        private readonly AppDbContext _context;

        public RegistrationViewModel(LoginViewModel loginViewModel)
        {
            _context = new AppDbContext();
            RegisterCommand = new RelayCommand(Register, CanRegister);
            CloseRegistrationCommand = new RelayCommand(o => CloseRegistration());
            _loginViewModel = loginViewModel;
        }

        private readonly LoginViewModel _loginViewModel;

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

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ((RelayCommand)RegisterCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand RegisterCommand { get; }

        public ICommand CloseRegistrationCommand { get; }

        private bool CanRegister(object parameter)
        {
            return !string.IsNullOrEmpty(Username) &&
                   !string.IsNullOrEmpty(Password) &&
                   !string.IsNullOrEmpty(ConfirmPassword) &&
                   !string.IsNullOrEmpty(Email) &&
                   Password == ConfirmPassword;
        }

        private void Register(object parameter)
        {
            // Vérifier si l'utilisateur existe déjà
            var existingUser = _context.User.SingleOrDefault(u => u.Username == Username);
            if (existingUser != null)
            {
                MessageBox.Show("Username already exists.");
                return;
            }

            // Créer le nouvel utilisateur
            var user = new User { Username = Username, Password = Password, Email = Email };
            try
            {
                _context.User.Add(user);
                _context.SaveChanges();
                MessageBox.Show("Inscription réussie !");

                // Retour à la page de login
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.Show();
                    Application.Current.MainWindow.Close();
                    Application.Current.MainWindow = loginWindow;
                });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Gérer les erreurs de validation
                var errorMessages = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => e.ErrorMessage);

                MessageBox.Show("Erreur de validation : " + string.Join("\n", errorMessages));
            }
            catch (Exception ex)
            {
                // Gérer d'autres erreurs
                MessageBox.Show("Une erreur s'est produite lors de l'inscription : " + ex.Message);
            }
        }

        private void CloseRegistration()
        {
            _loginViewModel.IsRegistrationWindowVisible = false;
            _loginViewModel.IsDefaultViewVisible = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using KryptIt.Helpers;
using KryptIt.Models;
using KryptIt.Views;
using OtpNet;

namespace KryptIt.ViewModels
{
    public class TwoFactorViewModel : INotifyPropertyChanged
    {
        private string _verificationCode;
        private User _user;

        public string VerificationCode
        {
            get => _verificationCode;
            set
            {
                _verificationCode = value;
                OnPropertyChanged();
                ((RelayCommand)VerifyCommand).RaiseCanExecuteChanged();
            }
        }

        public ICommand VerifyCommand { get; }

        public TwoFactorViewModel()
        {
            VerifyCommand = new RelayCommand(VerifyCode, CanVerify);
            _user = null;
        }

        public void SetUser(User user)
        {
            _user = user;
        }

        private bool CanVerify(object parameter)
        {
            return !string.IsNullOrWhiteSpace(VerificationCode) && VerificationCode.Length == 6;
        }

        private void VerifyCode(object parameter)
        {
            if (_user == null || string.IsNullOrEmpty(_user.TwoFactorSecret))
            {
                MessageBox.Show("Error: User or 2FA secret is missing.");
                return;
            }

            var totp = new Totp(Base32Encoding.ToBytes(_user.TwoFactorSecret));
            bool isValid = totp.VerifyTotp(VerificationCode, out _);

            if (isValid)
            {
                // Set authenticated user
                SessionManager.CurrentUser = _user;

                // Close this window and open main window
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Window.GetWindow(Application.Current.MainWindow).Close();
                    Application.Current.MainWindow = mainWindow;
                });
            }
            else
            {
                MessageBox.Show("Invalid verification code. Please try again.");
                VerificationCode = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
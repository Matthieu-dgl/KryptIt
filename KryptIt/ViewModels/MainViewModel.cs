using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using KryptIt.Helpers;
using KryptIt.Models;

namespace KryptIt.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PasswordEntry> AllPasswords { get; set; }

        private ObservableCollection<PasswordEntry> _filteredPasswords;
        public ObservableCollection<PasswordEntry> FilteredPasswords
        {
            get => _filteredPasswords;
            set
            {
                _filteredPasswords = value;
                OnPropertyChanged(nameof(FilteredPasswords));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ExecuteSearch();
            }
        }

        private string _newAccount;
        public string NewAccount
        {
            get => _newAccount;
            set
            {
                _newAccount = value;
                OnPropertyChanged(nameof(NewAccount));
                ((RelayCommand)AddPasswordCommand).RaiseCanExecuteChanged();
            }
        }

        private string _newPassword;
        public string NewPassword
        {
            get => _newPassword;
            set
            {
                _newPassword = value;
                OnPropertyChanged(nameof(NewPassword));
                ((RelayCommand)AddPasswordCommand).RaiseCanExecuteChanged();
            }
        }

        private string _newURL;
        public string NewURL
        {
            get => _newURL;
            set
            {
                _newURL = value;
                OnPropertyChanged(nameof(NewURL));
            }
        }

        private PasswordEntry _selectedPassword;
        public PasswordEntry SelectedPassword
        {
            get => _selectedPassword;
            set
            {
                _selectedPassword = value;
                OnPropertyChanged(nameof(SelectedPassword));
            }
        }

        public ICommand OpenInNewTabCommand { get; }
        public ICommand CopyUsernameCommand { get; }
        public ICommand CopyPasswordCommand { get; }
        public ICommand CopyWebsiteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddPasswordCommand { get; }

        // Clé de chiffrement
        private const string EncryptionKey = "MaCleSecrete123456";

        public MainViewModel()
        {
            OpenInNewTabCommand = new RelayCommand(o => OpenInNewTab(), o => SelectedPassword != null);
            CopyUsernameCommand = new RelayCommand(o => CopyUsername(), o => SelectedPassword != null);
            CopyPasswordCommand = new RelayCommand(o => CopyPassword(), o => SelectedPassword != null);
            CopyWebsiteCommand = new RelayCommand(o => CopyWebsite(), o => SelectedPassword != null);
            DeleteCommand = new RelayCommand(o => DeletePassword(), o => SelectedPassword != null);
            AddPasswordCommand = new RelayCommand(o => AddPassword(), o => !string.IsNullOrWhiteSpace(NewAccount) && !string.IsNullOrWhiteSpace(NewPassword));

            AllPasswords = new ObservableCollection<PasswordEntry>();
            FilteredPasswords = new ObservableCollection<PasswordEntry>();

            LoadPasswordsFromDatabase();
            ExecuteSearch();
        }

        private void OpenInNewTab()
        {
            if (SelectedPassword != null && Uri.IsWellFormedUriString(SelectedPassword.SiteName, UriKind.Absolute))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = SelectedPassword.SiteName,
                        UseShellExecute = true // Utilise le navigateur par défaut
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de l'ouverture du lien : " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("L'URL est invalide ou non définie.");
            }
        }

        private void CopyUsername()
        {
            Clipboard.SetText(SelectedPassword?.Login);
        }

        private void CopyPassword()
        {
            Clipboard.SetText(SelectedPassword?.EncryptedPassword);
        }

        private void CopyWebsite()
        {
            Clipboard.SetText(SelectedPassword?.SiteName);
        }

        private void DeletePassword()
        {
            if (SelectedPassword != null)
            {
                using (var context = new AppDbContext())
                {
                    var passwordEntry = context.PasswordEntry.SingleOrDefault(p => p.Id == SelectedPassword.Id);
                    if (passwordEntry != null)
                    {
                        context.PasswordEntry.Remove(passwordEntry);
                        context.SaveChanges();
                    }
                }

                AllPasswords.Remove(SelectedPassword);
                ExecuteSearch();
            }
        }

        private void LoadPasswordsFromDatabase()
        {
            using (var context = new AppDbContext())
            {
                var passwords = context.PasswordEntry.Where(p => p.UserId == SessionManager.CurrentUser.Id).ToList();
                foreach (var password in passwords)
                {
                    AllPasswords.Add(password);
                }
            }
        }

        public void AddPassword()
        {
            string encryptedPassword = SecurityHelper.Encrypt(NewPassword, EncryptionKey);
            var entry = new PasswordEntry
            {
                UserId = SessionManager.CurrentUser.Id,
                SiteName = NewURL,
                Login = NewAccount,
                EncryptedPassword = encryptedPassword,
                CreatedAt = DateTime.Now
            };

            AllPasswords.Add(entry);

            using (var context = new AppDbContext())
            {
                context.PasswordEntry.Add(entry);
                context.SaveChanges();
            }

            ExecuteSearch();

            NewAccount = string.Empty;
            NewPassword = string.Empty;
            NewURL = string.Empty;

            OnPropertyChanged(nameof(NewAccount));
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(NewURL));
        }

        public void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPasswords = new ObservableCollection<PasswordEntry>(AllPasswords);
            }
            else
            {
                var filtered = AllPasswords.Where(p => p.Login.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0);
                FilteredPasswords = new ObservableCollection<PasswordEntry>(filtered);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

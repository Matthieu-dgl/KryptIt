using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using KryptIt.Helpers;
using KryptIt.Models;
using KryptIt.Views;
using WindowsInput;
using WindowsInput.Native;
using System.Runtime.InteropServices;
using OtpNet;
using QRCoder;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace KryptIt.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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

        private ObservableCollection<Tag> _availableTags;
        public ObservableCollection<Tag> AvailableTags
        {
            get => _availableTags;
            set
            {
                _availableTags = value;
                OnPropertyChanged(nameof(AvailableTags));
            }
        }

        private ObservableCollection<Tag> _newEntryTags = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> NewEntryTags
        {
            get => _newEntryTags;
            set
            {
                _newEntryTags = value;
                OnPropertyChanged(nameof(NewEntryTags));
            }
        }

        private Tag _selectedTag;
        public Tag SelectedTag
        {
            get => _selectedTag;
            set
            {
                _selectedTag = value;
                OnPropertyChanged(nameof(SelectedTag));
                ((RelayCommand)AddTagCommand).RaiseCanExecuteChanged();
            }
        }

        private Tag _selectedFilterTag;
        public Tag SelectedFilterTag
        {
            get => _selectedFilterTag;
            set
            {
                _selectedFilterTag = value;
                OnPropertyChanged(nameof(SelectedFilterTag));
                FilterEntriesByTag();
            }
        }

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

        private bool _isSettingsWindowVisible = false;
        public bool IsSettingsWindowVisible
        {
            get => _isSettingsWindowVisible;
            set
            {
                _isSettingsWindowVisible = value;
                OnPropertyChanged(nameof(IsSettingsWindowVisible));
            }
        }

        private bool _isTagPopupOpen;
        public bool IsTagPopupOpen
        {
            get => _isTagPopupOpen;
            set
            {
                _isTagPopupOpen = value;
                OnPropertyChanged(nameof(IsTagPopupOpen));
            }
        }

        private bool _isTwoFactorEnabled;
        public bool IsTwoFactorEnabled
        {
            get => _isTwoFactorEnabled;
            set
            {
                _isTwoFactorEnabled = value;
                OnPropertyChanged(nameof(IsTwoFactorEnabled));

                if (_isTwoFactorEnabled && string.IsNullOrEmpty(TwoFactorSecret))
                {
                    using (var context = new AppDbContext())
                    {
                        var user = context.User.FirstOrDefault(u => u.Id == SessionManager.CurrentUser.Id);
                        if (user != null && !string.IsNullOrEmpty(user.TwoFactorSecret))
                        {
                            TwoFactorSecret = user.TwoFactorSecret;
                        }
                        else
                        {
                            GenerateTwoFactorSecret();
                        }
                    }
                }
                else if (!_isTwoFactorEnabled)
                {
                    DisableTwoFactorAuthentication();
                }
            }
        }

        private string _twoFactorSecret;
        public string TwoFactorSecret
        {
            get => _twoFactorSecret;
            set
            {
                _twoFactorSecret = value;
                OnPropertyChanged(nameof(TwoFactorSecret));
            }
        }

        private BitmapImage _qrCodeImageSource;
        public BitmapImage QrCodeImageSource
        {
            get => _qrCodeImageSource;
            set
            {
                _qrCodeImageSource = value;
                OnPropertyChanged(nameof(QrCodeImageSource));
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

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged(nameof(IsPopupOpen));
            }
        }

        public ICommand OpenInNewTabCommand { get; }
        public ICommand CopyUsernameCommand { get; }
        public ICommand CopyPasswordCommand { get; }
        public ICommand CopyWebsiteCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddPasswordCommand { get; }
        public ICommand OpenPopupCommand { get; }
        public ICommand ClosePopupCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand CloseSettingsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AutoFillCommand { get; }
        public ICommand AddTagCommand { get; }
        public ICommand AddTagToNewEntryCommand { get; }
        public ICommand OpenTagPopupCommand { get; }
        public ICommand CloseTagPopupCommand { get; }
        public ICommand RemoveTagCommand { get; }

        public ICommand SaveSettingsCommand { get; }

        // Clé de chiffrement
        private const string EncryptionKey = "Ax7p2Dx5MM87s5D22Gz";

        public MainViewModel()
        {
            OpenInNewTabCommand = new RelayCommand(o => OpenInNewTab(), o => SelectedPassword != null);
            CopyUsernameCommand = new RelayCommand(o => CopyUsername(), o => SelectedPassword != null);
            CopyPasswordCommand = new RelayCommand(o => CopyPassword(), o => SelectedPassword != null);
            CopyWebsiteCommand = new RelayCommand(o => CopyWebsite(), o => SelectedPassword != null);
            DeleteCommand = new RelayCommand(o => DeletePassword(), o => SelectedPassword != null);
            AddPasswordCommand = new RelayCommand(o => AddPassword(), o => !string.IsNullOrWhiteSpace(NewAccount) && !string.IsNullOrWhiteSpace(NewPassword));
            
            AddTagCommand = new RelayCommand(o => AddTagToPassword(), o => SelectedPassword != null && SelectedTag != null);
            AddTagToNewEntryCommand = new RelayCommand(o => AddTagToNewEntry(), o => SelectedTag != null);
            RemoveTagCommand = new RelayCommand(RemoveTagFromPassword);

            OpenPopupCommand = new RelayCommand(o => IsPopupOpen = true);
            ClosePopupCommand = new RelayCommand(o => IsPopupOpen = false);

            OpenTagPopupCommand = new RelayCommand(o => IsTagPopupOpen = true, o => SelectedPassword != null);
            CloseTagPopupCommand = new RelayCommand(o => IsTagPopupOpen = false);

            OpenSettingsCommand = new RelayCommand(o => OpenSettings());
            CloseSettingsCommand = new RelayCommand(o => CloseSettings());
            SaveSettingsCommand = new RelayCommand(o => SaveSettings());

            AllPasswords = new ObservableCollection<PasswordEntry>();
            FilteredPasswords = new ObservableCollection<PasswordEntry>();

            LogoutCommand = new RelayCommand(o => Logout());

            AutoFillCommand = new RelayCommand(o => AutoFill());

            LoadPasswordsFromDatabase();
            LoadAvailableTags();
            LoadUserSettings();
            ExecuteSearch();
        }

        private void LoadUserSettings()
        {
            // Check if current user has 2FA enabled
            using (var context = new AppDbContext())
            {
                var user = context.User.FirstOrDefault(u => u.Id == SessionManager.CurrentUser.Id);
                if (user != null)
                {
                    IsTwoFactorEnabled = user.TwoFactorEnabled;
                    if (IsTwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecret))
                    {
                        TwoFactorSecret = user.TwoFactorSecret;
                        GenerateQrCodeFromSecret(TwoFactorSecret);
                    }
                }
            }
        }

        private void GenerateQrCodeFromSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret))
                return;

            try
            {
                // Generate the URI for the QR code
                string issuer = "KryptIt";
                string uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(SessionManager.CurrentUser.Username)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

                // Generate the QR code
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                // Convert the QR code to an image
                Bitmap qrCodeImage = qrCode.GetGraphic(20);

                // Convert to BitmapImage for WPF
                using (MemoryStream memory = new MemoryStream())
                {
                    qrCodeImage.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    QrCodeImageSource = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating QR code: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            using (var context = new AppDbContext())
            {
                var user = context.User.FirstOrDefault(u => u.Id == SessionManager.CurrentUser.Id);
                if (user != null)
                {
                    user.TwoFactorEnabled = IsTwoFactorEnabled;
                    user.TwoFactorSecret = TwoFactorSecret;
                    context.SaveChanges();
                    MessageBox.Show("Settings saved successfully.");
                }
            }

            CloseSettings();
        }

        public void AutoFillLogin(string username, string password)
        {
            IntPtr hWnd = FindWindow("Chrome_WidgetWin_1", null);
            if (hWnd != IntPtr.Zero)
            {
                SetForegroundWindow(hWnd);

                var sim = new InputSimulator();

                sim.Keyboard.TextEntry(username);
                sim.Keyboard.KeyPress(VirtualKeyCode.TAB);

                sim.Keyboard.TextEntry(password);
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            else
            {
                MessageBox.Show("Navigateur non trouvé.");
            }
        }
        private void AutoFill()
        {
            if (SelectedPassword != null)
            {
                string decryptedPassword = SecurityHelper.Decrypt(SelectedPassword.EncryptedPassword, EncryptionKey);
                AutoFillLogin(SelectedPassword.Login, decryptedPassword);
            }
        }
        private void AddTagToNewEntry()
        {
            if (SelectedTag != null && !NewEntryTags.Contains(SelectedTag))
            {
                NewEntryTags.Add(SelectedTag);
            }
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
                        UseShellExecute = true
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

        private void GenerateTwoFactorSecret()
        {
            // Generate a unique secret
            var key = KeyGeneration.GenerateRandomKey(20);
            TwoFactorSecret = Base32Encoding.ToString(key);

            // Generate QR code from the secret
            GenerateQrCodeFromSecret(TwoFactorSecret);

            // Update the user in the database
            using (var context = new AppDbContext())
            {
                var user = context.User.FirstOrDefault(u => u.Id == SessionManager.CurrentUser.Id);
                if (user != null)
                {
                    user.TwoFactorSecret = TwoFactorSecret;
                    context.SaveChanges();
                }
            }
        }

        private void DisableTwoFactorAuthentication()
        {
            // Désactiver la 2FA pour l'utilisateur
            using (var context = new AppDbContext())
            {
                var user = context.User.SingleOrDefault(u => u.Id == SessionManager.CurrentUser.Id);
                if (user != null)
                {
                    user.TwoFactorEnabled = false;
                    user.TwoFactorSecret = null;
                    context.SaveChanges();
                }
            }
        }

        private void Logout()
        {
            SessionManager.CurrentUser = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                Application.Current.MainWindow.Close();
                Application.Current.MainWindow = loginWindow;
            });
        }

        private void LoadPasswordsFromDatabase()
        {
            using (var context = new AppDbContext())
            {
                var passwords = context.PasswordEntry
                    .Include("PasswordEntryTag.Tag")
                    .Where(p => p.UserId == SessionManager.CurrentUser.Id)
                    .ToList();

                AllPasswords = new ObservableCollection<PasswordEntry>(passwords);
            }
        }

        private void LoadAvailableTags()
        {
            using (var context = new AppDbContext())
            {
                var tags = context.Tag.ToList();
                tags.Insert(0, new Tag { Id = 0, TagName = "All" });
                AvailableTags = new ObservableCollection<Tag>(tags);
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

            using (var context = new AppDbContext())
            {
                context.PasswordEntry.Add(entry);
                context.SaveChanges();

                foreach (var tag in NewEntryTags)
                {
                    var passwordEntryTag = new PasswordEntryTag
                    {
                        PasswordEntryId = entry.Id,
                        TagId = tag.Id
                    };
                    context.PasswordEntryTag.Add(passwordEntryTag);
                }

                context.SaveChanges();
            }

            AllPasswords.Add(entry);
            ExecuteSearch();

            NewAccount = string.Empty;
            NewPassword = string.Empty;
            NewURL = string.Empty;
            NewEntryTags.Clear();
            OnPropertyChanged(nameof(NewAccount));
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(NewURL));
            IsPopupOpen = false;
        }

        private void AddTagToPassword()
        {
            if (SelectedPassword != null && SelectedTag != null)
            {
                using (var context = new AppDbContext())
                {
                    // Vérifiez si le tag est déjà associé à l'entrée
                    var existingEntry = context.PasswordEntryTag
                        .FirstOrDefault(pet => pet.PasswordEntryId == SelectedPassword.Id && pet.TagId == SelectedTag.Id);

                    if (existingEntry == null)
                    {
                        var passwordEntryTag = new PasswordEntryTag
                        {
                            PasswordEntryId = SelectedPassword.Id,
                            TagId = SelectedTag.Id
                        };

                        context.PasswordEntryTag.Add(passwordEntryTag);
                        context.SaveChanges();

                        // Ajouter le tag à la liste en mémoire
                        SelectedPassword.PasswordEntryTag.Add(passwordEntryTag);

                        // Notifier que les tags ont changé
                        OnPropertyChanged(nameof(SelectedPassword));
                        OnPropertyChanged(nameof(SelectedPassword.TagNames));

                        LoadPasswordsFromDatabase();
                        MessageBox.Show($"Le tag '{SelectedTag.TagName}' a été ajouté à l'entrée sélectionnée.");
                    }
                    else
                    {
                        MessageBox.Show($"Le tag '{SelectedTag.TagName}' est déjà associé à cette entrée.");
                    }
                }
            }
        }
        private void RemoveTagFromPassword(object parameter)
        {
            if (parameter is PasswordEntryTag passwordEntryTag && SelectedPassword != null)
            {
                using (var context = new AppDbContext())
                {
                    var entryToRemove = context.PasswordEntryTag
                        .FirstOrDefault(pet => pet.PasswordEntryId == passwordEntryTag.PasswordEntryId && pet.TagId == passwordEntryTag.TagId);

                    if (entryToRemove != null)
                    {
                        context.PasswordEntryTag.Remove(entryToRemove);
                        context.SaveChanges();

                        // Supprimer le tag de la liste en mémoire
                        SelectedPassword.PasswordEntryTag.Remove(passwordEntryTag);

                        // Notifier que les tags ont changé
                        OnPropertyChanged(nameof(SelectedPassword));
                        OnPropertyChanged(nameof(SelectedPassword.TagNames));
                        LoadPasswordsFromDatabase();

                        MessageBox.Show($"Le tag '{passwordEntryTag.Tag.TagName}' a été supprimé.");
                    }
                }
            }
        }

        private void FilterEntriesByTag()
        {
            if (SelectedFilterTag == null || SelectedFilterTag.TagName == "All")
            {
                // Si aucun tag n'est sélectionné ou si "All" est sélectionné, afficher toutes les entrées
                FilteredPasswords = new ObservableCollection<PasswordEntry>(AllPasswords);
            }
            else
            {
                // Filtrer les entrées qui contiennent le tag sélectionné
                var filtered = AllPasswords.Where(p => p.PasswordEntryTag.Any(pet => pet.TagId == SelectedFilterTag.Id));
                FilteredPasswords = new ObservableCollection<PasswordEntry>(filtered);
            }
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

        private void OpenSettings()
        {
            IsDefaultViewVisible = false;
            IsSettingsWindowVisible = true;
        }

        private void CloseSettings()
        {
            IsSettingsWindowVisible = false;
            IsDefaultViewVisible = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

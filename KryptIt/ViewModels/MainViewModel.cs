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
using System.Data.Entity;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Collections.Generic;


namespace KryptIt.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

        private bool _isSharePopupOpen;
        public bool IsSharePopupOpen
        {
            get => _isSharePopupOpen;
            set { _isSharePopupOpen = value; OnPropertyChanged(nameof(IsSharePopupOpen)); }
        }

        private ObservableCollection<User> _otherUsers;
        public ObservableCollection<User> OtherUsers
        {
            get => _otherUsers;
            set { _otherUsers = value; OnPropertyChanged(nameof(OtherUsers)); }
        }

        private User _selectedUserToShare;
        public User SelectedUserToShare
        {
            get => _selectedUserToShare;
            set
            {
                _selectedUserToShare = value;
                OnPropertyChanged(nameof(SelectedUserToShare));
                ((RelayCommand)SharePasswordCommand).RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<PasswordEntry> _sharedPasswords;
        public ObservableCollection<PasswordEntry> SharedPasswords
        {
            get => _sharedPasswords;
            set { _sharedPasswords = value; OnPropertyChanged(nameof(SharedPasswords)); }
        }

        private ObservableCollection<PasswordEntry> GetUserPasswords()
        {
            return AllPasswords;
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
        public ICommand OpenSharePopupCommand { get; }
        public ICommand CloseSharePopupCommand { get; }
        public ICommand SharePasswordCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportXmlCommand { get; }
        public ICommand ImportCommand { get; }

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

            OpenSharePopupCommand = new RelayCommand(o => OpenSharePopup(), o => SelectedPassword != null);
            CloseSharePopupCommand = new RelayCommand(o => IsSharePopupOpen = false);
            SharePasswordCommand = new RelayCommand(o => SharePassword(), o => SelectedUserToShare != null && SelectedPassword != null);

            OpenSettingsCommand = new RelayCommand(o => OpenSettings());
            CloseSettingsCommand = new RelayCommand(o => CloseSettings());
            SaveSettingsCommand = new RelayCommand(o => SaveSettings());

            AllPasswords = new ObservableCollection<PasswordEntry>();
            FilteredPasswords = new ObservableCollection<PasswordEntry>();

            LogoutCommand = new RelayCommand(o => Logout());

            AutoFillCommand = new RelayCommand(o => AutoFill());

            ExportCsvCommand = new RelayCommand(o => ExportCsv());
            ExportXmlCommand = new RelayCommand(o => ExportXml());
            ImportCommand = new RelayCommand(o => Import());

            LoadPasswordsFromDatabase();
            LoadSharedPasswords();
            LoadAvailableTags();
            LoadUserSettings();
            ExecuteSearch();
        }

        private void LoadUserSettings()
        {
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
                string issuer = "KryptIt";
                string uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(SessionManager.CurrentUser.Username)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                Bitmap qrCodeImage = qrCode.GetGraphic(20);

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
            var mainWindow = Application.Current.MainWindow;
            var mainWindowHandle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
            ShowWindow(mainWindowHandle, SW_MINIMIZE);

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
            if (SelectedPassword != null)
            {
                string decryptedPassword = SecurityHelper.Decrypt(SelectedPassword.EncryptedPassword, EncryptionKey);
                Clipboard.SetText(decryptedPassword);
            }
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
            var key = KeyGeneration.GenerateRandomKey(20);
            TwoFactorSecret = Base32Encoding.ToString(key);

            GenerateQrCodeFromSecret(TwoFactorSecret);

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

        private void LoadSharedPasswords()
        {
            using (var context = new AppDbContext())
            {
                var shared = context.SharedPassword
                    .Where(sp => sp.UserId == SessionManager.CurrentUser.Id)
                    .Select(sp => sp.PasswordEntry)
                    .Include("PasswordEntryTag.Tag")
                    .ToList();

                SharedPasswords = new ObservableCollection<PasswordEntry>(shared);
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

                        SelectedPassword.PasswordEntryTag.Add(passwordEntryTag);

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

                        SelectedPassword.PasswordEntryTag.Remove(passwordEntryTag);

                        OnPropertyChanged(nameof(SelectedPassword));
                        OnPropertyChanged(nameof(SelectedPassword.TagNames));
                        LoadPasswordsFromDatabase();

                        MessageBox.Show($"Le tag '{passwordEntryTag.Tag.TagName}' a été supprimé.");
                    }
                }
            }
        }

        private void OpenSharePopup()
        {
            LoadOtherUsers();
            IsSharePopupOpen = true;
        }

        private void LoadOtherUsers()
        {
            using (var context = new AppDbContext())
            {
                var users = context.User
                    .Where(u => u.Id != SessionManager.CurrentUser.Id)
                    .ToList();
                OtherUsers = new ObservableCollection<User>(users);
            }
        }

        private void SharePassword()
        {
            if (SelectedUserToShare == null || SelectedPassword == null)
                return;

            using (var context = new AppDbContext())
            {
                var alreadyShared = context.SharedPassword
                    .Any(sp => sp.PasswordEntryId == SelectedPassword.Id && sp.UserId == SelectedUserToShare.Id);

                if (alreadyShared)
                {
                    MessageBox.Show("Ce mot de passe est déjà partagé avec cet utilisateur.");
                    return;
                }

                var shared = new SharedPassword
                {
                    PasswordEntryId = SelectedPassword.Id,
                    UserId = SelectedUserToShare.Id,
                    Permission = true
                };
                context.SharedPassword.Add(shared);
                context.SaveChanges();
            }

            MessageBox.Show("Mot de passe partagé !");
            IsSharePopupOpen = false;
        }


        private void FilterEntriesByTag()
        {
            if (SelectedFilterTag == null || SelectedFilterTag.TagName == "All")
            {
                FilteredPasswords = new ObservableCollection<PasswordEntry>(AllPasswords);
            }
            else
            {
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

        private void ExportCsv()
        {
            var passwords = GetUserPasswords();
            if (passwords == null || passwords.Count == 0)
            {
                MessageBox.Show("Aucun mot de passe à exporter.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "passwords.csv"
            };
            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("SiteName,Login,EncryptedPassword,Description,CreatedAt");
                foreach (var entry in passwords)
                {
                    sb.AppendLine($"\"{entry.SiteName}\",\"{entry.Login}\",\"{entry.EncryptedPassword}\",\"{entry.Description}\",\"{entry.CreatedAt:yyyy-MM-dd HH:mm:ss}\"");
                }
                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Export CSV terminé !");
            }
        }

        private void ExportXml()
        {
            var passwords = GetUserPasswords();
            if (passwords == null || passwords.Count == 0)
            {
                MessageBox.Show("Aucun mot de passe à exporter.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                FileName = "passwords.xml"
            };
            if (dialog.ShowDialog() == true)
            {
                var exportList = passwords.Select(entry => new PasswordEntryExportDto
                {
                    SiteName = entry.SiteName,
                    Login = entry.Login,
                    EncryptedPassword = entry.EncryptedPassword,
                    Description = entry.Description,
                    CreatedAt = entry.CreatedAt
                }).ToList();

                var serializer = new XmlSerializer(typeof(List<PasswordEntryExportDto>));
                using (var stream = new FileStream(dialog.FileName, FileMode.Create))
                {
                    serializer.Serialize(stream, exportList);
                }
                MessageBox.Show("Export XML terminé !");
            }
        }

        private void Import()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|XML files (*.xml)|*.xml",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true)
            {
                var ext = Path.GetExtension(dialog.FileName).ToLower();
                if (ext == ".csv")
                    ImportFromCsv(dialog.FileName);
                else if (ext == ".xml")
                    ImportFromXml(dialog.FileName);
            }
        }

        private void ImportFromCsv(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length <= 1) return;

            using (var context = new AppDbContext())
            {
                foreach (var line in lines.Skip(1))
                {
                    var fields = line.Split(',').Select(f => f.Trim('"')).ToArray();
                    if (fields.Length < 6) continue;

                    var entry = new PasswordEntry
                    {
                        UserId = SessionManager.CurrentUser.Id,
                        SiteName = fields[0],
                        Login = fields[1],
                        EncryptedPassword = fields[2],
                        Description = fields[3],
                        CreatedAt = DateTime.TryParse(fields[4], out var dt) ? dt : DateTime.Now
                    };
                    context.PasswordEntry.Add(entry);
                }
                context.SaveChanges();
            }
            LoadPasswordsFromDatabase();
            MessageBox.Show("Import CSV terminé !");
        }

        private void ImportFromXml(string filePath)
        {
            var serializer = new XmlSerializer(typeof(List<PasswordEntryExportDto>));
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var entries = (List<PasswordEntryExportDto>)serializer.Deserialize(stream);
                using (var context = new AppDbContext())
                {
                    foreach (var dto in entries)
                    {
                        var entry = new PasswordEntry
                        {
                            UserId = SessionManager.CurrentUser.Id,
                            SiteName = dto.SiteName,
                            Login = dto.Login,
                            EncryptedPassword = dto.EncryptedPassword,
                            Description = dto.Description,
                            CreatedAt = dto.CreatedAt
                        };
                        context.PasswordEntry.Add(entry);
                    }
                    context.SaveChanges();
                }
            }
            LoadPasswordsFromDatabase();
            MessageBox.Show("Import XML terminé !");
        }

    }
}

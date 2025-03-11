using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using KryptIt.Helpers;
using KryptIt.Models;

namespace KryptIt.ViewModels
{
    public class PasswordViewModel : INotifyPropertyChanged
    {
        // Collection de tous les mots de passe
        public ObservableCollection<PasswordEntry> AllPasswords { get; set; }

        // Collection filtrée selon la recherche
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

        // Texte de recherche
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ExecuteSearch(); // Rechercher dès que le texte change
            }
        }

        // Champs pour un nouveau mot de passe
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

        // Commandes
        public ICommand AddPasswordCommand { get; }
        public ICommand SearchCommand { get; }

        // Clé de chiffrement
        private const string EncryptionKey = "MaCleSecrete123456"; // Clé de 16 caractères

        public PasswordViewModel()
        {
            AllPasswords = new ObservableCollection<PasswordEntry>();
            FilteredPasswords = new ObservableCollection<PasswordEntry>();

            // Commande pour ajouter un mot de passe
            AddPasswordCommand = new RelayCommand(o => AddPassword(), o =>
                !string.IsNullOrWhiteSpace(NewAccount) && !string.IsNullOrWhiteSpace(NewPassword));

            // Commande pour la recherche
            SearchCommand = new RelayCommand(o => ExecuteSearch());

            LoadPasswordsFromDatabase();
            ExecuteSearch();
        }

        // Charger les mots de passe depuis la base de données
        private void LoadPasswordsFromDatabase()
        {
            using (var context = new AppDbContext())
            {
                var passwords = context.PasswordEntries.ToList();
                foreach (var password in passwords)
                {
                    AllPasswords.Add(password);
                }
            }
        }

        // Ajouter un mot de passe (avec chiffrement)
        public void AddPassword()
        {
            string encryptedPassword = SecurityHelper.Encrypt(NewPassword, EncryptionKey);
            var entry = new PasswordEntry
            {
                URL = NewURL,
                NomCompte = NewAccount,
                MotDePasse = encryptedPassword,
                DateAjout = DateTime.Now
            };

            // Ajouter à la collection locale
            AllPasswords.Add(entry);

            // Ajouter à la base de données
            using (var context = new AppDbContext())
            {
                context.PasswordEntries.Add(entry);
                context.SaveChanges();
            }

            // Mettre à jour la recherche
            ExecuteSearch();

            // Réinitialiser les champs après ajout
            NewAccount = string.Empty;
            NewPassword = string.Empty;
            NewURL = string.Empty;

            OnPropertyChanged(nameof(NewAccount));
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(NewURL));
        }

        // Effectuer la recherche dans la liste de mots de passe
        public void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPasswords = new ObservableCollection<PasswordEntry>(AllPasswords);
            }
            else
            {
                var filtered = AllPasswords.Where(p => p.NomCompte.IndexOf(SearchText, StringComparison.InvariantCultureIgnoreCase) >= 0);
                FilteredPasswords = new ObservableCollection<PasswordEntry>(filtered);
            }
        }

        // Implémentation de l'interface INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

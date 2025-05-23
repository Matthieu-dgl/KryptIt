# :closed_lock_with_key: KryptIt

**KryptIt** est une application de gestion de mots de passe sécurisée développée en **C# avec WPF (.NET Framework 4.8)**. Elle permet de stocker, organiser, partager et exporter des mots de passe de manière sécurisée, avec prise en charge de l’**authentification à deux facteurs (2FA)**.

---

## :sparkles: Fonctionnalités principales

- **Gestion des mots de passe** : Ajout, modification, suppression et affichage de mots de passe chiffrés.
- **Recherche & filtrage** : Recherche instantanée et filtrage par tag.
- **Partage sécurisé** : Partage des mots de passe entre utilisateurs.
- **Import / Export** : Format CSV ou XML pour la migration ou la sauvegarde.
- **Remplissage automatique** : Autofill des champs de connexion dans le navigateur.
- **2FA (TOTP)** : Authentification à deux facteurs avec QR code (Google Authenticator et Microsoft Authenticator compatible).
- **Interface moderne** : Ergonomique, fluide, conçue pour la productivité.

---

## :bricks: Architecture du projet

Le projet suit le **pattern MVVM (Model - View - ViewModel)**, adapté aux applications WPF.

### :package: Models

- `User` : Utilisateur de l’application.
- `PasswordEntry` : Entrée de mot de passe (site, identifiants, mot de passe chiffré).
- `Tag` : Étiquette personnalisée.
- `PasswordEntryTag` : Table de liaison (relation many-to-many).
- `SharedPassword` : Gestion du partage entre utilisateurs.

### :brain: ViewModels

- `MainViewModel` : Logique principale (gestion, 2FA, export/import...).
- Utilisation de `ICommand` pour lier l’UI à la logique métier.

### :eye: Views

- Interfaces utilisateur en WPF (`LoginWindow`, `MainWindow`, etc.).

### :tools: Helpers

- `SecurityHelper` : Chiffrement/déchiffrement des mots de passe.
- `SessionManager` : Gestion de la session utilisateur.

### :card_box: Data

- `AppDbContext` : Contexte Entity Framework (Code First).

---

## :closed_lock_with_key: Sécurité

- **Chiffrement local** des mots de passe avant stockage.
- **2FA avec TOTP** (Time-based One-Time Passwords).
- **Gestion des permissions** lors du partage des mots de passe.

---

## :outbox_tray: Export / Import

- **Export CSV/XML** : Sauvegarde ou migration de mots de passe.
- **Import CSV/XML** : Récupération depuis d'autres outils.

---

## :rocket: Démarrage rapide

```bash
# 1. Cloner le dépôt
git clone https://github.com/votre-utilisateur/KryptIt.git

# 2. Ouvrir la solution avec Visual Studio 2022

# 3. Restaurer les packages NuGet

# 4. Configurer la chaîne de connexion dans App.config si nécessaire

# 5. Lancer l’application

```

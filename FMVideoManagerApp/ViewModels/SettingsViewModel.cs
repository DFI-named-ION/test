using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.DTO;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.Services;
using FMVideoManagerApp.ViewModels.Items;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class SettingsViewModel : ObservableObject, IComponentViewModel
    {
        private readonly MessageService _messageService;
        private readonly AuthService _authService;
        private readonly LocalIndexedPathService _indexedPathService;
        private readonly ApiClient _apiClient;

        private ObservableCollection<LocalIndexedPathItemViewModel> _paths = new();
        public ObservableCollection<LocalIndexedPathItemViewModel> Paths
        {
            get => _paths;
            private set
            {
                if (_paths != value)
                {
                    _paths = value;
                    OnPropertyChanged(nameof(Paths));
                }
            }
        }

        private int _columnsCount = 1;
        public int ColumnsCount
        {
            get => _columnsCount;
            set
            {
                if (_columnsCount != value)
                {
                    _columnsCount = value;
                    OnPropertyChanged(nameof(ColumnsCount));
                    OnColumnsCountChanged?.Invoke(_columnsCount);
                }
            }
        }

        private ObservableCollection<CloudAccountItemViewModel> _cloudAccounts = new();
        public ObservableCollection<CloudAccountItemViewModel> CloudAccounts
        {
            get => _cloudAccounts;
            private set
            {
                if (_cloudAccounts != value)
                {
                    _cloudAccounts = value;
                    OnPropertyChanged(nameof(CloudAccounts));
                }
            }
        }

        //public string DropboxAccountText
        //{
        //    get
        //    {
        //        CloudProviderAccountDto? account = CloudAccounts
        //            .FirstOrDefault(x =>
        //                x.Provider == CloudProviderType.Dropbox &&
        //                x.IsActive);

        //        if (account == null)
        //            return "Dropbox is not connected.";

        //        if (!string.IsNullOrWhiteSpace(account.Email))
        //            return $"Dropbox connected: {account.Email}";

        //        if (!string.IsNullOrWhiteSpace(account.DisplayName))
        //            return $"Dropbox connected: {account.DisplayName}";

        //        return "Dropbox connected.";
        //    }
        //}

        public event Action<int>? OnColumnsCountChanged;

        public ICommand AddPathCommand { get; }
        public ICommand RemovePathCommand { get; }
        public ICommand CopyPathCommand { get; }
        public ICommand RefreshPathsCommand { get; }
        public ICommand ConnectDropboxCommand { get; }
        public ICommand RefreshCloudAccountsCommand { get; }
        public ICommand DeactivateCloudAccountCommand { get; }
        public ICommand ActivateCloudAccountCommand { get; }
        public ICommand RemoveCloudAccountCommand { get; }

        public SettingsViewModel(MessageService messageService, AuthService authService, 
            LocalIndexedPathService indexedPathService, ApiClient apiClient)
        {
            _messageService = messageService;
            _authService = authService;
            _indexedPathService = indexedPathService;
            _apiClient = apiClient;

            // PATH
            {
                AddPathCommand = new RelayCommand(_ => AddPath());
                RemovePathCommand = new RelayCommand(x => RemovePath(x));
                CopyPathCommand = new RelayCommand(x => CopyPath(x));
                RefreshPathsCommand = new RelayCommand(_ => UpdateLocalPathsList());
            }

            // CLOUD
            {
                RefreshCloudAccountsCommand = new RelayCommand(async _ => await UpdateCloudAccountsListAsync());
                DeactivateCloudAccountCommand = new RelayCommand(async x => await DeactivateAccountAsync(x));
                ActivateCloudAccountCommand = new RelayCommand(async x => await ActivateAccountAsync(x));
                RemoveCloudAccountCommand = new RelayCommand(async x => await RemoveCloudAccountAsync(x));

                // DROPBOX
                {
                    ConnectDropboxCommand = new RelayCommand(async _ => await ConnectDropboxAsync());
                }

                // GOOGLE DRIVE
                { }

                _authService.LoggedIn += async _ => await UpdateCloudAccountsListAsync();
                _authService.LoggedOut += () => CloudAccounts.Clear();
            }

            _authService.LoggedIn += _ => UpdateLocalPathsList();
            _authService.LoggedOut += OnLoggedOut;

            if (_authService.IsLoggedIn)
            {
                UpdateLocalPathsList();
            }
        }

        private void AddPath()
        {
            try
            {
                if (!_authService.IsLoggedIn)
                {
                    _messageService.ShowWarning("You need to log in first.");
                    return;
                }

                var folderDialog = new OpenFolderDialog
                {
                    Title = "Select folder to index",
                    InitialDirectory = Directory.GetCurrentDirectory(),
                    Multiselect = false
                };

                if (folderDialog.ShowDialog() != true)
                    return;

                LocalIndexedPath addedPath = _indexedPathService.AddPath(folderDialog.FolderName);

                Paths.Add(new LocalIndexedPathItemViewModel(addedPath));

                _messageService.ShowMessage("Successfully added path.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private void RemovePath(object? param)
        {
            try
            {
                if (param is not LocalIndexedPathItemViewModel pathVm)
                    return;

                _indexedPathService.RemovePath(pathVm.Id);

                Paths.Remove(pathVm);

                _messageService.ShowMessage("Successfully removed path.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private void CopyPath(object? param)
        {
            try
            {
                if (param is not LocalIndexedPathItemViewModel pathVm)
                    return;

                Clipboard.SetText(pathVm.Path);

                _messageService.ShowMessage("Successfully copied path.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private void UpdateLocalPathsList()
        {
            try
            {
                if (!_authService.IsLoggedIn)
                {
                    Paths.Clear();
                    return;
                }

                List<LocalIndexedPath> paths = _indexedPathService.GetCurrentUserPaths();

                Paths = new ObservableCollection<LocalIndexedPathItemViewModel>(
                    paths.Select(x => new LocalIndexedPathItemViewModel(x)));
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private void OnLoggedOut()
        {
            Paths.Clear();
        }

        private async Task ConnectDropboxAsync()
        {
            try
            {
                if (!_authService.IsLoggedIn)
                {
                    _messageService.ShowWarning("You need to log in first.");
                    return;
                }

                StartDropboxConnectResponse response =
                    await _apiClient.StartDropboxConnectAsync();

                OpenBrowser(response.AuthorizationUrl);

                _messageService.ShowMessage("Dropbox authorization page opened in browser.");
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Failed to start Dropbox connection: {ex.Message}");
            }
        }

        private static void OpenBrowser(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        private async Task UpdateCloudAccountsListAsync()
        {
            try
            {
                if (!_authService.IsLoggedIn)
                {
                    CloudAccounts.Clear();
                    return;
                }

                List<CloudProviderAccountDto> accounts =
                    await _apiClient.GetCloudAccountsAsync();

                CloudAccounts = new ObservableCollection<CloudAccountItemViewModel>(
                    accounts.Select(x => new CloudAccountItemViewModel(x)));
            }
            catch (Exception ex)
            {
                _messageService.ShowError($"Failed to load cloud accounts: {ex.Message}");
            }
        }

        private async Task ActivateAccountAsync(object? param)
        {
            try
            {
                if (param is not CloudAccountItemViewModel accountVM)
                    return;

                await _apiClient.ActivateCloudAccountAsync(accountVM.Id);

                await UpdateCloudAccountsListAsync();
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task DeactivateAccountAsync(object? param)
        {
            try
            {
                if (param is not CloudAccountItemViewModel accountVM)
                    return;

                await _apiClient.DeactivateCloudAccountAsync(accountVM.Id);

                await UpdateCloudAccountsListAsync();
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }

        private async Task RemoveCloudAccountAsync(object? param)
        {
            try
            {
                if (param is not CloudAccountItemViewModel accountVM)
                    return;

                await _apiClient.RemoveCloudAccountAsync(accountVM.Id);

                await UpdateCloudAccountsListAsync();
            }
            catch (Exception ex)
            {
                _messageService.ShowError(ex.Message);
            }
        }
    }
}
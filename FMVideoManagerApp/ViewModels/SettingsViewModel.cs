using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.Repositories.UserPathRepository;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class SettingsViewModel : ObservableObject, IComponentViewModel
    {
        private MessageService _messageService;
        private AuthService _authService;
        private IUserPathRepository _userPathRepo;

        private ObservableCollection<UserPathItemViewModel> _paths;
        public ObservableCollection<UserPathItemViewModel> Paths
        {
            get => _paths;
            set {
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
                _columnsCount = value;
                OnPropertyChanged(nameof(ColumnsCount));
                OnColumnsCountChanged?.Invoke(_columnsCount);
            }
        }

        public event Action<int>? OnColumnsCountChanged;

        public ICommand AddPathCommand { get; }
        public ICommand RemovePathCommand { get; }
        public ICommand CopyPathCommand { get; }

        public SettingsViewModel(MessageService messageService, AuthService authService, IUserPathRepository userPathRepo)
        {
            _messageService = messageService;
            _authService = authService;
            _userPathRepo = userPathRepo;

            _paths = new ObservableCollection<UserPathItemViewModel>();
            _authService.OnLogIn += UpdatePathsList;

            AddPathCommand = new RelayCommand(x =>
            {
                try
                {
                    var folderDialog = new OpenFolderDialog
                    {
                        Title = "",
                        InitialDirectory = Directory.GetCurrentDirectory(),
                        Multiselect = false
                    };

                    if (folderDialog.ShowDialog() == true)
                    {
                        _userPathRepo.AddUserPath(new UserPath
                        {
                            UserId = _authService.GetUser().Id,
                            Path = folderDialog.FolderName
                        });

                        UpdatePathsList();
                        _messageService.ShowMessage("Successfully added path...");
                    }
                }
                catch (Exception ex)
                {
                    _messageService.ShowError("Error happened during adding new path...");
                }
            });
            RemovePathCommand = new RelayCommand(x =>
            {
                UserPathItemViewModel path = null!;

                try
                {
                    path = (UserPathItemViewModel)x;

                    _userPathRepo.RemovePath(path.ToEntity());

                    UpdatePathsList();

                    _messageService.ShowMessage("Successfully removed path...");
                }
                catch
                {
                    _messageService.ShowError($"Error happened during removing path...");
                }
            });
            CopyPathCommand = new RelayCommand(x =>
            {
                try
                {
                    var path = (UserPathItemViewModel)x;

                    Clipboard.SetText(path.Path);

                    _messageService.ShowMessage("Successfully copied path...");
                }
                catch
                {
                    _messageService.ShowError("Error happened during copying path...");
                }
            });
        }

        private void UpdatePathsList()
        {
            var userId = _authService.GetUser().Id;

            Paths = new ObservableCollection<UserPathItemViewModel>(
                _userPathRepo
                    .GetAllUserPaths(userId)
                    .Select(x => new UserPathItemViewModel(x, _userPathRepo))
            );
        }
    }
}
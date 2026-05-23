using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.Repositories.FileRepository;
using FMVideoManagerApp.Models;
using FMVideoManagerApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class FileListViewModel : ObservableObject, IComponentViewModel
    {
        private readonly MessageService _messageService;
        private readonly AuthService _authService;
        private readonly FileIndexingService _fileIndexingService;
        private readonly IFileRepository _fileRepo;

        private ObservableCollection<FileItem> _hierarchyEntries;
        public ObservableCollection<FileItem> HierarchyEntries
        {
            get => _hierarchyEntries;
            set
            {
                if (_hierarchyEntries != value)
                {
                    _hierarchyEntries = value;
                    OnPropertyChanged(nameof(HierarchyEntries));
                }
            }
        }

        private FileItem _selectedEntry;
        public FileItem SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (_selectedEntry != value)
                {
                    _selectedEntry = value;
                    OnPropertyChanged(nameof(SelectedEntry));
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
                    OnPropertyChanged(nameof(CardHeight));
                    OnPropertyChanged(nameof(CardWidth));
                    OnPropertyChanged(nameof(ImageWidth));
                    OnPropertyChanged(nameof(ImageHeight));
                    OnPropertyChanged(nameof(CardFontSize));
                    OnPropertyChanged(nameof(TextMaxHeight));
                }
            }
        }

        public double CardHeight => ColumnsCount switch
        {
            1 => 270,
            2 => 230,
            3 => 190,
            4 => 150,
            5 => 125,
            6 => 100,
            _ => 100
        };

        public double CardWidth => ColumnsCount switch
        {
            1 => 555,
            2 => 267,
            3 => 171,
            4 => 123,
            5 => 95,
            6 => 75,
            _ => 75
        };

        public double ImageWidth => ColumnsCount switch
        {
            1 => 300,
            2 => 220,
            3 => 155,
            4 => 110,
            5 => 80,
            6 => 60,
            _ => 60
        };

        public double ImageHeight => ColumnsCount switch
        {
            1 => 200,
            2 => 150,
            3 => 115,
            4 => 85,
            5 => 65,
            6 => 50,
            _ => 50
        };

        public double CardFontSize => ColumnsCount switch
        {
            1 => 18,
            2 => 16,
            3 => 14,
            4 => 12,
            5 => 11,
            6 => 10,
            _ => 10
        };

        public double TextMaxHeight => ColumnsCount switch
        {
            1 => 110,
            2 => 90,
            3 => 75,
            4 => 65,
            5 => 60,
            6 => 55,
            _ => 55
        };

        public ICommand StartIndexingCommand { get; }
        public ICommand SelectEntryCommand { get; }

        public FileListViewModel(MessageService messageService, AuthService authService, FileIndexingService fileIndexingService, IFileRepository fileRepo)
        {
            _messageService = messageService;
            _authService = authService;
            _authService.OnLogIn += FetchUserFiles;
            _fileIndexingService = fileIndexingService;
            _fileRepo = fileRepo;

            _hierarchyEntries= new ObservableCollection<FileItem>();
            _selectedEntry = new FileItem()
            {
                OriginalFilename = "Select media",
                Path = "Select media",
                Notes = "Select media"
            };

            StartIndexingCommand = new RelayCommand(x =>
            {
                try
                {
                    _fileIndexingService.StartIndexing();
                    FetchUserFiles();
                }
                catch (Exception ex)
                {
                    _messageService.ShowError("Error happened during file indexing...");
                }
            });
            SelectEntryCommand = new RelayCommand(entry =>
            {
                SelectedEntry = (FileItem)entry;
            });
        }

        public void AssingColumnsCount(int columnsCount)
        {
            ColumnsCount = columnsCount;
        }

        public void FetchUserFiles()
        {
            HierarchyEntries = new ObservableCollection<FileItem>(_fileRepo.GetByUserId(_authService.GetUser().Id));
        }
    }
}
namespace FMVideoManagerApp.Core
{
    public sealed class FileIndexingState : ObservableObject
    {
        private bool _isIndexing;
        public bool IsIndexing
        {
            get => _isIndexing;
            internal set
            {
                if (_isIndexing != value)
                {
                    _isIndexing = value;
                    OnPropertyChanged(nameof(IsIndexing));
                }
            }
        }

        private bool _isIndeterminate;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            internal set
            {
                if (_isIndeterminate != value)
                {
                    _isIndeterminate = value;
                    OnPropertyChanged(nameof(IsIndeterminate));
                }
            }
        }

        private int _totalFiles;
        public int TotalFiles
        {
            get => _totalFiles;
            internal set
            {
                if (_totalFiles != value)
                {
                    _totalFiles = value;
                    OnPropertyChanged(nameof(TotalFiles));
                    OnPropertyChanged(nameof(ProgressPercent));
                }
            }
        }

        private int _processedFiles;
        public int ProcessedFiles
        {
            get => _processedFiles;
            internal set
            {
                if (_processedFiles != value)
                {
                    _processedFiles = value;
                    OnPropertyChanged(nameof(ProcessedFiles));
                    OnPropertyChanged(nameof(ProgressPercent));
                }
            }
        }

        private int _failedFiles;
        public int FailedFiles
        {
            get => _failedFiles;
            internal set
            {
                if (_failedFiles != value)
                {
                    _failedFiles = value;
                    OnPropertyChanged(nameof(FailedFiles));
                }
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            internal set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        private string? _currentFilePath;
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            internal set
            {
                if (_currentFilePath != value)
                {
                    _currentFilePath = value;
                    OnPropertyChanged(nameof(CurrentFilePath));
                }
            }
        }

        public double ProgressPercent
        {
            get
            {
                if (TotalFiles <= 0)
                    return 0;

                return (double)ProcessedFiles / TotalFiles * 100.0;
            }
        }

        internal void Reset()
        {
            IsIndexing = false;
            IsIndeterminate = false;
            TotalFiles = 0;
            ProcessedFiles = 0;
            StatusMessage = string.Empty;
            CurrentFilePath = null;
        }
    }
}
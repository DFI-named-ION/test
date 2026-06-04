using FMVideoManagerApp.Core;
using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.ViewModels.Items
{
    public sealed class LocalIndexedPathItemViewModel : ObservableObject
    {
        public long Id { get; }

        public long ServerUserId { get; }

        private string _path;
        public string Path
        {
            get => _path;
            private set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public bool IsEnabled { get; }

        public bool IncludeSubdirectories { get; }

        public DateTime CreatedAtUtc { get; }

        public DateTime? LastScannedAtUtc { get; }

        public LocalIndexedPathItemViewModel(LocalIndexedPath path)
        {
            Id = path.Id;
            ServerUserId = path.ServerUserId;
            _path = path.Path;
            IsEnabled = path.IsEnabled;
            IncludeSubdirectories = path.IncludeSubdirectories;
            CreatedAtUtc = path.CreatedAtUtc;
            LastScannedAtUtc = path.LastScannedAtUtc;
        }
    }
}
using FMVideoManagerApp.Core;
using FMVideoManagerApp.Data.Repositories.UserPathRepository;
using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.ViewModels
{
    public sealed class UserPathItemViewModel : ObservableObject
    {
        private readonly IUserPathRepository _userPathRepo;

        public long Id { get; }
        public long UserId { get; }

        private string _path;

        public string Path
        {
            get => _path;
            set
            {
                if (_path == value)
                    return;

                _path = value;
                OnPropertyChanged(nameof(Path));

                _userPathRepo.UpdatePath(Id, _path);
            }
        }

        public UserPathItemViewModel(UserPath userPath, IUserPathRepository userPathRepo)
        {
            _userPathRepo = userPathRepo;

            Id = userPath.Id;
            UserId = userPath.UserId;
            _path = userPath.Path;
        }

        public UserPath ToEntity()
        {
            return new UserPath
            {
                Id = Id,
                UserId = UserId,
                Path = Path
            };
        }
    }
}
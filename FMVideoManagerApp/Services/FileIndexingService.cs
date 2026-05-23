using FFMpegCore;
using FMVideoManagerApp.Data.Repositories.FileRepository;
using FMVideoManagerApp.Data.Repositories.UserPathRepository;
using System.IO;
using System.Windows;

namespace FMVideoManagerApp.Services
{
    public sealed class FileIndexingService
    {
        private readonly IUserPathRepository _userPathRepo;
        private readonly IFileRepository _fileRepo;

        private readonly MessageService _messageService;
        private readonly AuthService _authService;

        public FileIndexingService(MessageService messageService, AuthService authService, IUserPathRepository userPathRepo, IFileRepository fileRepo)
        {
            _messageService = messageService;
            _authService = authService;
            _userPathRepo = userPathRepo;
            _fileRepo = fileRepo;
        }

        public void StartIndexing() // async
        {
            try
            {
                var indexingPaths = _userPathRepo.GetAllUserPaths(_authService.GetUser().Id);
                List<FileInfo> files = new();

                foreach (var it in indexingPaths)
                {
                    string path = it.Path;
                    string[] filePaths = Directory.GetFiles(path, "*.mp4");

                    foreach (string filePath in filePaths)
                    {
                        files.Add(new FileInfo(filePath));
                    }
                }

                var userId = _authService.GetUser().Id;

                foreach (FileInfo file in files)
                {
                    string fileHash = CryptographyService.HashFile(file);
                    var mediaInfo = FFProbe.Analyse(file.FullName);

                    _fileRepo.AddFile(
                        userId, null,
                        file, fileHash,
                        (long)mediaInfo.Duration.TotalMilliseconds,
                        mediaInfo.PrimaryVideoStream?.Width,
                        mediaInfo.PrimaryVideoStream?.Height,
                        "");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during indexing...");
            }
        }
    }
}
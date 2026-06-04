using FMVideoManagerApp.Data.Repositories.LocalIndexedPathRepository;
using FMVideoManagerApp.Models;
using System.IO;

namespace FMVideoManagerApp.Services
{
    public sealed class LocalIndexedPathService
    {
        private readonly ILocalIndexedPathRepository _repo;
        private readonly AuthService _authService;

        public LocalIndexedPathService(ILocalIndexedPathRepository repository, AuthService authService)
        {
            _repo = repository;
            _authService = authService;
        }

        public List<LocalIndexedPath> GetCurrentUserPaths()
        {
            long userId = _authService.GetCurrentUserId();

            return _repo.GetByUserId(userId);
        }

        public LocalIndexedPath AddPath(string path, bool includeSubdirectories = true)
        {
            long userId = _authService.GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Path is empty.");

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory does not exist: {path}");

            string normalizedPath = NormalizePath(path);

            LocalIndexedPath? existing = _repo.FindByPath(userId, normalizedPath);

            if (existing != null)
                throw new InvalidOperationException("This path is already indexed.");

            return _repo.Add(new LocalIndexedPath
            {
                ServerUserId = userId,
                Path = normalizedPath,
                IsEnabled = true,
                IncludeSubdirectories = includeSubdirectories
            });
        }

        public void RemovePath(long id)
        {
            long userId = _authService.GetCurrentUserId();

            _repo.Remove(id, userId);
        }

        public void SetEnabled(long id, bool isEnabled)
        {
            long userId = _authService.GetCurrentUserId();

            _repo.SetEnabled(id, userId, isEnabled);
        }

        private static string NormalizePath(string path)
        {
            string fullPath = Path.GetFullPath(path);

            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
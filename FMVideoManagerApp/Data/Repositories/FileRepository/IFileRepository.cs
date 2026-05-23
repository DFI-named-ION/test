using FMVideoManagerApp.Models;
using System.IO;

namespace FMVideoManagerApp.Data.Repositories.FileRepository
{
    public interface IFileRepository
    {
        public FileItem AddFile(long userId, long? parentNodeId, FileInfo file,
            string hash, long duration, int? width, int? height, string notes = "");
        public List<FileItem> FindByHash(string hash);
        public List<FileItem> GetByUserId(long userId);
        public List<FileItem> GetAll();
    }
}
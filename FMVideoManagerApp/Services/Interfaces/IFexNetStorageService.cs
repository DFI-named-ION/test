using System.IO;

namespace FMVideoManagerApp.Services.Interfaces
{
    internal interface IFexNetStorageService : IBaseStorageService
    {
        void ShareFile(FileInfo file);
        void ShareFiles(List<FileInfo> files);
    }
}
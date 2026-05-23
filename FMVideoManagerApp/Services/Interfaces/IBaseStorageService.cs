using System.IO;

namespace FMVideoManagerApp.Services.Interfaces
{
    internal interface IBaseStorageService
    {
        void UploadFile(FileInfo file);

        void RemoveFile(FileInfo file);
        void DownloadFile(FileInfo file);
    }
}
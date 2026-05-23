using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Data.Repositories.UserPathRepository
{
    public interface IUserPathRepository
    {
        public List<UserPath> GetAllUserPaths(long userId);

        public UserPath AddUserPath(UserPath path);

        public UserPath? FindByPath(string path);

        public void RemovePath(UserPath path);

        public void UpdatePath(long id, string path);
    }
}
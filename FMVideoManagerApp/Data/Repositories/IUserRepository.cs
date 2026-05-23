using FMVideoManagerApp.Models;

namespace FMVideoManagerApp.Data.Repositories
{
    public interface IUserRepository
    {
        public List<AppUser> GetAll();

        public List<AppUser> FindByAlias(string alias);
        public AppUser? FindById(long id);
        public AppUser? FindByLogin(string login);

        AppUser Add(AppUser user);

        public int Count();
    }
}
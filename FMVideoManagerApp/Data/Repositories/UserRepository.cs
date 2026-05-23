using FMVideoManagerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FMVideoManagerApp.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _contextFactory;

        public UserRepository(IDbContextFactory<LocalDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public List<AppUser> GetAll()
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.Users
            //    .AsNoTracking()
            //    .ToList();

            return null;
        }

        public int Count()
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.Users.Count();

            return 0;
        }

        public List<AppUser> FindByAlias(string alias)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.Users
            //    .AsNoTracking()
            //    .Where(x => x.Alias == alias)
            //    .ToList();

            return null;
        }

        public AppUser? FindById(long id)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.Users
            //    .AsNoTracking()
            //    .FirstOrDefault(x => x.Id == id);

            return null;
        }

        public AppUser? FindByLogin(string login)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.Users
            //    .AsNoTracking()
            //    .FirstOrDefault(x => x.Login == login);

            return null;
        }

        

        public AppUser Add(AppUser user)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //db.Users.Add(user);
            //db.SaveChanges();

            //return user;

            return null;
        }
    }
}
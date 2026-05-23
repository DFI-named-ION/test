using FMVideoManagerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FMVideoManagerApp.Data.Repositories.UserPathRepository
{
    public sealed class UserPathRepository : IUserPathRepository
    {
        private readonly IDbContextFactory<LocalDbContext> _contextFactory;

        public UserPathRepository(IDbContextFactory<LocalDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        public List<UserPath> GetAllUserPaths(long userId)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.UserPaths
            //    .AsNoTracking()
            //    .Where(x => x.UserId == userId)
            //    .ToList();

            return null;
        }

        public UserPath AddUserPath(UserPath path)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //db.UserPaths.Add(path);
            //db.SaveChanges();

            //return path;

            return null;
        }

        public UserPath? FindByPath(string path)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //return db.UserPaths
            //    .AsNoTracking()
            //    .FirstOrDefault(x => x.Path == path);

            return null;
        }


        public void RemovePath(UserPath path)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //db.UserPaths.Remove(path);
            //db.SaveChanges();
        }

        public void UpdatePath(long id, string path)
        {
            using LocalDbContext db = _contextFactory.CreateDbContext();

            //UserPath userPath = new UserPath
            //{
            //    Id = id,
            //    Path = path
            //};

            //db.UserPaths.Attach(userPath);
            //db.Entry(userPath).Property(x => x.Path).IsModified = true;

            //db.SaveChanges();
        }
    }
}
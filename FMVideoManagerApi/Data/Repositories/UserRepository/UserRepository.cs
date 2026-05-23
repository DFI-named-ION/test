using Microsoft.EntityFrameworkCore;
using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.Repositories.UserRepository
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly ServerDbContext _db;

        public UserRepository(ServerDbContext db)
        {
            _db = db;
        }

        public Task<AppUser?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<AppUser?> FindByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Login.ToLower() == login, cancellationToken);
        }

        public Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return _db.Users
                .AnyAsync(x => x.Login.ToLower() == login, cancellationToken);
        }

        public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        {
            await _db.Users.AddAsync(user, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _db.SaveChangesAsync(cancellationToken);
        }
    }
}

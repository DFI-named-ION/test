using FMVideoManagerApi.Models;

namespace FMVideoManagerApi.Data.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task<AppUser?> FindByIdAsync(long id, CancellationToken cancellationToken = default);

        Task<AppUser?> FindByLoginAsync(string login, CancellationToken cancellationToken = default);

        Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default);

        Task AddAsync(AppUser user, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
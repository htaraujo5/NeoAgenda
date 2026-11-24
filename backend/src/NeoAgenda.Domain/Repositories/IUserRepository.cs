using NeoAgenda.Domain.Entities;

namespace NeoAgenda.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

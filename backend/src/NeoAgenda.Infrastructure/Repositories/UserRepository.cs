using Microsoft.EntityFrameworkCore;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Data;

namespace NeoAgenda.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.ToLowerInvariant().Trim();
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == normalized, ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken, ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Users.OrderBy(u => u.Username).ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}

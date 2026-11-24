using Microsoft.EntityFrameworkCore;
using NeoAgenda.Domain.Entities;
using NeoAgenda.Domain.Repositories;
using NeoAgenda.Infrastructure.Data;

namespace NeoAgenda.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly AppDbContext _context;

    public ContactRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Contact?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<Contact> query = _context.Contacts;

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Contact?> GetByNormalizedPhoneAsync(string normalizedPhone, CancellationToken ct = default)
    {
        return await _context.Contacts
            .FirstOrDefaultAsync(c => c.PhoneNormalized == normalizedPhone, ct);
    }

    public async Task<bool> ExistsByNormalizedPhoneAsync(string normalizedPhone, Guid? excludeContactId = null, CancellationToken ct = default)
    {
        var query = _context.Contacts.Where(c => c.PhoneNormalized == normalizedPhone);

        if (excludeContactId.HasValue)
        {
            query = query.Where(c => c.Id != excludeContactId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Contact contact, CancellationToken ct = default)
    {
        await _context.Contacts.AddAsync(contact, ct);
    }

    public void Update(Contact contact)
    {
        _context.Contacts.Update(contact);
    }

    public void DeletePermanently(Contact contact)
    {
        _context.Contacts.Remove(contact);
    }

    public async Task<(IReadOnlyList<Contact> Items, int TotalCount)> GetActivePagedAsync(
        string? search, 
        int page, 
        int pageSize, 
        string? sortBy, 
        bool sortDesc, 
        string? currentUser = null,
        bool isAdmin = true,
        CancellationToken ct = default)
    {
        var query = _context.Contacts.AsNoTracking().AsQueryable();

        // Se o usuário não for administrador, filtra apenas os contatos da sua própria agenda
        if (!isAdmin && !string.IsNullOrWhiteSpace(currentUser))
        {
            query = query.Where(c => c.CreatedBy == currentUser);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => 
                c.Name.ToLower().Contains(term) || 
                c.Email.ToLower().Contains(term) || 
                c.PhoneOriginal.Contains(term) || 
                c.PhoneNormalized.Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "email" => sortDesc ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "phone" => sortDesc ? query.OrderByDescending(c => c.PhoneOriginal) : query.OrderBy(c => c.PhoneOriginal),
            "createdat" => sortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Contact>> GetRecycleBinAsync(
        string? currentUser = null,
        bool isAdmin = true,
        CancellationToken ct = default)
    {
        var query = _context.Contacts
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted);

        if (!isAdmin && !string.IsNullOrWhiteSpace(currentUser))
        {
            query = query.Where(c => c.CreatedBy == currentUser);
        }

        return await query
            .OrderByDescending(c => c.DeletedAt)
            .ToListAsync(ct);
    }

    public async Task<int> PurgeExpiredRecycleBinAsync(DateTime expirationThreshold, CancellationToken ct = default)
    {
        var expiredContacts = await _context.Contacts
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted && c.RetentionExpiresAt.HasValue && c.RetentionExpiresAt.Value <= expirationThreshold)
            .ToListAsync(ct);

        if (expiredContacts.Count > 0)
        {
            _context.Contacts.RemoveRange(expiredContacts);
            return await _context.SaveChangesAsync(ct);
        }

        return 0;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeoAgenda.Application.DTOs;
using NeoAgenda.Application.Interfaces;

namespace NeoAgenda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    private string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
    private bool IsAdmin => User.IsInRole("Admin");

    /// <summary>
    /// Lista contatos ativos com suporte a paginação, busca e ordenação.
    /// Administradores visualizam todas as agendas; usuários padrão visualizam apenas os seus contatos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ContactDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivePaged(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var result = await _contactService.GetActivePagedAsync(search, page, pageSize, sortBy, sortDesc, CurrentUsername, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtém detalhes de um contato ativo pelo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _contactService.GetByIdAsync(id, CurrentUsername, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo contato com validação e garantia de unicidade de telefone.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateContactDto dto, CancellationToken ct)
    {
        var created = await _contactService.CreateAsync(dto, CurrentUsername, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Atualiza os dados de um contato existente.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactDto dto, CancellationToken ct)
    {
        var updated = await _contactService.UpdateAsync(id, dto, CurrentUsername, IsAdmin, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Move um contato para a Lixeira (Soft Delete) com período de retenção de 30 dias.
    /// Administradores podem mover contatos de qualquer agenda para a lixeira.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(SoftDeleteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        var result = await _contactService.SoftDeleteAsync(id, CurrentUsername, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Restaura um contato da lixeira de volta para a listagem ativa (se dentro dos 30 dias).
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(RestoreContactResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status410Gone)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await _contactService.RestoreAsync(id, CurrentUsername, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Executa a exclusão definitiva e irrevogável dos dados pessoais conforme Artigo 18, VI da LGPD.
    /// Administradores têm permissão global para expurgar contatos de qualquer agenda.
    /// </summary>
    [HttpDelete("{id:guid}/lgpd")]
    [ProducesResponseType(typeof(LgpdPurgeResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PurgeLgpd(Guid id, [FromQuery] string? requestedBy = null, CancellationToken ct = default)
    {
        var requester = !string.IsNullOrWhiteSpace(requestedBy) ? requestedBy : CurrentUsername;
        var result = await _contactService.PurgeLgpdAsync(id, requester, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista os contatos temporariamente na lixeira com dias restantes para expiração.
    /// Administradores visualizam a lixeira geral.
    /// </summary>
    [HttpGet("recycle-bin")]
    [ProducesResponseType(typeof(IReadOnlyList<RecycleBinContactDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecycleBin(CancellationToken ct)
    {
        var result = await _contactService.GetRecycleBinAsync(CurrentUsername, IsAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Lista o histórico de protocolos anônimos de conformidade LGPD (Art. 18, VI).
    /// </summary>
    [HttpGet("lgpd-audits")]
    [ProducesResponseType(typeof(IReadOnlyList<LgpdAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLgpdAudits(CancellationToken ct)
    {
        var result = await _contactService.GetLgpdAuditLogsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Importa contatos em lote via arquivo CSV, com tratamento tolerante a falhas por linha.
    /// </summary>
    [HttpPost("import-csv")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CsvImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportCsv(IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Arquivo inválido",
                Detail = "Por favor, selecione um arquivo CSV válido para upload."
            });
        }

        await using var stream = file.OpenReadStream();
        var result = await _contactService.ImportCsvAsync(stream, CurrentUsername, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exporta todos os contatos ativos no formato CSV.
    /// </summary>
    [HttpGet("export-csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var bytes = await _contactService.ExportCsvAsync(CurrentUsername, IsAdmin, ct);
        return File(bytes, "text/csv; charset=utf-8", $"contatos_neoagenda_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}

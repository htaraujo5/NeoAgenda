using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using NeoAgenda.Application.DTOs;
using Xunit;

namespace NeoAgenda.IntegrationTests.Controllers;

public class ContactsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContactsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CompleteLifecycle_ShouldCreate_DetectDuplicate_SoftDelete_Restore_AndPurgeLgpd()
    {
        // 1. Criar novo contato
        var createDto = new CreateContactDto("Gabriel Santana", "gabriel@teste.com", "(11) 98765-9988");
        var createResponse = await _client.PostAsJsonAsync("/api/contacts", createDto);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ContactDto>();
        created.Should().NotBeNull();
        created!.Name.Should().Be("Gabriel Santana");
        created.PhoneNormalized.Should().Be("+5511987659988");

        // 2. Tentar cadastrar outro contato com o MESMO telefone formatado diferente (Deve retornar 409 Conflict)
        var duplicateDto = new CreateContactDto("Gabriel Clone", "clone@teste.com", "+55 11 98765-9988");
        var duplicateResponse = await _client.PostAsJsonAsync("/api/contacts", duplicateDto);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict, "telefone duplicado não deve ser permitido");

        // 3. Obter contato por ID
        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Soft Delete (Mover para lixeira)
        var deleteResponse = await _client.DeleteAsync($"/api/contacts/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Garantir que não aparece mais na listagem de contatos ativos
        var listResponse = await _client.GetAsync($"/api/contacts?search=Gabriel");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<ContactDto>>();
        list!.Items.Should().NotContain(c => c.Id == created.Id);

        // 6. Consultar a lixeira e verificar presença do contato
        var recycleResponse = await _client.GetAsync("/api/contacts/recycle-bin");
        recycleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var recycleList = await recycleResponse.Content.ReadFromJsonAsync<List<RecycleBinContactDto>>();
        recycleList!.Should().Contain(c => c.Id == created.Id);

        // 7. Restaurar contato da lixeira
        var restoreResponse = await _client.PostAsync($"/api/contacts/{created.Id}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Exclusão definitiva por LGPD (Direito ao Esquecimento)
        var lgpdResponse = await _client.DeleteAsync($"/api/contacts/{created.Id}/lgpd?requestedBy=teste_integracao");
        lgpdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lgpdResult = await lgpdResponse.Content.ReadFromJsonAsync<LgpdPurgeResultDto>();
        lgpdResult!.Protocol.Should().StartWith("LGPD-");

        // 9. Garantir que após LGPD nem mesmo a lixeira contém o contato
        var afterLgpdGet = await _client.GetAsync($"/api/contacts/{created.Id}");
        afterLgpdGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 10. Consultar rota de auditoria LGPD e verificar o registro anônimo gerado
        var auditResponse = await _client.GetAsync("/api/contacts/lgpd-audits");
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var audits = await auditResponse.Content.ReadFromJsonAsync<List<LgpdAuditLogDto>>();
        audits.Should().NotBeNull();
        audits!.Should().Contain(a => a.RequestedBy == "teste_integracao");
    }

    [Fact]
    public async Task ImportCsv_ShouldUploadFile_AndReturnGranularSummary()
    {
        // Arrange
        var csvContent = """
        nome;email;telefone
        Importado Integracao;integracao@teste.com;(11) 97777-1111
        Invalido Integracao;;(11) 96666-2222
        """;

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "contatos.csv");

        // Act
        var response = await _client.PostAsync("/api/contacts/import-csv", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        result.Should().NotBeNull();
        result!.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Field == "Email");
    }

    [Fact]
    public async Task ExportCsv_ShouldReturnValidCsvFile()
    {
        // Act
        var response = await _client.GetAsync("/api/contacts/export-csv");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        var csvString = await response.Content.ReadAsStringAsync();
        csvString.Should().Contain("Nome;Email;Telefone;DataCadastro");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

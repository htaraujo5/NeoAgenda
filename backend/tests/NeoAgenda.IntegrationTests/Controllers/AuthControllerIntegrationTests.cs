using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NeoAgenda.Application.DTOs;
using Xunit;

namespace NeoAgenda.IntegrationTests.Controllers;

public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ShouldSucceed_WithValidCredentials()
    {
        var loginDto = new LoginRequestDto("admin", "Admin@123");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.Username.Should().Be("admin");
    }

    [Fact]
    public async Task Login_ShouldFail_WithInvalidCredentials()
    {
        var loginDto = new LoginRequestDto("admin", "SenhaErrada");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ShouldIssueNewToken_WhenRefreshTokenIsValid()
    {
        // 1. Faz login inicial
        var loginDto = new LoginRequestDto("admin", "Admin@123");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var initialAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // 2. Renova token
        var refreshDto = new RefreshTokenRequestDto(initialAuth!.RefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshDto);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        newAuth.Should().NotBeNull();
        newAuth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        newAuth.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ContactsEndpoints_ShouldReturn401Unauthorized_WhenNoTokenProvided()
    {
        var response = await _client.GetAsync("/api/contacts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

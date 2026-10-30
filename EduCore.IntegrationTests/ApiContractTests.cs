using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EduCore.IntegrationTests.TestSupport;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// API contract checks: the removed email-existence oracle (Issue D),
    /// authentication/authorization boundaries, and validation error
    /// consistency (problem+json, 400/401/403/404).
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class ApiContractTests : IClassFixture<EduCoreApiFactory>
    {
        private readonly EduCoreApiFactory _api;

        public ApiContractTests(EduCoreApiFactory api) => _api = api;

        private void RequireFactory()
            => Skip.IfNot(Environment.GetEnvironmentVariable("DB_CONNECTION") != null,
                "Database fixture unavailable (no SQL Server) — API host not configured.");

        private HttpClient ClientAs(int userId, string email, params string[] roles)
        {
            HttpClient client = _api.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Token(userId, email, roles));
            return client;
        }

        [Fact]
        public async Task EmailExists_OracleEndpoint_IsRemoved()
        {
            RequireFactory();

            HttpResponseMessage response = await _api.CreateClient()
                .GetAsync($"/api/users/email-exists?email=probe{Guid.NewGuid():N}@test.local");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_Anonymous_Returns401()
        {
            RequireFactory();

            HttpResponseMessage response = await _api.CreateClient().GetAsync("/api/users/students");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_ForgedToken_Returns401()
        {
            RequireFactory();

            HttpClient client = _api.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.forged.signature");

            HttpResponseMessage response = await client.GetAsync("/api/users/students");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AdminEndpoint_Student_Returns403()
        {
            RequireFactory();

            HttpResponseMessage response = await ClientAs(999998, "student@test.local", "Student")
                .GetAsync("/api/payments?pageNumber=1&pageSize=5");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AuditFeed_InvalidPaging_Returns400()
        {
            RequireFactory();

            HttpResponseMessage response = await ClientAs(999997, "admin@test.local", "SuperAdmin")
                .GetAsync("/api/audit?pageNumber=0&pageSize=10000");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterStudent_InvalidEmail_Returns400()
        {
            RequireFactory();

            HttpResponseMessage response = await _api.CreateClient().PostAsync(
                "/api/users/students",
                new StringContent(
                    $$"""{"name":"ab","email":"not-an-email","birthDate":"1990-01-01","password":"short"}""",
                    System.Text.Encoding.UTF8,
                    "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Swagger_IsAvailable()
        {
            RequireFactory();

            HttpResponseMessage response = await _api.CreateClient().GetAsync("/swagger/v1/swagger.json");

            response.EnsureSuccessStatusCode();
        }
    }
}

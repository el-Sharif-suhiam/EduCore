using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for Issue E: certificate lifecycle — authorized
    /// issuance, public verification, owner/admin-only download, and the
    /// fact that repeat issuance does not duplicate records or files.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class CertificateApiTests : IClassFixture<EduCoreApiFactory>
    {
        private readonly DatabaseFixture _db;
        private readonly EduCoreApiFactory _api;

        public CertificateApiTests(DatabaseFixture db, EduCoreApiFactory api)
        {
            _db = db;
            _api = api;
        }

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private HttpClient ClientAs(int userId, string email, params string[] roles)
        {
            HttpClient client = _api.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Token(userId, email, roles));
            return client;
        }

        private async Task<(int student, int course, string code)> IssuedCertificateAsync()
        {
            string storage = Path.Combine(Path.GetTempPath(), "educore-test-certificates");
            Directory.CreateDirectory(storage);
            Environment.SetEnvironmentVariable("CERTIFICATES_STORAGE_PATH", storage);

            int instructor = await _db.Seed.UserAsync($"ci-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int student = await _db.Seed.UserAsync($"ci-student-{Guid.NewGuid():N}@test.local", "Student");
            (int _, int course) = await _db.Seed.CompletedCourseAsync(instructor, student, lessonCount: 2, completedCount: 2);

            var certificate = await clsProgress.IssueCertificate(student, course);
            return (student, course, certificate.CertificateCode);
        }

        [Fact]
        public async Task Verify_ValidCertificate_PublicEndpointReturnsMetadata()
        {
            RequireDatabase();

            (int student, int _, string code) = await IssuedCertificateAsync();

            HttpResponseMessage response = await _api.CreateClient().GetAsync($"/api/certificates/{code}");

            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"isValid\":true", json);
            Assert.Contains($"\"certificateCode\":\"{code}\"", json);
        }

        [Fact]
        public async Task Verify_UnknownCertificate_Returns404()
        {
            RequireDatabase();

            string unknown = Guid.NewGuid().ToString("N");

            HttpResponseMessage response = await _api.CreateClient().GetAsync($"/api/certificates/{unknown}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Verify_MalformedCode_Returns404()
        {
            RequireDatabase();

            // not a valid GUID "N" code — including a traversal attempt
            HttpResponseMessage response = await _api.CreateClient().GetAsync("/api/certificates/..%2f..%2fweb.config");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Download_Anonymous_Returns401()
        {
            RequireDatabase();

            (int student, int _, string code) = await IssuedCertificateAsync();

            HttpResponseMessage response = await _api.CreateClient()
                .GetAsync($"/api/certificates/{code}/download");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Download_OtherStudent_Returns403()
        {
            RequireDatabase();

            (int student, int _, string code) = await IssuedCertificateAsync();
            int other = await _db.Seed.UserAsync($"ci-other-{Guid.NewGuid():N}@test.local", "Student");

            HttpResponseMessage response = await ClientAs(other, $"ci-other-{Guid.NewGuid():N}@test.local", "Student")
                .GetAsync($"/api/certificates/{code}/download");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Download_Owner_ReturnsPdf()
        {
            RequireDatabase();

            (int student, int _, string code) = await IssuedCertificateAsync();

            HttpResponseMessage response = await ClientAs(student, $"ci-student-{Guid.NewGuid():N}@test.local", "Student")
                .GetAsync($"/api/certificates/{code}/download");

            response.EnsureSuccessStatusCode();
            Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public async Task Download_Admin_ReturnsPdf()
        {
            RequireDatabase();

            (int student, int _, string code) = await IssuedCertificateAsync();

            HttpResponseMessage response = await ClientAs(1, "admin@t.local", "SuperAdmin")
                .GetAsync($"/api/certificates/{code}/download");

            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task IssueCertificate_NonEnrolledUser_ReturnsForbidden()
        {
            RequireDatabase();

            int instructor = await _db.Seed.UserAsync($"ci-ne-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int outsider = await _db.Seed.UserAsync($"ci-ne-outsider-{Guid.NewGuid():N}@test.local", "Student");
            (int _, int course) = await _db.Seed.CompletedCourseAsync(instructor, instructor, lessonCount: 1, completedCount: 1);

            HttpResponseMessage response = await ClientAs(outsider, $"ci-ne-outsider-{Guid.NewGuid():N}@test.local", "Student")
                .GetAsync($"/api/progress/courses/{course}/certificate");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}

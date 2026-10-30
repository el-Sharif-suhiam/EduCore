using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EduCore.IntegrationTests.TestSupport;
using EduCore_BusinessLayer;
using Xunit;

namespace EduCore.IntegrationTests
{
    /// <summary>
    /// Regression tests for Issue C: unpublished bundles must not leak.
    /// Covers the SQL layer (filters applied in the query) and the API
    /// layer (anonymous, student, owning instructor, other instructor,
    /// SuperAdmin) for list and detail endpoints.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class BundlePublicationTests : IClassFixture<EduCoreApiFactory>
    {
        private readonly DatabaseFixture _db;
        private readonly EduCoreApiFactory _api;

        public BundlePublicationTests(DatabaseFixture db, EduCoreApiFactory api)
        {
            _db = db;
            _api = api;
        }

        private void RequireDatabase()
            => Skip.IfNot(_db.DatabaseAvailable, $"SQL Server unavailable: {_db.LastInitError}");

        private HttpClient Client(params string[] roles)
        {
            HttpClient client = _api.CreateClient();
            if (roles.Length > 0)
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", TestJwt.Token(1, "principal@test.local", roles));
            return client;
        }

        private HttpClient ClientAs(int userId, string email, params string[] roles)
        {
            HttpClient client = _api.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestJwt.Token(userId, email, roles));
            return client;
        }

        // ---------------- SQL layer ----------------

        [Fact]
        public async Task GetBundlesView_PublicFeed_ExcludesUnpublished()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"bp-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int draftProduct = await _db.Seed.BundleProductAsync($"Draft {Guid.NewGuid():N}", 40m, owner, false);
            int draft = await _db.Seed.BundleAsync(draftProduct);

            var publicFeed = await clsBundle.GetBundlesView(includeUnpublished: false);

            Assert.DoesNotContain(publicFeed, b => b.Id == draft);
        }

        [Fact]
        public async Task GetBundlesView_InstructorOwnOnly_NeverSeesOtherDrafts()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"ow-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int other = await _db.Seed.UserAsync($"ow-other-{Guid.NewGuid():N}@test.local", "Instructor");

            int ownDraftProduct = await _db.Seed.BundleProductAsync($"Own draft {Guid.NewGuid():N}", 30m, owner, false);
            int ownDraft = await _db.Seed.BundleAsync(ownDraftProduct);
            int otherDraftProduct = await _db.Seed.BundleProductAsync($"Other draft {Guid.NewGuid():N}", 30m, other, false);
            int otherDraft = await _db.Seed.BundleAsync(otherDraftProduct);

            var feed = await clsBundle.GetBundlesView(includeUnpublished: true, ownOnly: true, instructorId: owner);

            Assert.Contains(feed, b => b.Id == ownDraft);
            Assert.DoesNotContain(feed, b => b.Id == otherDraft);
        }

        [Fact]
        public async Task BundleExists_PublishedOnly_HidesDraftsFromPublic()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"bx-inst-{Guid.NewGuid():N}@test.local", "Instructor");

            int publishedProduct = await _db.Seed.BundleProductAsync($"pub {Guid.NewGuid():N}", 20m, owner, true);
            int publishedBundle = await _db.Seed.BundleAsync(publishedProduct);
            int draftProduct = await _db.Seed.BundleProductAsync($"draft {Guid.NewGuid():N}", 30m, owner, false);
            int draftBundle = await _db.Seed.BundleAsync(draftProduct);

            // anonymous existence probes see published only
            Assert.True(await clsBundle.IsBundleExist(publishedBundle, publishedOnly: true));
            Assert.False(await clsBundle.IsBundleExist(draftBundle, publishedOnly: true));

            // privileged callers still see drafts
            Assert.True(await clsBundle.IsBundleExist(draftBundle, publishedOnly: false));
        }

        // ---------------- API layer ----------------

        [Fact]
        public async Task GetBundleById_Draft_HiddenFromPublicAndOtherInstructors()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"bd-owner-{Guid.NewGuid():N}@test.local", "Instructor");
            int other = await _db.Seed.UserAsync($"bd-other-{Guid.NewGuid():N}@test.local", "Instructor");
            int draftProduct = await _db.Seed.BundleProductAsync($"Secret draft {Guid.NewGuid():N}", 40m, owner, false);
            int draft = await _db.Seed.BundleAsync(draftProduct);

            // anonymous
            HttpResponseMessage anon = await _api.CreateClient().GetAsync($"/api/bundles/{draft}");
            Assert.Equal(HttpStatusCode.NotFound, anon.StatusCode);

            // student (authenticated, not the owner)
            HttpResponseMessage student = await ClientAs(999999, "student@test.local", "Student")
                .GetAsync($"/api/bundles/{draft}");
            Assert.Equal(HttpStatusCode.NotFound, student.StatusCode);

            // a DIFFERENT instructor must not receive the owner's draft
            HttpResponseMessage otherInstructor = await ClientAs(other, "other@test.local", "Instructor")
                .GetAsync($"/api/bundles/{draft}");
            Assert.Equal(HttpStatusCode.NotFound, otherInstructor.StatusCode);

            // the owning instructor can preview their own draft
            HttpResponseMessage ownerResponse = await ClientAs(owner, "owner@test.local", "Instructor")
                .GetAsync($"/api/bundles/{draft}");
            Assert.Equal(HttpStatusCode.OK, ownerResponse.StatusCode);

            // administrators may see drafts
            HttpResponseMessage admin = await ClientAs(1, "admin@t.local", "SuperAdmin")
                .GetAsync($"/api/bundles/{draft}");
            Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        }

        [Fact]
        public async Task GetAllBundlesForAdmin_InstructorSeesOnlyOwnBundles()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"ba-owner-{Guid.NewGuid():N}@test.local", "Instructor");
            int other = await _db.Seed.UserAsync($"ba-other-{Guid.NewGuid():N}@test.local", "Instructor");
            int superAdmin = await _db.Seed.UserAsync($"ba-admin-{Guid.NewGuid():N}@test.local", "SuperAdmin");

            int ownDraftProduct = await _db.Seed.BundleProductAsync($"Own draft {Guid.NewGuid():N}", 30m, owner, false);
            int ownDraft = await _db.Seed.BundleAsync(ownDraftProduct);
            int otherDraftProduct = await _db.Seed.BundleProductAsync($"Other draft {Guid.NewGuid():N}", 30m, other, false);
            int otherDraft = await _db.Seed.BundleAsync(otherDraftProduct);

            // instructor sees only their own bundles (drafts + published)
            HttpResponseMessage ownFeed = await ClientAs(owner, $"b-owner-{Guid.NewGuid():N}@test.local", "Instructor")
                .GetAsync("/api/bundles/all");
            ownFeed.EnsureSuccessStatusCode();
            string ownJson = await ownFeed.Content.ReadAsStringAsync();
            Assert.Contains($"\"id\":{ownDraft}", ownJson);
            Assert.DoesNotContain($"\"id\":{otherDraft}", ownJson);

            // SuperAdmin sees all bundles including others' drafts
            HttpResponseMessage adminFeed = await ClientAs(superAdmin, $"ba-admin-{Guid.NewGuid():N}@test.local", "SuperAdmin")
                .GetAsync("/api/bundles/all");
            adminFeed.EnsureSuccessStatusCode();
            string adminJson = await adminFeed.Content.ReadAsStringAsync();
            Assert.Contains($"\"id\":{ownDraft}", adminJson);
            Assert.Contains($"\"id\":{otherDraft}", adminJson);
        }

        [Fact]
        public async Task GetAllBundles_PublicFeed_ReturnsPublishedOnly()
        {
            RequireDatabase();

            int owner = await _db.Seed.UserAsync($"bp-inst-{Guid.NewGuid():N}@test.local", "Instructor");
            int publishedProduct = await _db.Seed.BundleProductAsync($"Pub {Guid.NewGuid():N}", 30m, owner, true);
            int publishedBundle = await _db.Seed.BundleAsync(publishedProduct);
            int draftProduct = await _db.Seed.BundleProductAsync($"Draft {Guid.NewGuid():N}", 30m, owner, false);
            int draft = await _db.Seed.BundleAsync(draftProduct);

            HttpResponseMessage response = await _api.CreateClient().GetAsync("/api/bundles");
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();

            Assert.Contains($"\"id\":{publishedBundle}", json);
            Assert.DoesNotContain($"\"id\":{draft}", json);
        }
    }
}

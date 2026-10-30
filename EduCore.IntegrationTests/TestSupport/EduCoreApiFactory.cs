using System;
using EduCore.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EduCore.IntegrationTests.TestSupport
{
    /// <summary>
    /// In-process API host for authorization/API tests. The database fixture
    /// has already pointed DB_CONNECTION at the disposable EduCoreTests
    /// database and set JWT_SECRET_KEY, so the real pipeline (JWT auth,
    /// authorization handlers, exception middleware, controllers) runs
    /// against isolated data. No external services: Stripe calls are never
    /// triggered by these tests.
    /// </summary>
    public sealed class EduCoreApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            base.ConfigureWebHost(builder);
        }
    }
}

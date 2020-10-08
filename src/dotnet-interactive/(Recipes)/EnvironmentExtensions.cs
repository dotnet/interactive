using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Recipes
{
    public static class EnvironmentExtensions
    {
        public static bool IsTest(this IHostEnvironment hostingEnvironment) =>
            hostingEnvironment.EnvironmentName == "test";

        public static IWebHostBuilder UseTestEnvironment(this IWebHostBuilder builder) =>
            builder.UseEnvironment("test");
    }
}

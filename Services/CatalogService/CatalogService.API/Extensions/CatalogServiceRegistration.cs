using CatalogService.API.Infrastructure;
using CatalogService.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.API.Extensions
{
    public static class CatalogServiceRegistration
    {
        public static void AddCatalogServiceRegistration(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<CatalogContext>(options => options.UseSqlServer(config.GetConnectionString("SqlServer")));


            services.Configure<CatalogSettings>(config.GetSection("CatalogSettings"));
        }
    }
}

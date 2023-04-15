#nullable disable

using Microsoft.EntityFrameworkCore;
using Nest;
using Serilog;

namespace CountryService.Data
{
    public static class PrepDb
    {
        public const int MILLION = 1000 * 1000;
        public const int BILLION = 1000 * 1000 * 1000;
    
        public static void PrepPopulation(IApplicationBuilder app)
        {
            using(var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(
                    serviceScope.ServiceProvider.GetService<AppDbContext>(), 
                    serviceScope.ServiceProvider.GetService<IElasticClient>(),
                    serviceScope.ServiceProvider.GetService<IConfiguration>()
                );
            }
        }

        private static void SeedData(AppDbContext context, IElasticClient elasticClient, IConfiguration configuration)
        {
            Log.Logger.Information("--> Applying Migrations...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex) {
                Log.Logger.Warning($"--> Could not run migrations: {ex.Message}");
            }
            var defaultIndex = configuration["ELKConfiguration:index"];
            
            if (!context.Countries.Any()) {
                Log.Logger.Information("--> Seeding data...");

                // Vietnam
                var hanoi = new Models.City() {
                    Name = "Hanoi",
                    Population = (int)(8.331 * MILLION)
                };
                context.Cities.Add(hanoi);

                var hochiminhCity = new Models.City() {
                    Name = "Ho Chi Minh City",
                    Population = (int)(8.993 * MILLION)
                };
                context.Cities.Add(hochiminhCity);

                var vietnam = new Models.Country() {
                    Name = "Vietnam",
                    OfficialName = "Socialist Republic of Vietnam",
                    Population = (int)(95.54 * MILLION),
                    Cities = new HashSet<Models.City>()
                };
                vietnam.Cities.Add(hanoi);
                vietnam.Cities.Add(hochiminhCity);
                context.Countries.Add(vietnam); 
                context.SaveChanges();
                elasticClient.Index(vietnam, i => i.Index(defaultIndex).Id(vietnam.Id));

                // Russia
                var moscow = new Models.City() {
                    Name = "Moscow",
                    Population = (int) (13.01 * MILLION)
                };
                context.Cities.Add(moscow);

                var russia = new Models.Country() {
                    Name = "Russia",
                    OfficialName = "Russian Federation",
                    Population = (int)(144.3 * MILLION),
                    Cities = new HashSet<Models.City>()
                };              
                russia.Cities.Add(moscow);   
                context.Countries.Add(russia); 
                context.SaveChanges();
                elasticClient.Index(russia, i => i.Index(defaultIndex).Id(russia.Id));

                // USA
                var usa = new Models.Country() {
                    Name = "USA",
                    OfficialName = "United States of America",
                    Population = (int)(326.7 * MILLION),
                    Cities = new HashSet<Models.City>()
                };
                context.Countries.Add(usa);   
                context.SaveChanges();
                elasticClient.Index(usa, i => i.Index(defaultIndex).Id(usa.Id)); 

                // China
                var china = new Models.Country() {
                    Name = "China",
                    OfficialName = "People's Republic of China",
                    Population = (int)(1.393 * BILLION),
                    Cities = new HashSet<Models.City>()
                };
                context.Countries.Add(china);   
                context.SaveChanges();
                elasticClient.Index(china, i => i.Index(defaultIndex).Id(china.Id)); 

                context.SaveChanges();
            }
            else
            {
                Log.Logger.Information("--> Data already populated");
            }
        }
    }
}
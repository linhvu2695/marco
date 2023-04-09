#nullable disable

using Microsoft.EntityFrameworkCore;

namespace CountryService.Data
{
    public static class PrepDb
    {
        public const int MILLION = 1000000;
    
        public static void PrepPopulation(IApplicationBuilder app)
        {
            using(var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(serviceScope.ServiceProvider.GetService<AppDbContext>());
            }
        }

        private static void SeedData(AppDbContext context)
        {
            System.Console.WriteLine("---> Applying Migrations...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex) {
                System.Console.WriteLine($"---> Could not run migrations: {ex.Message}");
            }
            

            if (!context.Countries.Any()) {
                System.Console.WriteLine("---> Seeding data...");

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
                    Population = (int)(95.54 * MILLION),
                    Cities = new HashSet<Models.City>()
                };
                vietnam.Cities.Add(hanoi);
                vietnam.Cities.Add(hochiminhCity);
                context.Countries.Add(vietnam); 

                // Russia
                var moscow = new Models.City() {
                    Name = "Moscow",
                    Population = (int) (13.01 * MILLION)
                };
                context.Cities.Add(moscow);

                var russia = new Models.Country() {
                    Name = "Russia",
                    Population = (int)(144.3 * MILLION),
                    Cities = new HashSet<Models.City>()
                };              
                russia.Cities.Add(moscow);   
                context.Countries.Add(russia); 

                context.SaveChanges();
            }
            else
            {
                System.Console.WriteLine("---> Data already populated");
            }
        }
    }
}
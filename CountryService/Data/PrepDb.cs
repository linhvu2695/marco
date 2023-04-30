#nullable disable

using CountryService.Constants;
using CountryService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Nest;
using Newtonsoft.Json.Linq;
using Serilog;
using FieldType = Microsoft.VisualBasic.FileIO.FieldType;

namespace CountryService.Data
{
    public static class PrepDb
    {
        public const int MILLION = 1000 * 1000;
        public const int BILLION = 1000 * 1000 * 1000;
        public const string PATH_CITIES_DATA = "Data/cities.csv";
    
        public static void PrepPopulation(IApplicationBuilder app)
        {
            var serviceScope = app.ApplicationServices.CreateScope();

            SeedData(
                serviceScope.ServiceProvider.GetService<AppDbContext>(), 
                serviceScope.ServiceProvider.GetService<IElasticClient>(),
                serviceScope.ServiceProvider.GetService<IConfiguration>()
            );
        }

        private static async void SeedData(AppDbContext context, IElasticClient elasticClient, IConfiguration configuration)
        {
            Log.Logger.Information("--> Applying Migrations...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex) {
                Log.Logger.Warning($"--> Could not run migrations: {ex.Message}");
            }
            var defaultIndex = configuration[Configurations.Const.CONFIG_INDEX_NAME];
            var restCountriesApiUrl = configuration[Configurations.Const.CONFIG_REST_COUNTRIES_API_URL];

            var httpClient = new HttpClient();
            HttpResponseMessage response;
            
            if (!context.Countries.Any()) {
                Log.Logger.Information("--> Seeding Countries data...");

                // Fetch data from external API
                response = await httpClient.GetAsync(restCountriesApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var stringContent = await response.Content.ReadAsStringAsync();
                    JArray countries = JArray.Parse(stringContent);

                    foreach (JObject country in countries)
                    {
                        Country countryModel = new Country();
                        countryModel.Name = (string)country["name"]["common"];
                        countryModel.OfficialName = (string)country["name"]["official"];
                        countryModel.CountryCode = (string)country["cca2"];
                        countryModel.Population = (int)country["population"];
                        context.Countries.Add(countryModel); 
                        context.SaveChanges();
                        elasticClient.Index(countryModel, i => i.Index(defaultIndex).Id(countryModel.Id));
                        Log.Logger.Information($"--> Country saved: {countryModel.Name}, {countryModel.OfficialName}, {countryModel.Population}", DateTime.UtcNow);
                    }
                }
                context.SaveChanges();
                Log.Logger.Information("--> Countries data Fetching process completed");
            }
            else
            {
                Log.Logger.Information("--> Country data already populated");
            }

            if (!context.Cities.Any())
            {
                Log.Logger.Information("--> Seeding Cities data...");
                using (TextFieldParser parser = new TextFieldParser(PATH_CITIES_DATA))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    while (!parser.EndOfData)
                    {
                        string[] cities = parser.ReadFields();
                        foreach (string city in cities)
                        {
                            if (!String.IsNullOrWhiteSpace(city))
                            {
                                await SeedCity(city, context, configuration);
                            }
                        }
                    }
                }
                Log.Logger.Information("--> Cities data Fetching process completed");
            }
            else
            {
                Log.Logger.Information("--> Cities data already populated");
            }
        }

        private async static Task SeedCity(string cityName, AppDbContext context, IConfiguration configuration)
        {
            System.Console.WriteLine($"Seeding city {cityName}");
            var apiNinjasKey = configuration[Configurations.Const.CONFIG_API_NINJAS_KEY];
            var cityApiUrl = configuration[Configurations.Const.CONFIG_API_NINJAS_CITY_URL];

            var httpClient = new HttpClient();
            HttpResponseMessage response;

            httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiNinjasKey);
            response = await httpClient.GetAsync( cityApiUrl + "?name=" + cityName);
            var stringContent = await response.Content.ReadAsStringAsync();
            JArray cities = JArray.Parse(stringContent);
            JToken city = new JObject();

            if (cities != null && cities.Count() > 0)
            {
                city = cities.First;
                City cityModel = new City();
                cityModel.Name = (string)city["name"];
                cityModel.Population = city["population"]?.Value<int>() ?? 0;

                var countryModel = context.Countries.Include(c => c.Cities).FirstOrDefault<Country>(c => c.CountryCode == (string)city["country"]);
                if (countryModel != null) 
                {
                    countryModel.Cities.Add(cityModel);
                    context.Cities.Add(cityModel);
                    context.SaveChanges();
                    Log.Logger.Information($"--> City saved: {cityModel.Name}, {cityModel.Population}, {countryModel.Name}", DateTime.UtcNow);
                }
            }

        }
    }
}
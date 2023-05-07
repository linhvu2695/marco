#nullable disable

using AwsS3.Models;
using AwsS3.Services;
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
        # region Constants
        public const int MILLION = 1000 * 1000;
        public const int BILLION = 1000 * 1000 * 1000;

        public const string PATH_CITIES_DATA = "Data/Cities.csv";
        public const string PATH_STAGING_DATA = "Data/Staging/";
        public const string PATH_AWS_S3_FLAGS = "flags/";

        # endregion
    
        public static void PrepPopulation(IApplicationBuilder app)
        {
            var serviceScope = app.ApplicationServices.CreateScope();

            SeedData(
                serviceScope.ServiceProvider.GetService<AppDbContext>(), 
                serviceScope.ServiceProvider.GetService<IElasticClient>(),
                serviceScope.ServiceProvider.GetService<IConfiguration>(),
                serviceScope.ServiceProvider.GetService<IStorageService>()
            );
        }

        private static async void SeedData(AppDbContext context, IElasticClient elasticClient, IConfiguration configuration, IStorageService storageService)
        {
            Log.Logger.Information("--> Applying Migrations...");
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex) {
                Log.Logger.Warning($"--> Could not run migrations: {ex.Message}");
            }
            
            if (bool.Parse(configuration[ConnStringKeys.Const.CONFIG_SEEDING_COUNTRIES]))
            {
                await SeedCountries(context, elasticClient, configuration);
            }
            
            if (bool.Parse(configuration[ConnStringKeys.Const.CONFIG_SEEDING_CITIES]))
            {
                await SeedCities(context, configuration);
            }
            
            if (bool.Parse(configuration[ConnStringKeys.Const.CONFIG_SEEDING_FLAGS]))
            {
                await SeedFlags(storageService, configuration);
            }
        }

        private async static Task SeedCountries(AppDbContext context, IElasticClient elasticClient, IConfiguration configuration)
        {
            var defaultIndex = configuration[ConnStringKeys.Const.CONFIG_INDEX_NAME];
            var restCountriesApiUrl = configuration[ConnStringKeys.Const.CONFIG_REST_COUNTRIES_API_URL];

            var httpClient = new HttpClient();
            HttpResponseMessage response;

            // Fetch Country data
            Log.Logger.Information("--> Seeding Countries data...");

            // Fetch data from external API
            response = await httpClient.GetAsync(restCountriesApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var stringContent = await response.Content.ReadAsStringAsync();
                JArray countries = JArray.Parse(stringContent);

                foreach (JObject country in countries)
                {
                    bool bAddNewCountry = false;
                    string countryName = (string)country["name"]["common"];

                    var countryModel = context.Countries.FirstOrDefault(c => c.Name == countryName);
                    if (countryModel == null)
                    {
                        bAddNewCountry = true;
                        countryModel = new Country();
                    }

                    countryModel.Name = countryName;
                    countryModel.Name_Official = (string)country["name"]["official"];
                    countryModel.Name_Chinese = (string)country["translations"]?["zho"]?["common"];
                    countryModel.CountryCode = (string)country["cca2"];
                    countryModel.CountryCodeA3 = (string)country["cca3"];
                    countryModel.Population = (int)country["population"];
                    countryModel.FlagPermalink = (string)country["flags"]?["png"];
                    countryModel.FlagDescription = (string)country["flags"]?["alt"];
                    countryModel.Region = (string)country["region"];
                    countryModel.Subregion = (string)country["subregion"];
                    countryModel.Area = (double)country["area"];
                    countryModel.CoatOfArmsPermalink = (string)country["coatOfArms"]?["png"];

                    var countryLanguages = (JObject)country["languages"];
                    if (countryLanguages != null)
                    {
                        countryModel.Languages = string.Join(" ", countryLanguages.Properties().Select(x => x.Value.ToString()));
                    }

                    if (bAddNewCountry)
                    {
                        context.Countries.Add(countryModel); 
                    }
                    
                    context.SaveChanges();
                    elasticClient.Index(countryModel, i => i.Index(defaultIndex).Id(countryModel.Id));
                    
                    Log.Logger.Information($"--> Country added: {countryModel.Name}", DateTime.UtcNow);
                }
            }
            context.SaveChanges();
            Log.Logger.Information("--> Countries data Fetching process completed");
        }

        private async static Task SeedCities(AppDbContext context, IConfiguration configuration)
        {
            
            Log.Logger.Information("--> Seeding Cities data...");
            using (TextFieldParser parser = new TextFieldParser(PATH_CITIES_DATA))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] cities = parser.ReadFields();
                    foreach (string city in cities.Distinct())
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

        private async static Task SeedCity(string cityName, AppDbContext context, IConfiguration configuration)
        {
            if (context.Cities.FirstOrDefault(c => c.Name == cityName) != null)
            {
                return;
            }
            Log.Logger.Information($"--> Seeding city {cityName}");
            var apiNinjasKey = configuration[ConnStringKeys.Const.CONFIG_API_NINJAS_KEY];
            var cityApiUrl = configuration[ConnStringKeys.Const.CONFIG_API_NINJAS_CITY_URL];

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
                cityModel.Latitude = city["latitude"]?.Value<double>() ?? 0;
                cityModel.Longitude = city["longitude"]?.Value<double>() ?? 0;
                if (bool.TryParse((string)city["is_capital"], out bool isCapital))
                {
                    cityModel.IsCapital = isCapital;
                }

                var countryModel = context.Countries.FirstOrDefault<Country>(c => c.CountryCode == (string)city["country"]);
                cityModel.CountryId = countryModel.Id;

                // Add city if it belongs to a country in DB and
                // it has not been added before (the search name and result name of a city can be different)
                if (countryModel != null && context.Cities.FirstOrDefault(c => c.Name == cityModel.Name) == null) 
                {
                    context.Cities.Add(cityModel);
                    context.SaveChanges();
                    Log.Logger.Information($"--> City saved: {cityModel.Name}, {cityModel.Population}, {countryModel.Name}", DateTime.UtcNow);
                }
            }
        }
    
        private async static Task SeedFlags(IStorageService storageService, IConfiguration configuration)
        {
            
            Log.Logger.Information("--> Downloading Flags...");
            var restCountriesApiUrl = configuration[ConnStringKeys.Const.CONFIG_REST_COUNTRIES_API_URL];

            var httpClient = new HttpClient();
            HttpResponseMessage response;

            // Fetch data from external API
            response = await httpClient.GetAsync(restCountriesApiUrl);

            if (response.IsSuccessStatusCode)
            {
                var stringContent = await response.Content.ReadAsStringAsync();
                JArray countries = JArray.Parse(stringContent);

                // Create staging area
                if (!Directory.Exists(PATH_STAGING_DATA))
                {
                    Directory.CreateDirectory(PATH_STAGING_DATA);
                }

                foreach (JObject country in countries)
                {
                    // Download
                    string flagImageUrl = (string)country["flags"]["png"];
                    string fileName = ((string)country["cca2"]).ToLower() + Path.GetExtension(flagImageUrl);
                    string flagImagePath = Path.Combine(PATH_STAGING_DATA, fileName);

                    Log.Logger.Information($"--> Downloading at {flagImageUrl}");
                    HttpResponseMessage flagDownloadResponse = await httpClient.GetAsync(flagImageUrl);
                    byte[] flagImageBytes = await flagDownloadResponse.Content.ReadAsByteArrayAsync();
                    Log.Logger.Information($"--> Download at {flagImageUrl} completed");

                    // Upload to S3
                    IFormFile file = new FormFile(
                        baseStream : new MemoryStream(flagImageBytes), 
                        baseStreamOffset : 0,
                        length : flagImageBytes.Length,
                        name : "fileUpload",
                        fileName : fileName
                    );

                    await using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    S3Object s3Object = new S3Object() {
                        BucketName = configuration[ConnStringKeys.Const.CONFIG_AWS_S3_BUCKET_NAME],
                        InputStream = memoryStream,
                        Name = PATH_AWS_S3_FLAGS + file.FileName
                    };

                    var credentials = new AwsCredentials(){
                        AwsKey = configuration[ConnStringKeys.Const.CONFIG_AWS_S3_ACCESS_KEY],
                        AwsSecretKey = configuration[ConnStringKeys.Const.CONFIG_AWS_S3_SECRET_KEY]
                    };

                    var result = await storageService.UploadFileAsync(s3Object, credentials);
                    Log.Logger.Information($"--> Upload to S3 from {fileName} completed");
                }

                // Remove staging area
                if (Directory.Exists(PATH_STAGING_DATA))
                {
                    Directory.Delete(PATH_STAGING_DATA, true);
                }
            }       
        }
    }
}
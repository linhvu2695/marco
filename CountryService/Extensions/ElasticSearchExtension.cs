#nullable disable

using CountryService.Models;
using Nest;
using Serilog;

namespace CountryService.Extensions
{
    public static class ElasticSearchExtension
    {
        public static void AddElasticSearch(this IServiceCollection services, IConfiguration configuration)
        {
            Log.Logger.Information("--> Adding Elastic Search...", DateTime.UtcNow);   
            var url = configuration["ELKConfiguration:Uri"];
            var defaultIndex = configuration["ELKConfiguration:index"];

            var settings = new ConnectionSettings(new Uri(url)).PrettyJson().DefaultIndex(defaultIndex);
            AddDefaultMappings(settings);

            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);

            CreateIndex(client, defaultIndex);
        }

        private static void AddDefaultMappings(ConnectionSettings settings)
        {
            settings.DefaultMappingFor<Country>(c => c
                .Ignore(x => x.Population));
        }

        private static void CreateIndex(ElasticClient client, string indexName)
        {
            client.Indices.Create(indexName, i => i.Map<Country>(x => x.AutoMap()));
        }
    }

    
}
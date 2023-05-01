using AutoMapper;
using AwsS3.Services;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Models;
using CountryService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ICountryRepo _countryRepo;
        private readonly ICityRepo _cityRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<CountryController> _logger;
        private readonly IElasticClient _elasticClient;
        private readonly ICacheService _cacheService;
        private readonly IStorageService _storageService;

        public const int TIME_CACHE_EXPIRY_SECONDS = 60;

        public CountryController(IConfiguration configuration, ICountryRepo countryRepo, ICityRepo cityRepo, IMapper mapper, ILogger<CountryController> logger, IElasticClient elasticClient, ICacheService cacheService, IStorageService storageService)
        {
            _configuration = configuration;
            _countryRepo = countryRepo;
            _cityRepo = cityRepo;
            _mapper = mapper;
            _logger = logger;
            _elasticClient = elasticClient;
            _cacheService = cacheService;
            _storageService = storageService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CountryReadDto>> GetCountries()
        {
            _logger.LogInformation("--> Getting Countries...", DateTime.UtcNow);
            
            var countryItem = _countryRepo.GetAllCountries();

            return Ok(_mapper.Map<IEnumerable<CountryReadDto>>(countryItem)); 
        }

        [HttpGet("{id}", Name="GetCountryById")]
        public ActionResult<CountryReadDto> GetCountryById(int id)
        {
            _logger.LogInformation($"--> Getting Country by Id: {id}...", DateTime.UtcNow);

            var countryItem = _countryRepo.GetCountryById(id);
            if (countryItem != null) 
            {
                return Ok(_mapper.Map<Country,CountryReadDto>(countryItem));
            }
            return NotFound();
        }

        [HttpGet("name/{name}", Name="GetCountryByName")]
        public ActionResult<CountryReadDto> GetCountryByName(string name)
        {
            _logger.LogInformation($"--> Getting Country by Name: {name}...", DateTime.UtcNow);

            // Check cache
            var cacheData = _cacheService.GetData<Country>(name);
            if (cacheData != null)
            {
                _logger.LogInformation("--> Cache hit!");
                _cacheService.SetData<Country>(name, cacheData, DateTimeOffset.Now.AddSeconds(TIME_CACHE_EXPIRY_SECONDS));
                return Ok(cacheData);
            }

            // Check database
            var countryItem = _countryRepo.GetCountryByName(name);
            if (countryItem != null)
            {
                _logger.LogInformation("--> Cache missed!");
                _cacheService.SetData<Country>(name, countryItem, DateTimeOffset.Now.AddSeconds(TIME_CACHE_EXPIRY_SECONDS));
                return Ok(countryItem);
            }
            return NotFound();
        }

        [HttpGet("{id}/city")]
        public ActionResult<IEnumerable<CityReadDto>> GetCitiesFromCountry(int id)
        {
            _logger.LogInformation($"--> Getting Cities from Country {id}...", DateTime.UtcNow);

            if (!_countryRepo.CountryExists(id))
            {
                return NotFound();
            }

            var cities = _countryRepo.GetCitiesFromCountry(id);
            return Ok(_mapper.Map<IEnumerable<CityReadDto>>(cities));
        }

        [HttpPost]
        public ActionResult<CountryCreateDto> CreateCountry(CountryCreateDto countryCreateDto)
        {
            System.Console.WriteLine("---> Creating Country...");

            var countryModel = _mapper.Map<Country>(countryCreateDto);
            _countryRepo.CreateCountry(countryModel);
            _countryRepo.SaveChanges();

            // Index record to ElasticSearch
            _elasticClient.Index(countryModel, c => c.Index(_configuration[Configurations.Const.CONFIG_INDEX_NAME]).Id(countryModel.Id));

            // Set cache
            _cacheService.SetData<Country>(countryModel.Name, countryModel, DateTimeOffset.Now.AddSeconds(TIME_CACHE_EXPIRY_SECONDS));

            var countryReadDto = _mapper.Map<CountryReadDto>(countryModel);
            return CreatedAtRoute(nameof(GetCountryById), new {Id = countryReadDto.Id}, countryReadDto);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteCountry(int id)
        {
            _logger.LogInformation($"--> Deleting Country with ID: {id}...", DateTime.UtcNow);

            var countryItem = _countryRepo.GetCountryById(id);
            if (countryItem == null)
            {
                return NotFound();
            }

            try
            {
                _countryRepo.DeleteCountry(countryItem);
                _countryRepo.SaveChanges();
            }
            catch (DbUpdateException ex) 
            {
                if (ex.InnerException is SqlException sqlException && sqlException.Number == Exceptions.Const.SQL_EXCEPTION_CODE_FOREIGN_KEY_CONSTRAINT_VIOLATION)
                {
                    return Conflict(Messages.Const.MESSAGE_DELETE_COUNTRY_CONFLICT);
                }
            }

            // Delete record from ElasticSearch
            _elasticClient.Delete<Country>(id, c => c.Index(_configuration[Configurations.Const.CONFIG_INDEX_NAME]));

            // Invalidate cache
            _cacheService.RemoveData(countryItem.Name);

            return NoContent();
        }

        [HttpPut("{id}/population")]
        public ActionResult UpdateCountryPopulation (int id, CountryUpdatePopulationDto countryUpdatePopulationDto)
        {
            _logger.LogInformation($"---> Updating Population at Country {id}...", DateTime.UtcNow);

            var countryItem = _countryRepo.GetCountryById(id);
            if (countryItem == null)
            {
                return NotFound();
            }

            _countryRepo.UpdateCountryPopulation(id, countryUpdatePopulationDto.Population);
            _countryRepo.SaveChanges();

            // Invalidate cache
            _cacheService.RemoveData(countryItem.Name);
            
            return NoContent();
        }

        [HttpGet("query")]
        public async Task<IActionResult> Search(string keyword)
        {
            // Search ElasticSearch
            _logger.LogInformation($"--> Searching with keyword: {keyword}...", DateTime.UtcNow);
            var results = await _elasticClient.SearchAsync<Country>(
                s => s.Query(
                    q => q.QueryString(
                        d => d.Query(keyword)
                    )
                ).Size(1000)
            );
            var resultIds = results.Documents.Select(c => c.Id).ToList();

            // Search database
            List<Country> resultCountries = new List<Country>();
            foreach(int id in resultIds)
            {
                var countryItem = _countryRepo.GetCountryById(id);
                if (countryItem != null)
                {
                    resultCountries.Add(countryItem);
                }
            }
            return Ok(resultCountries);
        }
    }
}
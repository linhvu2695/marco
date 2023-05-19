using AutoMapper;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly ICityRepo _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<CityController> _logger;

        public CityController(ICityRepo countryRepo, IMapper mapper, ILogger<CityController> logger)
        {
            _repository = countryRepo;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CityReadDto>> GetCities()
        {
            _logger.LogInformation("--> Getting Cities...", DateTime.UtcNow);
            
            var cityItem = _repository.GetAllCities();

            return Ok(_mapper.Map<IEnumerable<CityReadDto>>(cityItem)); 
        }

        [HttpGet("{id}", Name="GetCityById")]
        public ActionResult<CountryReadDto> GetCityById(int id)
        {
            _logger.LogInformation("---> Getting City by Id...", DateTime.UtcNow);

            var cityItem = _repository.GetCityById(id);

            if (cityItem != null) 
            {
                return Ok(_mapper.Map<City,CityReadDto>(cityItem));
            }
            return NotFound();
        }

        [HttpPost]
        public ActionResult<CityCreateDto> CreateCity(CityCreateDto cityCreateDto)
        {
            _logger.LogInformation("---> Creating City...", DateTime.UtcNow);

            var cityModel = _mapper.Map<City>(cityCreateDto);
            _repository.CreateCity(cityModel);
            _repository.SaveChanges();

            var cityReadDto = _mapper.Map<CityReadDto>(cityModel);
            return CreatedAtRoute(nameof(GetCityById), new {Id = cityReadDto.Id}, cityReadDto);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteCity(int id)
        {
            _logger.LogInformation($"--> Deleting City with ID: {id}...", DateTime.UtcNow);

            var cityItem = _repository.GetCityById(id);
            if (cityItem == null)
            {
                return NotFound();
            }

            _repository.DeleteCity(cityItem);
            _repository.SaveChanges();

            return NoContent();
        }
    }
}
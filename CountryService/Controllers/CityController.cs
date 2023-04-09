using AutoMapper;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Models;
using CountryService.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly ICityRepo _repository;
        private readonly IMapper _mapper;

        public CityController(ICityRepo countryRepo, IMapper mapper)
        {
            _repository = countryRepo;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CityReadDto>> GetCities()
        {
            System.Console.WriteLine("--> Getting Cities...");
            
            var cityItem = _repository.GetAllCities();

            return Ok(_mapper.Map<IEnumerable<CityReadDto>>(cityItem)); 
        }

        [HttpGet("{id}", Name="GetCityById")]
        public ActionResult<CountryReadDto> GetCityById(int id)
        {
            System.Console.WriteLine("---> Getting City by Id...");

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
            System.Console.WriteLine("---> Creating City...");

            var cityModel = _mapper.Map<City>(cityCreateDto);
            _repository.CreateCity(cityModel);
            _repository.SaveChanges();

            var cityReadDto = _mapper.Map<CityReadDto>(cityModel);
            return CreatedAtRoute(nameof(GetCityById), new {Id = cityReadDto.Id}, cityReadDto);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteCity(int id)
        {
            System.Console.WriteLine($"--> Deleting City with ID: {id}...");

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
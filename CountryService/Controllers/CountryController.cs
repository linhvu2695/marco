using AutoMapper;
using CountryService.Constants;
using CountryService.Data;
using CountryService.Dtos;
using CountryService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CountryService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly ICountryRepo _countryRepo;
        private readonly ICityRepo _cityRepo;
        private readonly IMapper _mapper;

        public CountryController(ICountryRepo countryRepo, ICityRepo cityRepo, IMapper mapper)
        {
            _countryRepo = countryRepo;
            _cityRepo = cityRepo;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CountryReadDto>> GetCountries()
        {
            System.Console.WriteLine("--> Getting Countries...");
            
            var countryItem = _countryRepo.GetAllCountries();

            return Ok(_mapper.Map<IEnumerable<CountryReadDto>>(countryItem)); 
        }

        [HttpGet("{id}", Name="GetCountryById")]
        public ActionResult<CountryReadDto> GetCountryById(int id)
        {
            System.Console.WriteLine("---> Getting Country by Id...");

            var countryItem = _countryRepo.GetCountryById(id);

            if (countryItem != null) 
            {
                return Ok(_mapper.Map<Country,CountryReadDto>(countryItem));
            }
            return NotFound();
        }

        [HttpGet("{id}/city")]
        public ActionResult<IEnumerable<CityReadDto>> GetCitiesFromCountry(int id)
        {
            System.Console.WriteLine($"---> Getting Cities from Country {id}...");

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

            var countryReadDto = _mapper.Map<CountryReadDto>(countryModel);
            return CreatedAtRoute(nameof(GetCountryById), new {Id = countryReadDto.Id}, countryReadDto);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteCountry(int id)
        {
            System.Console.WriteLine($"--> Deleting Country with ID: {id}...");

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
                if (ex.InnerException is SqlException sqlException && sqlException.Number == Exceptions.SQL_EXCEPTION_CODE_FOREIGN_KEY_CONSTRAINT_VIOLATION)
                {
                    return Conflict(Messages.MESSAGE_DELETE_COUNTRY_CONFLICT);
                }
            }

            return NoContent();
        }
    }
}
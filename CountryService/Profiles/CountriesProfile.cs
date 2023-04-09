using AutoMapper;
using CountryService.Dtos;
using CountryService.Models;

namespace CountryService.Profiles
{
    public class CountriesProfile : Profile
    {
        public CountriesProfile()
        {
            CreateMap<Country, CountryReadDto>();
            CreateMap<CountryCreateDto,Country>();
        }
    }
}
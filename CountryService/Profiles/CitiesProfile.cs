using AutoMapper;
using CountryService.Dtos;
using CountryService.Models;

namespace CountryService.Profiles
{
    public class CitiesProfile : Profile
    {
        public CitiesProfile()
        {
            CreateMap<City, CityReadDto>();
            CreateMap<CityCreateDto,City>();
        }
    }
}
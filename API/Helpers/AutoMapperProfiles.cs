using AutoMapper;
using API.Entities;
using API.DTOs;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDto>()
            .ForMember(dest => dest.PhotoUrl,
                       options => options.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url))
            .ForMember(dest => dest.Age,
                       options => options.MapFrom(src => src.DateOfBirth.CalculateAge()));


            CreateMap<Photo, PhotoDto>();
        }
    }
}
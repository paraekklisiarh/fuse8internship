using AutoMapper;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;

public interface IMapFrom<T>
{
    void MappingFrom(Profile profile)
    {
        profile.CreateMap(typeof(T), GetType());
    }
}
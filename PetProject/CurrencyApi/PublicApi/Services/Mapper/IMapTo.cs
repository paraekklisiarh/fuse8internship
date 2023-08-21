using AutoMapper;

namespace Fuse8_ByteMinds.SummerSchool.PublicApi.Services.Mapper;

public interface IMapTo<T>
{
    void MappingTo(Profile profile)
    {
        profile.CreateMap(GetType(), typeof(T));
    }
}
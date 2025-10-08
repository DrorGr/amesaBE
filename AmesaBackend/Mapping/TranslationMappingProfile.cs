using AutoMapper;
using AmesaBackend.Models;
using AmesaBackend.DTOs;

namespace AmesaBackend.Mapping
{
    public class TranslationMappingProfile : Profile
    {
        public TranslationMappingProfile()
        {
            CreateMap<Translation, TranslationDto>()
                .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            CreateMap<Language, LanguageDto>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.NativeName, opt => opt.MapFrom(src => src.NativeName))
                .ForMember(dest => dest.FlagUrl, opt => opt.MapFrom(src => src.FlagUrl))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
                .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder));

            CreateMap<CreateTranslationRequest, Translation>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.LanguageCode, opt => opt.MapFrom(src => src.LanguageCode))
                .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<CreateLanguageRequest, Language>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.NativeName, opt => opt.MapFrom(src => src.NativeName))
                .ForMember(dest => dest.FlagUrl, opt => opt.MapFrom(src => src.FlagUrl))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
                .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}

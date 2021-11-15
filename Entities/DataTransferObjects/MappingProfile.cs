using AutoMapper;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DataTransferObjects
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Employee, EmployeeDto>();
            CreateMap<Company, CompanyDto>()
                .ForMember(c => c.FullAddress,
                opt => opt.MapFrom(x => string.Join(' ', x.Address, x.Country)));
            CreateMap<CompanyForCreationDTO, Company>();
            CreateMap<EmployeeForCreationDTO, Employee>();
            CreateMap<EmployeeForUpdateDTO, Employee>().ReverseMap();
            CreateMap<CompanyForCreationDTO, Company>();
        }
    }
}

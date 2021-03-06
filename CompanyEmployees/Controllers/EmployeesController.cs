using AutoMapper;
using Contracts;
using Entities.DataTransferObjects;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompanyEmployees.Controllers
{
    [Route("api/companies/{companyId}/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly IRepositoryManager _repository;
        private readonly ILoggerManager _logger;
        private readonly IMapper _mapper;

        public EmployeesController(IRepositoryManager repository, ILoggerManager logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        [HttpHead]
        public async Task<IActionResult> GetEmployeesForCompany(Guid companyId, [FromQuery]EmployeeParameters employeeParameters)
        {
            if(!employeeParameters.ValidAgeRange)
            {
                return BadRequest("Max age can't be less than the min age");
            }
            var company = await  _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if (company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }
            var employeesFromDb = await _repository.Employee.GetEmployeesAsync(companyId, employeeParameters, trackChanges: false);
            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employeesFromDb);
            return Ok(employeesDto);
        }

        [HttpGet("{id}", Name = "GetEmployeeForCompany")]
        public async Task<IActionResult> GetEmployeeForCompany(Guid companyId, Guid id)
        {
            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id:{companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeDb = _repository.Employee.GetEmployee(companyId, id, trackChanges: false);
            if(employeeDb == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database.");
                return NotFound();
            }

            var employee = _mapper.Map<EmployeeDto>(employeeDb);
            return Ok(employee);
        }

        [HttpPost]
        public async Task<IActionResult>  CreateEmployeeForCompany(Guid companyId, [FromBody]EmployeeForCreationDTO employee)
        {
            if(employee == null)
            {
                _logger.LogError("EmployeeForCreation sent from the client is null");
                return BadRequest("EmployeeForCreationDTO object is null");
            }

            if(!ModelState.IsValid)
            {
                _logger.LogError("Invalid model for the EmployeeForCreationDto object");
                return UnprocessableEntity(ModelState);
            }

            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} does not exist in the database");
                return NotFound();
            }
            var employeeEntity = _mapper.Map<Employee>(employee);
            _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
            await _repository.SaveAsync();

            var employeeToReturn = _mapper.Map<EmployeeDto>(employeeEntity);
            return CreatedAtRoute("GetEmployeeForCompany", new { companyId, id = employeeToReturn.Id}, employeeToReturn);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>  DeleteEmployeeForCompany(Guid companyId, Guid id)
        {
            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} doesn't exist in the database.");
                return NotFound();
            }

            var employeeForCompany = _repository.Employee.GetEmployee(companyId, id, trackChanges: false);
            if(employeeForCompany == null)
            {
                _logger.LogInfo($"Employee with id: {id} doesn't exist in the database");
                return NotFound();
            }
            _repository.Employee.DeleteEmployee(employeeForCompany);
            await _repository.SaveAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult>  UpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody]EmployeeForUpdateDTO employee)
        {
            if(employee == null)
            {
                _logger.LogError("EmployeeForUpdateDTO object sent from client is null.");
                return BadRequest("EmployeeForUpdateDTO is null");
            }

            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} does not exist in the database.");
                return NotFound();
            }

            var employeeEntity = _repository.Employee.GetEmployee(companyId, id, trackChanges: true);
            if(employeeEntity == null)
            {
                _logger.LogInfo($"Employee with id: {id} does not exist in the database.");
                return NotFound();
            }

            _mapper.Map(employee, employeeEntity);
            await _repository.SaveAsync();

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult>  PartiallyUpdateEmployeeForCompany(Guid companyId, Guid id, [FromBody]JsonPatchDocument<EmployeeForUpdateDTO> patchDoc)
        {
            if(patchDoc == null)
            {
                _logger.LogError($"patchDoc object sent from client is null.");
                return BadRequest("patchDoc object is null");
            }

            var company = await _repository.Company.GetCompanyAsync(companyId, trackChanges: false);
            if(company == null)
            {
                _logger.LogInfo($"Company with id: {companyId} does not exist in the database.");
                return NotFound();
            }

            var employeeEntity = _repository.Employee.GetEmployee(companyId, id, trackChanges: true);
            if(employeeEntity == null)
            {
                _logger.LogInfo($"Employee with id: {id} does not exist in the database.");
                return NotFound();
            }

            var employeeToPatch = _mapper.Map<EmployeeForUpdateDTO>(employeeEntity);
            patchDoc.ApplyTo(employeeToPatch);
            TryValidateModel(employeeToPatch);
            if (!ModelState.IsValid)
            {
                _logger.LogError("Invalid model for the patch document");
                return UnprocessableEntity(ModelState);
            }
            _mapper.Map(employeeToPatch, employeeEntity);
            await _repository.SaveAsync();
            return NoContent();
        }
    }
}

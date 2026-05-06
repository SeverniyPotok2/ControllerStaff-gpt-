using ControllerStaff.Core.Abstraction;
using ControllerStaff.Core.DTOs;
using ControllerStaff.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControllerStaff.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentRepository _repository;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentRepository repository, ILogger<DepartmentsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Department>>> GetAll()
        {
            var departments = await _repository.GetAllAsync();
            return Ok(departments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetById(int id)
        {
            var department = await _repository.GetByIdAsync(id);
            if (department == null) return NotFound();
            return Ok(department);
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<Department>>> GetActive()
        {
            var departments = await _repository.GetActiveByDepartmentAsync(int.MaxValue);
            // Фикс: можно добавить GetActiveDepartmentsAsync() в репозиторий
            return Ok(departments);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateDepartmentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Название обязательно.");

            var existing = await _repository.DepartmentExistsAsync(dto.Name);
            if (existing) return BadRequest("Отдел с таким именем уже существует.");

            var department = new Department(
                0, dto.Name, DateTime.UtcNow);

            var created = await _repository.CreateAsync(department);
            if (!created) return StatusCode(500, "Ошибка создания.");

            _logger.LogInformation("Отдел создан: {Name}", dto.Name);
            return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentDto dto)
        {
            if (id != dto.Id) return BadRequest("ID не совпадает.");

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var updated = new Department(
                dto.Id, dto.Name, existing.CreatedAt);

            var result = await _repository.UpdateAsync(updated);
            if (!result) return BadRequest("Ошибка обновления.");

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _repository.DeactivateAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }
    }
}
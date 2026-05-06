using ControllerStaff.Core.Abstraction;
using ControllerStaff.Core.DTOs;
using ControllerStaff.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControllerStaff.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CriteriaController : ControllerBase
    {
        private readonly ICriteriaRepository _repository;
        private readonly ILogger<CriteriaController> _logger;

        public CriteriaController(ICriteriaRepository repository, ILogger<CriteriaController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Criteria>>> GetAll()
        {
            return Ok(await _repository.GetAllAsync());
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<Criteria>>> GetActive()
        {
            return Ok(await _repository.GetActiveCriteriaAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Criteria>> GetById(int id)
        {
            var criterion = await _repository.GetByIdAsync(id);
            if (criterion == null) return NotFound();
            return Ok(criterion);
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateCriteriaDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Название обязательно.");

            if (await _repository.CriteriaExistsAsync(dto.Title))
                return BadRequest("Критерий с таким названием уже существует.");

            var criterion = new Criteria(
                0, dto.Title, dto.Description, dto.MaxScore, true);

            var created = await _repository.CreateAsync(criterion);
            if (!created) return StatusCode(500, "Ошибка создания.");

            _logger.LogInformation("Критерий создан: {Title}", dto.Title);
            return CreatedAtAction(nameof(GetById), new { id = criterion.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCriteriaDto dto)
        {
            if (id != dto.Id) return BadRequest("ID не совпадает.");

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var updated = new Criteria(
                dto.Id, dto.Title, dto.Description, dto.MaxScore, dto.IsActive);

            var result = await _repository.UpdateAsync(updated);
            return result ? NoContent() : BadRequest("Ошибка обновления.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _repository.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var result = await _repository.ActivateAsync(id);
            return result ? NoContent() : NotFound();
        }

        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var result = await _repository.DeactivateAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
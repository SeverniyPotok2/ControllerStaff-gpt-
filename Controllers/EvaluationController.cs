using ControllerStaff.Core.Abstraction;
using ControllerStaff.Core.DTOs;
using ControllerStaff.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ControllerStaff.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationsController : ControllerBase
    {
        private readonly IEvaluationRepository _evalRepo;
        private readonly IEvaluationItemRepository _itemRepo;
        private readonly ILogger<EvaluationsController> _logger;

        public EvaluationsController(
            IEvaluationRepository evalRepo,
            IEvaluationItemRepository itemRepo,
            ILogger<EvaluationsController> logger)
        {
            _evalRepo = evalRepo;
            _itemRepo = itemRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<Evaluation>>> GetAll()
        {
            return Ok(await _evalRepo.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Evaluation>> GetById(int id)
        {
            var evaluation = await _evalRepo.GetByIdAsync(id);
            if (evaluation == null) return NotFound();
            return Ok(evaluation);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<List<Evaluation>>> GetByEmployeeId(int employeeId)
        {
            return Ok(await _evalRepo.GetByEmployeeIdAsync(employeeId));
        }

        [HttpGet("reviewer/{reviewerId}")]
        public async Task<ActionResult<List<Evaluation>>> GetByReviewerId(int reviewerId)
        {
            return Ok(await _evalRepo.GetByReviewerIdAsync(reviewerId));
        }

        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] CreateEvaluationDto dto)
        {
            if (dto.EmployeeId <= 0 || dto.ReviewerId <= 0)
                return BadRequest("ID сотрудника и оценивающего должны быть положительными.");

            if (dto.EmployeeId == dto.ReviewerId)
                return BadRequest("Сотрудник не может оценивать сам себя.");

            if (await _evalRepo.EvaluationExistsAsync(dto.EmployeeId, dto.ReviewerId))
                return BadRequest("Оценка уже существует.");

            var evaluation = new Evaluation(
                0, dto.EmployeeId, dto.ReviewerId, null, dto.Comment, DateTime.UtcNow, DateTime.UtcNow);

            var created = await _evalRepo.CreateAsync(evaluation);
            if (!created) return StatusCode(500, "Ошибка создания.");

            _logger.LogInformation("Оценка создана: Employee={EmployeeId}, Reviewer={ReviewerId}",
                dto.EmployeeId, dto.ReviewerId);
            return CreatedAtAction(nameof(GetById), new { id = evaluation.Id }, evaluation.Id);
        }

        [HttpPut("{id}/items")]
        public async Task<IActionResult> UpdateItems(int id, [FromBody] List<CreateEvaluationItemDto> items)
        {
            var evaluation = await _evalRepo.GetByIdAsync(id);
            if (evaluation == null) return NotFound();

            foreach (var itemDto in items)
            {
                if (await _itemRepo.EvaluationItemExistsAsync(id, itemDto.CriteriaId))
                {
                    return BadRequest($"Item для CriteriaId={itemDto.CriteriaId} уже существует.");
                }

                var item = new EvaluationItem(
                     id, itemDto.CriteriaId, itemDto.Score, itemDto.Score, itemDto.Comment);
                await _itemRepo.CreateAsync(item);

            }

            // Рассчитываем итоговый балл
            await _evalRepo.CompleteEvaluationAsync(id);

            return NoContent();
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var result = await _evalRepo.CompleteEvaluationAsync(id);
            return result ? NoContent() : NotFound();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEvaluationDto dto)
        {
            if (id != dto.Id) return BadRequest("ID не совпадает.");

            var existing = await _evalRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var updated = new Evaluation(
                dto.Id, dto.EmployeeId, dto.ReviewerId, dto.TotalScore, dto.Comment, dto.EvaluatedAt, existing.CreatedAt);

            var result = await _evalRepo.UpdateAsync(updated);
            return result ? NoContent() : BadRequest();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _evalRepo.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
    }
}
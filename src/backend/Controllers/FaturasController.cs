using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaturasController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FaturamentoService _fat;

        public FaturasController(AppDbContext db, FaturamentoService fat)
        {
            _db = db;
            _fat = fat;
        }

        private static bool CompetenciaValida(string? comp) =>
            !string.IsNullOrWhiteSpace(comp) &&
            Regex.IsMatch(comp, @"^\d{4}-(0[1-9]|1[0-2])$");

        private ActionResult ValidationProblem400(string message) =>
            Problem(detail: message,
                    title: "Requisição inválida",
                    statusCode: StatusCodes.Status400BadRequest);

        [HttpPost("gerar")]
        public async Task<IActionResult> Gerar([FromBody] GerarFaturaRequest req, CancellationToken ct)
        {
            if (req is null)
                return ValidationProblem400("Corpo da requisição ausente.");

            if (!CompetenciaValida(req.Competencia))
                return ValidationProblem400("O campo 'competencia' deve estar no formato yyyy-MM.");

            try
            {
                var criadas = await _fat.GerarAsync(req.Competencia, ct);
                return Ok(new { criadas = criadas.Count });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Conflito de concorrência ao gerar faturas. Tente novamente.");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível gerar as faturas. Verifique os dados e tente novamente.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? competencia = null, CancellationToken ct = default)
        {
            var q = _db.Faturas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(competencia))
            {
                if (!CompetenciaValida(competencia))
                    return ValidationProblem400("O parâmetro 'competencia' deve estar no formato yyyy-MM.");

                q = q.Where(f => f.Competencia == competencia);
            }

            var list = await q
                .OrderByDescending(f => f.CriadaEm)
                .Select(f => new
                {
                    f.Id,
                    f.Competencia,
                    f.ClienteId,
                    f.Valor,
                    f.CriadaEm,
                    qtdVeiculos = _db.FaturasVeiculos.AsNoTracking().Count(x => x.FaturaId == f.Id)
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        [HttpGet("{id:guid}/placas")]
        public async Task<IActionResult> Placas(Guid id, CancellationToken ct = default)
        {
            var existeFatura = await _db.Faturas.AsNoTracking().AnyAsync(f => f.Id == id, ct);
            if (!existeFatura)
                return NotFound("Fatura não encontrada.");

            var placas = await _db.FaturasVeiculos
                .AsNoTracking()
                .Where(x => x.FaturaId == id)
                .Join(_db.Veiculos.AsNoTracking(),
                      fv => fv.VeiculoId,
                      v  => v.Id,
                      (fv, v) => v.Placa)
                .OrderBy(p => p)
                .ToListAsync(ct);

            return Ok(placas);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;

        public VeiculosController(AppDbContext db, PlacaService placa)
        {
            _db = db;
            _placa = placa;
        }

        private ActionResult ValidationProblem400(string message) =>
            Problem(detail: message, title: "Requisição inválida", statusCode: StatusCodes.Status400BadRequest);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VeiculoCreateDto dto, CancellationToken ct)
        {
            if (dto is null) return ValidationProblem400("Corpo da requisição ausente.");
            if (dto.ClienteId == Guid.Empty) return ValidationProblem400("O campo 'clienteId' é obrigatório.");

            var clienteExiste = await _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == dto.ClienteId, ct);
            if (!clienteExiste) return ValidationProblem400("Cliente associado não existe.");

            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return ValidationProblem400("Placa inválida.");
            var placaJaExiste = await _db.Veiculos.AsNoTracking().AnyAsync(v => v.Placa == placa, ct);
            if (placaJaExiste) return Conflict("Já existe um veículo com essa placa.");

            if (dto.Ano is int ano && (ano < 1900 || ano > DateTime.UtcNow.Year + 1))
                return ValidationProblem400($"Ano fora do intervalo esperado: {ano}.");

            var v = new Veiculo
            {
                Placa = placa,
                Modelo = string.IsNullOrWhiteSpace(dto.Modelo) ? null : dto.Modelo.Trim(),
                Ano = dto.Ano,
                ClienteId = dto.ClienteId,
                DataInclusao = DateTime.UtcNow
            };

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                _db.Veiculos.Add(v);
                await _db.SaveChangesAsync(ct);

                var hist = new VeiculoHistorico
                {
                    VeiculoId = v.Id,
                    ClienteId = v.ClienteId,
                    Inicio = v.DataInclusao,
                    Fim = null
                };
                _db.Set<VeiculoHistorico>().Add(hist);

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return CreatedAtAction(nameof(GetById), new { id = v.Id }, v);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return BadRequest("Não foi possível criar o veículo. Verifique os dados e tente novamente.");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var v = await _db.Veiculos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return v == null ? NotFound("Veículo não encontrado.") : Ok(v);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] VeiculoUpdateDto dto, CancellationToken ct)
        {
            if (dto is null) return ValidationProblem400("Corpo da requisição ausente.");

            var v = await _db.Veiculos.FindAsync([id], ct);
            if (v == null) return NotFound("Veículo não encontrado.");

           if (dto.ClienteId == Guid.Empty) return ValidationProblem400("O campo 'clienteId' é obrigatório.");
            var clienteExiste = await _db.Clientes.AsNoTracking().AnyAsync(c => c.Id == dto.ClienteId, ct);
            if (!clienteExiste) return ValidationProblem400("Cliente associado não existe.");

            var placa = _placa.Sanitizar(dto.Placa);
            if (!_placa.EhValida(placa)) return ValidationProblem400("Placa inválida.");

            var placaDuplicada = await _db.Veiculos.AsNoTracking().AnyAsync(x => x.Placa == placa && x.Id != id, ct);
            if (placaDuplicada) return Conflict("Já existe um veículo com essa placa.");

           if (dto.Ano is int ano && (ano < 1900 || ano > DateTime.UtcNow.Year + 1))
                return ValidationProblem400($"Ano fora do intervalo esperado: {ano}.");

            var clienteMudou = v.ClienteId != dto.ClienteId;
            v.Placa = placa;
            v.Modelo = string.IsNullOrWhiteSpace(dto.Modelo) ? null : dto.Modelo.Trim();
            v.Ano = dto.Ano;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                if (clienteMudou)
                {
                    var aberto = await _db.Set<VeiculoHistorico>()
                        .Where(h => h.VeiculoId == v.Id && h.Fim == null)
                        .OrderByDescending(h => h.Inicio)
                        .FirstOrDefaultAsync(ct);

                    if (aberto != null)
                    {
                        aberto.Fim = DateTime.UtcNow;
                        _db.Update(aberto);
                    }

                    v.ClienteId = dto.ClienteId;

                    var novo = new VeiculoHistorico
                    {
                        VeiculoId = v.Id,
                        ClienteId = dto.ClienteId,
                        Inicio = DateTime.UtcNow,
                        Fim = null
                    };
                    _db.Set<VeiculoHistorico>().Add(novo);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                return Ok(v);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                return Conflict("Conflito de concorrência ao atualizar o veículo. Recarregue e tente novamente.");
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return BadRequest("Não foi possível salvar as alterações do veículo.");
            }
        }
    }
}
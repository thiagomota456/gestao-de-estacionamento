using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Dtos;
using Parking.Api.Models;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ClientesController(AppDbContext db) => _db = db;

        private static string? NormalizePhone(string? tel)
            => string.IsNullOrWhiteSpace(tel) ? null : Regex.Replace(tel, "[^0-9]", "");

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanho = 10,
            [FromQuery] string? filtro = null,
            [FromQuery] string mensalista = "all",
            CancellationToken ct = default)
        {
            if (pagina < 1 || tamanho < 1 || tamanho > 200)
                return ValidationProblem("Parâmetros de paginação inválidos. 'pagina' >= 1 e 'tamanho' entre 1 e 200.");

            var q = _db.Clientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
                q = q.Where(c => c.Nome.Contains(filtro));

            if (mensalista == "true") q = q.Where(c => c.Mensalista);
            else if (mensalista == "false") q = q.Where(c => !c.Mensalista);
            
            var total = await q.CountAsync(ct);
            var itens = await q
                .OrderBy(c => c.Nome)
                .Skip((pagina - 1) * tamanho)
                .Take(tamanho)
                .ToListAsync(ct);

            return Ok(new { total, itens });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto, CancellationToken ct)
        {
            if (dto is null)
                return ValidationProblem("Corpo da requisição ausente.");

            if (string.IsNullOrWhiteSpace(dto.Nome))
                return ValidationProblem("O campo 'Nome' é obrigatório.");
            
            if (string.IsNullOrWhiteSpace(dto.Telefone))
                return ValidationProblem("O campo 'Telefone' é obrigatório.");

            var telefone = NormalizePhone(dto.Telefone);

            var existe = await _db.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Nome == dto.Nome && c.Telefone == telefone, ct);

            if (existe)
                return Conflict("Já existe um cliente com o mesmo Nome e Telefone.");

            var c = new Cliente
            {
                Nome = dto.Nome.Trim(),
                Telefone = telefone,
                Endereco = dto.Endereco,
                Mensalista = dto.Mensalista,
                ValorMensalidade = dto.ValorMensalidade
            };

            _db.Clientes.Add(c);

            try
            {
                await _db.SaveChangesAsync(ct);
                return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível criar o cliente. Verifique os dados e tente novamente.");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var c = await _db.Clientes
                .AsNoTracking()
                .Include(x => x.Veiculos)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            return c is null
                ? NotFound("Cliente não encontrado.")
                : Ok(c);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ClienteUpdateDto dto, CancellationToken ct)
        {
            if (dto is null)
                return ValidationProblem("Corpo da requisição ausente.");

            if (string.IsNullOrWhiteSpace(dto.Nome))
                return ValidationProblem("O campo 'Nome' é obrigatório.");
            
            if (string.IsNullOrWhiteSpace(dto.Telefone))
                return ValidationProblem("O campo 'Telefone' é obrigatório.");

            var telefone = NormalizePhone(dto.Telefone);

            var duplicado = await _db.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Id != id && c.Nome == dto.Nome && c.Telefone == telefone, ct);

            if (duplicado)
                return Conflict("Já existe outro cliente com este Nome e Telefone.");

            var c = await _db.Clientes.FindAsync([id], ct);
            if (c is null)
                return NotFound("Cliente não encontrado.");

            c.Nome = dto.Nome.Trim();
            c.Telefone = telefone;
            c.Endereco = dto.Endereco;
            c.Mensalista = dto.Mensalista;
            c.ValorMensalidade = dto.ValorMensalidade;

            try
            {
                await _db.SaveChangesAsync(ct);
                return Ok(c);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Conflito de concorrência ao atualizar o cliente. Recarregue os dados e tente novamente.");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível salvar as alterações do cliente.");
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var c = await _db.Clientes.FindAsync([id], ct);
            if (c is null)
                return NotFound("Cliente não encontrado.");

            var temVeiculos = await _db.Veiculos.AsNoTracking().AnyAsync(v => v.ClienteId == id, ct);
            if (temVeiculos)
                return BadRequest("Cliente possui veículos associados. Transfira ou remova os veículos antes de excluir.");

            _db.Clientes.Remove(c);

            try
            {
                await _db.SaveChangesAsync(ct);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Conflito de concorrência ao excluir o cliente.");
            }
            catch (DbUpdateException)
            {
                return BadRequest("Não foi possível excluir o cliente.");
            }
        }

        private ActionResult ValidationProblem(string message)
            => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest, title: "Requisição inválida");
    }
}
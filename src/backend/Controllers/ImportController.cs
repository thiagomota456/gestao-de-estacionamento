using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;
using Parking.Api.Services;

namespace Parking.Api.Controllers
{
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PlacaService _placa;

        public ImportController(AppDbContext db, PlacaService placa)
        {
            _db = db;
            _placa = placa;
        }

        private record ErroImportacao(int Linha, string Campo, string Motivo, string LinhaBruta);

        [HttpPost("csv")]
        public async Task<IActionResult> ImportCsv(CancellationToken ct)
        {
            if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
                return ValidationProblem("Envie um arquivo CSV no campo 'file'.");

            var file = Request.Form.Files[0];
            if (file.Length == 0)
                return ValidationProblem("O arquivo está vazio.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return ValidationProblem("O arquivo deve ter extensão .csv.");

            using var s = file.OpenReadStream();
            using var r = new StreamReader(s, Encoding.UTF8);

            int linha = 0, processados = 0, inseridos = 0;
            var erros = new List<ErroImportacao>();

            string? header = await r.ReadLineAsync();
            const int COLS_ESPERADAS = 9;

            while (!r.EndOfStream)
            {
                linha++;
                var raw = await r.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(raw)) continue;
                processados++;

                var cols = raw.Split(',');

                if (cols.Length < COLS_ESPERADAS)
                {
                    erros.Add(new ErroImportacao(
                        Linha: linha,
                        Campo: "arquivo",
                        Motivo: $"Colunas insuficientes (recebido {cols.Length}, esperado {COLS_ESPERADAS}).",
                        LinhaBruta: raw));
                    continue;
                }

                var placaSan = _placa.Sanitizar(cols[0]);
                if (string.IsNullOrWhiteSpace(placaSan))
                {
                    erros.Add(new ErroImportacao(linha, "placa", "Placa ausente.", raw));
                    continue;
                }
                if (!_placa.EhValida(placaSan))
                {
                    erros.Add(new ErroImportacao(linha, "placa", "Placa inválida.", raw));
                    continue;
                }

                var modelo = string.IsNullOrWhiteSpace(cols[1]) ? null : cols[1].Trim();
                int? ano = null;
                if (!string.IsNullOrWhiteSpace(cols[2]))
                {
                    if (int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var anoParsed))
                    {
                        if (anoParsed < 1900 || anoParsed > DateTime.UtcNow.Year + 1)
                        {
                            erros.Add(new ErroImportacao(linha, "ano", $"Ano fora do intervalo esperado: {anoParsed}.", raw));
                            continue;
                        }
                        ano = anoParsed;
                    }
                    else
                    {
                        erros.Add(new ErroImportacao(linha, "ano", $"Valor inválido para ano: '{cols[2]}'.", raw));
                        continue;
                    }
                }

                var cliIdIgnorado = cols[3];

                var cliNome = cols[4]?.Trim();
                if (string.IsNullOrWhiteSpace(cliNome))
                {
                    erros.Add(new ErroImportacao(linha, "cliente_nome", "Nome do cliente ausente.", raw));
                    continue;
                }

                var cliTel = new string((cols[5] ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrWhiteSpace(cliTel))
                {
                    erros.Add(new ErroImportacao(linha, "cliente_telefone", "Telefone do cliente ausente ou inválido.", raw));
                    continue;
                }
                var cliEnd = cols[6];

                bool mensalista = false;
                if (!string.IsNullOrWhiteSpace(cols[7]))
                {
                    if (!bool.TryParse(cols[7], out mensalista))
                    {
                        erros.Add(new ErroImportacao(linha, "mensalista", $"Valor inválido para mensalista: '{cols[7]}'. Use true/false.", raw));
                        continue;
                    }
                }

                decimal? valorMens = null;
                if (!string.IsNullOrWhiteSpace(cols[8]))
                {
                    if (decimal.TryParse(cols[8], NumberStyles.Number, CultureInfo.InvariantCulture, out var vm))
                        valorMens = vm;
                    else
                    {
                        erros.Add(new ErroImportacao(linha, "valor_mensalidade", $"Valor inválido (use ponto como separador decimal): '{cols[8]}'.", raw));
                        continue;
                    }
                }

                try
                {
                    await using var tx = await _db.Database.BeginTransactionAsync(ct);

                    var placaExiste = await _db.Veiculos
                        .AsNoTracking()
                        .AnyAsync(v => v.Placa == placaSan, ct);

                    if (placaExiste)
                        throw new InvalidOperationException("Placa duplicada.");

                    var cliente = await _db.Clientes
                        .FirstOrDefaultAsync(c => c.Nome == cliNome && c.Telefone == cliTel, ct);

                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            Nome = cliNome,
                            Telefone = cliTel,
                            Endereco = cliEnd,
                            Mensalista = mensalista,
                            ValorMensalidade = valorMens
                        };

                        _db.Clientes.Add(cliente);
                        await _db.SaveChangesAsync(ct);
                        
                    }

                    var v = new Veiculo
                    {
                        Placa = placaSan,
                        Modelo = modelo,
                        Ano = ano,
                        ClienteId = cliente.Id
                    };

                    _db.Veiculos.Add(v);
                    await _db.SaveChangesAsync(ct);
                    
                    var historico = new VeiculoHistorico
                    {
                        VeiculoId = v.Id,
                        ClienteId = v.ClienteId,
                        Inicio = DateTime.UtcNow,
                        Fim = null
                    };
                    _db.Set<VeiculoHistorico>().Add(historico);
                    await _db.SaveChangesAsync(ct);

                    await tx.CommitAsync(ct);
                    inseridos++;
                }
                catch (DbUpdateException ex)
                {
                    erros.Add(new ErroImportacao(linha, "persistencia", $"Erro de banco de dados: {ex.GetBaseException().Message}", raw));
                }
                catch (InvalidOperationException ex)
                {
                    erros.Add(new ErroImportacao(linha, "regra_negocio", ex.Message, raw));
                }
                catch (Exception ex)
                {
                    erros.Add(new ErroImportacao(linha, "desconhecido", ex.Message, raw));
                }
            }

            return Ok(new
            {
                processados,
                inseridos,
                erros
            });
        }

        private ActionResult ValidationProblem(string message) =>
            Problem(detail: message, title: "Requisição inválida", statusCode: StatusCodes.Status400BadRequest);
    }
}

using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Models;

namespace Parking.Api.Services
{
    public class FaturamentoService
    {
        private readonly AppDbContext _db;
        public FaturamentoService(AppDbContext db) => _db = db;
        
        public async Task<List<Fatura>> GerarAsync(string competencia, CancellationToken ct = default)
        {
            var parts = competencia.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var ano) || !int.TryParse(parts[1], out var mes))
                throw new ArgumentException("Competência inválida. Use o formato yyyy-MM.", nameof(competencia));

            var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fimMes = new DateTime(ano, mes, DateTime.DaysInMonth(ano, mes), 23, 59, 59, DateTimeKind.Utc);
            var diasNoMes = (int)(fimMes.Date - inicioMes.Date).TotalDays + 1; // dias inteiros do mês

            var mensalistas = await _db.Clientes
                .AsNoTracking()
                .Where(c => c.Mensalista)
                .ToListAsync(ct);

            var criadas = new List<Fatura>();

            foreach (var cli in mensalistas)
            {
                var existente = await _db.Faturas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.ClienteId == cli.Id && f.Competencia == competencia, ct);

                if (existente != null) continue;

                var hist = await _db.Set<VeiculoHistorico>()
                    .AsNoTracking()
                    .Where(h => h.ClienteId == cli.Id &&
                                h.Inicio <= fimMes &&
                                (h.Fim == null || h.Fim >= inicioMes))
                    .Select(h => new
                    {
                        h.VeiculoId,
                        Inicio = h.Inicio,
                        Fim = h.Fim
                    })
                    .ToListAsync(ct);

                if (hist.Count == 0)
                {
                    continue;
                }

                var porVeiculo = hist
                    .Select(h => new
                    {
                        h.VeiculoId,
                        Inicio = h.Inicio < inicioMes ? inicioMes : h.Inicio,
                        Fim = (h.Fim == null || h.Fim > fimMes) ? fimMes : h.Fim.Value
                    })
                    .Where(h => h.Inicio <= h.Fim)
                    .GroupBy(h => h.VeiculoId)
                    .ToList();

                var todosIntervalos = porVeiculo.SelectMany(g => g).ToList();
                var intervalosMesclados = MergeIntervalos(todosIntervalos.Select(x => (x.Inicio, x.Fim)));

                var diasAtivos = 0;
                foreach (var (ini, fim) in intervalosMesclados)
                {
                    var di = (int)(fim.Date - ini.Date).TotalDays + 1;
                    if (di > 0) diasAtivos += di;
                }

                if (diasAtivos <= 0)
                {
                    continue;
                }

                var mensalidade = cli.ValorMensalidade ?? 0m;
                var valor = Math.Round(mensalidade * diasAtivos / diasNoMes, 2, MidpointRounding.AwayFromZero);

                var fat = new Fatura
                {
                    Competencia = competencia,
                    ClienteId = cli.Id,
                    Valor = valor,
                    CriadaEm = DateTime.UtcNow,
                    Observacao = $"Proporcional: {diasAtivos}/{diasNoMes} dias com ≥1 veículo."
                };

                var veiculosDoMes = porVeiculo.Select(g => g.Key).Distinct().ToList();
                foreach (var vId in veiculosDoMes)
                    fat.Veiculos.Add(new FaturaVeiculo { FaturaId = fat.Id, VeiculoId = vId });

                _db.Faturas.Add(fat);
                criadas.Add(fat);
            }

            await _db.SaveChangesAsync(ct);
            return criadas;
        }

        private static List<(DateTime Inicio, DateTime Fim)> MergeIntervalos(IEnumerable<(DateTime Inicio, DateTime Fim)> intervalos)
        {
            var list = intervalos.OrderBy(x => x.Inicio).ThenBy(x => x.Fim).ToList();
            var result = new List<(DateTime, DateTime)>();
            foreach (var cur in list)
            {
                if (result.Count == 0)
                {
                    result.Add(cur);
                    continue;
                }

                var (ri, rf) = result[^1];
                if (cur.Inicio <= rf)
                {
                    var novoFim = cur.Fim > rf ? cur.Fim : rf;
                    result[^1] = (ri, novoFim);
                }
                else
                {
                    result.Add(cur);
                }
            }
            return result;
        }
    }
}

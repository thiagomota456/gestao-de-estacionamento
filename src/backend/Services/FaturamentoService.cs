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
                throw new ArgumentException("Competência inválida. Use yyyy-MM.", nameof(competencia));

            var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fimMes = new DateTime(ano, mes, DateTime.DaysInMonth(ano, mes), 23, 59, 59, DateTimeKind.Utc);
            var diasNoMes = (int)(fimMes.Date - inicioMes.Date).TotalDays + 1;

            var mensalistas = await _db.Clientes.AsNoTracking()
                .Where(c => c.Mensalista)
                .ToListAsync(ct);

            var criadas = new List<Fatura>();

            foreach (var cli in mensalistas)
            {
                var existente = await _db.Faturas.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.ClienteId == cli.Id && f.Competencia == competencia, ct);
                if (existente != null) continue;

                var hist = await _db.Set<VeiculoHistorico>().AsNoTracking()
                    .Where(h => h.ClienteId == cli.Id &&
                                h.Inicio <= fimMes &&
                                (h.Fim == null || h.Fim >= inicioMes))
                    .Select(h => new { h.VeiculoId, h.Inicio, h.Fim })
                    .ToListAsync(ct);

                if (hist.Count == 0) continue;

                var intervalos = hist.Select(h => new
                {
                    h.VeiculoId,
                    Inicio = h.Inicio < inicioMes ? inicioMes : h.Inicio,
                    Fim = (h.Fim == null || h.Fim > fimMes) ? fimMes : h.Fim.Value
                })
                .Where(h => h.Inicio <= h.Fim)
                .ToList();

                var mesclados = MergeIntervalos(intervalos.Select(i => (i.Inicio, i.Fim)));
                var diasAtivos = mesclados.Sum(i => (int)(i.Item2.Date - i.Item1.Date).TotalDays + 1);
                if (diasAtivos <= 0) continue;

                var mensalidade = cli.ValorMensalidade ?? 0m;
                var valor = Math.Round(mensalidade * diasAtivos / diasNoMes, 2, MidpointRounding.AwayFromZero);

                var f = new Fatura
                {
                    Competencia = competencia,
                    ClienteId = cli.Id,
                    Valor = valor,
                    CriadaEm = DateTime.UtcNow,
                    Observacao = $"Proporcional: {diasAtivos}/{diasNoMes} dias com ≥1 veículo."
                };

                // Vincula veículos do mês para exibir placas na UI
                foreach (var vId in intervalos.Select(x => x.VeiculoId).Distinct())
                    f.Veiculos.Add(new FaturaVeiculo { FaturaId = f.Id, VeiculoId = vId });

                _db.Faturas.Add(f);
                criadas.Add(f);
            }

            await _db.SaveChangesAsync(ct);
            return criadas;
        }

        private static List<(DateTime Inicio, DateTime Fim)> MergeIntervalos(IEnumerable<(DateTime Inicio, DateTime Fim)> xs)
        {
            var list = xs.OrderBy(x => x.Inicio).ThenBy(x => x.Fim).ToList();
            var res = new List<(DateTime, DateTime)>();
            foreach (var cur in list)
            {
                if (res.Count == 0) { res.Add(cur); continue; }
                var (ri, rf) = res[^1];
                if (cur.Inicio <= rf) res[^1] = (ri, cur.Fim > rf ? cur.Fim : rf);
                else res.Add(cur);
            }
            return res;
        }
        
    }
}

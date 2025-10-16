namespace Parking.Api.Models;

public class VeiculoHistorico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VeiculoId { get; set; }
    public Guid ClienteId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }

    public Veiculo Veiculo { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace AutoFix.Api.Models;

public abstract class EntidadConcurrente
{
    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();
}

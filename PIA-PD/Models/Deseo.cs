using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIA_PD.Models
{
    public class Deseo
    {
        [Key]
        public int Id { get; set; }

        // Relacionamos este deseo con el usuario que inició sesión
        public string Usuario { get; set; } = "";

        public string LibroId { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string? PortadaUrl { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Precio { get; set; }

        public DateTime FechaAgregado { get; set; } = DateTime.Now;
    }
}
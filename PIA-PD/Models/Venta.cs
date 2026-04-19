using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIA_PD.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        public string Usuario { get; set; } = "Cliente Anónimo";

        // El correo es opcional (puede ser nulo)
        public string? Email { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Total { get; set; }

        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    }

    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }

        public int VentaId { get; set; }
        public Venta Venta { get; set; } = null!;

        public string LibroId { get; set; } = "";

        public string Titulo { get; set; } = "";

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioUnitario { get; set; }

        public int Cantidad { get; set; }
    }
}
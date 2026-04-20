using System.ComponentModel.DataAnnotations;

namespace PIA_PD.Models
{
    public class Cupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = "";

        // Descuento en porcentaje (ej. 15 = 15% de descuento)
        [Range(1, 100)]
        public int PorcentajeDescuento { get; set; }

        public bool Activo { get; set; } = true;
    }
}
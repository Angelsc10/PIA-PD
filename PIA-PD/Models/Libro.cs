using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIA_PD.Models
{
    public class Libro
    {
        [Key]
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "El título es obligatorio")]
        public string Titulo { get; set; } = "";

        [Required(ErrorMessage = "El autor es obligatorio")]
        public string Autor { get; set; } = "";

        public string? PortadaUrl { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad inicial debe ser al menos 1")]
        public int Stock { get; set; }

        public string Categoria { get; set; } = "Ficción";
    }
}
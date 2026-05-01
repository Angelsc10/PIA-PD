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

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad inicial debe ser al menos 1")]
        public int Stock { get; set; }

        public string Categoria { get; set; } = "Ficción";

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Column(TypeName = "decimal(18,2)")]
        [DisplayFormat(DataFormatString = "{0:0.00}", ApplyFormatInEditMode = true)]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "El precio solo puede tener hasta 2 decimales.")]
        public decimal Precio { get; set; }
    }
}
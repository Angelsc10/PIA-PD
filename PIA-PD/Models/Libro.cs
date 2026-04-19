using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PIA_PD.Models
{
    public class Libro
    {
        [Key]
        public string Id { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string Autor { get; set; } = "";

        public string? PortadaUrl { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Precio { get; set; }

        // NUEVO: Cantidad de libros disponibles en bodega
        public int Stock { get; set; } = 0;
    }
}
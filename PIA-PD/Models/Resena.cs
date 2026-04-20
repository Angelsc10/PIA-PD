using System.ComponentModel.DataAnnotations;

namespace PIA_PD.Models
{
    public class Resena
    {
        [Key]
        public int Id { get; set; }

        // Puede ser un ID de MySQL o un "OL-..." de la API
        public string LibroId { get; set; } = "";

        public string Usuario { get; set; } = "";

        [Range(1, 5)]
        public int Calificacion { get; set; }

        public string Comentario { get; set; } = "";

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
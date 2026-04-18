namespace PIA_PD.Models
{
    public class Libro
    {
        public string Id { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Autor { get; set; } = "";
        public string PortadaUrl { get; set; } = "";
        public decimal Precio { get; set; }
    }
}
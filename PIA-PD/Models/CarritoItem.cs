namespace PIA_PD.Models
{
    public class CarritoItem
    {
        public string LibroId { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string PortadaUrl { get; set; } = "";
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
    }
}
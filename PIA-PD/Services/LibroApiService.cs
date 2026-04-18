using System.Text.Json;
using PIA_PD.Models;

namespace PIA_PD.Services
{
    public class LibroApiService
    {
        private readonly HttpClient _httpClient;

        public LibroApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Un User-Agent amigable para identificarnos como proyecto universitario
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProyectoUniversidad_PIA/1.0 (contacto@test.com)");
        }

        public async Task<List<Libro>> ObtenerLibrosDestacadosAsync()
        {
            var libros = new List<Libro>();
            try
            {
                // Usamos OpenLibrary, buscando libros de ciencia ficción (no requiere llaves)
                var response = await _httpClient.GetAsync("https://openlibrary.org/subjects/science_fiction.json?limit=12");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonString);

                    if (document.RootElement.TryGetProperty("works", out var works))
                    {
                        foreach (var item in works.EnumerateArray())
                        {
                            var titulo = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "Sin Título";

                            var autor = "Autor Desconocido";
                            if (item.TryGetProperty("authors", out var authorsProp) && authorsProp.GetArrayLength() > 0)
                            {
                                autor = authorsProp[0].TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Autor Desconocido";
                            }

                            // OpenLibrary maneja las portadas por un ID numérico
                            var portadaUrl = "https://via.placeholder.com/300x400?text=Sin+Portada";
                            if (item.TryGetProperty("cover_id", out var coverIdProp) && coverIdProp.ValueKind != JsonValueKind.Null)
                            {
                                portadaUrl = $"https://covers.openlibrary.org/b/id/{coverIdProp.GetInt32()}-L.jpg";
                            }

                            // Simulamos el precio como lo hacíamos antes
                            var random = new Random();
                            var precio = (decimal)(random.Next(150, 500) + 0.99);

                            libros.Add(new Libro
                            {
                                Id = item.TryGetProperty("key", out var keyProp) ? keyProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                                Titulo = titulo ?? "Sin Título",
                                Autor = autor ?? "Autor Desconocido",
                                PortadaUrl = portadaUrl,
                                Precio = precio
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error interno al consultar la API: {ex.Message}");
            }

            // Sistema de respaldo por si se va el internet
            if (libros.Count == 0)
            {
                libros.Add(new Libro
                {
                    Id = "error-101",
                    Titulo = "Error de Conexión a la API",
                    Autor = "Soporte Técnico",
                    Precio = 0,
                    PortadaUrl = "https://via.placeholder.com/300x400?text=Error+API"
                });
            }

            return libros;
        }
    }
}
using System.Net.Http;
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
        }

        public async Task<List<Libro>> ObtenerLibrosDestacadosAsync()
        {
            return await ExtraerLibrosDeSujeto("fiction");
        }

        // NUEVO: Buscador por Categorías Exactas
        public async Task<List<Libro>> ObtenerLibrosPorCategoriaAsync(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return new List<Libro>();

            // Traducimos el español del menú a los temas oficiales de la API
            string subject = categoria.ToLower() switch
            {
                "romance" => "romance",
                "misterio" => "mystery_and_detective_stories",
                "fantasía" => "fantasy",
                "ciencia ficción" => "science_fiction",
                "tecnología" => "programming",
                _ => "fiction"
            };

            return await ExtraerLibrosDeSujeto(subject);
        }

        // Función auxiliar para no repetir código al leer la API por temas
        private async Task<List<Libro>> ExtraerLibrosDeSujeto(string subject)
        {
            var libros = new List<Libro>();
            try
            {
                var response = await _httpClient.GetAsync($"https://openlibrary.org/subjects/{subject}.json?limit=12");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var works = doc.RootElement.GetProperty("works");

                    foreach (var work in works.EnumerateArray())
                    {
                        libros.Add(new Libro
                        {
                            Id = "OL-" + work.GetProperty("key").GetString()?.Split('/').Last(),
                            Titulo = work.GetProperty("title").GetString() ?? "Sin título",
                            Autor = work.TryGetProperty("authors", out var authors) && authors.GetArrayLength() > 0
                                    ? authors[0].GetProperty("name").GetString() ?? "Anónimo" : "Anónimo",
                            PortadaUrl = work.TryGetProperty("cover_id", out var id)
                                ? $"https://covers.openlibrary.org/b/id/{id}-M.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                            Precio = 250.00m
                        });
                    }
                }
            }
            catch { }
            return libros;
        }

        public async Task<List<Libro>> BuscarLibrosAsync(string query)
        {
            var libros = new List<Libro>();
            if (string.IsNullOrWhiteSpace(query)) return libros;
            try
            {
                var response = await _httpClient.GetAsync($"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}&limit=12");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var docs = doc.RootElement.GetProperty("docs");

                    foreach (var d in docs.EnumerateArray())
                    {
                        libros.Add(new Libro
                        {
                            Id = "OL-" + d.GetProperty("key").GetString()?.Split('/').Last(),
                            Titulo = d.GetProperty("title").GetString() ?? "Sin título",
                            Autor = d.TryGetProperty("author_name", out var authors) ? authors[0].GetString() ?? "Anónimo" : "Anónimo",
                            PortadaUrl = d.TryGetProperty("cover_i", out var coverId)
                                ? $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                            Precio = 299.90m
                        });
                    }
                }
            }
            catch { }
            return libros;
        }

        public async Task<Libro?> ObtenerLibroPorIdAsync(string id)
        {
            try
            {
                string key = id.Replace("OL-", "");
                var response = await _httpClient.GetAsync($"https://openlibrary.org/works/{key}.json");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var root = doc.RootElement;

                    return new Libro
                    {
                        Id = id,
                        Titulo = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Sin título" : "Sin título",
                        Autor = "Autor de OpenLibrary",
                        PortadaUrl = root.TryGetProperty("covers", out var covers) && covers.GetArrayLength() > 0
                            ? $"https://covers.openlibrary.org/b/id/{covers[0].GetInt32()}-L.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                        Precio = 250.00m,
                        Stock = 100
                    };
                }
            }
            catch { }
            return null;
        }
    }
}
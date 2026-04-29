using System.Net.Http;
using System.Text.Json;
using PIA_PD.Models;

namespace PIA_PD.Services
{
    public class LibroApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly Random _random = new Random();

        public LibroApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Método único para calcular precio (reutilizable)
        private decimal CalcularPrecio(int seed, int? year = null, int? editions = null)
        {
            var rand = new Random(seed);
            decimal precio = 250; // Base

            // Ajuste por año (si existe)
            if (year.HasValue)
            {
                if (year < 1900) precio -= 40;
                else if (year < 2000) precio -= 20;
                else if (year > 2020) precio += 30;
                else if (year > 2010) precio += 15;
            }

            // Ajuste por popularidad (si existe)
            if (editions.HasValue)
                precio += Math.Min(editions.Value, 30);

            // Ajuste por longitud del título y aleatorio
            precio += (seed % 60) - 25; // -25 a +34
            precio += rand.Next(-10, 11);

            return Math.Clamp(precio, 165, 350);
        }

        private int ObtenerSeed(string titulo, string id) => (titulo + id).GetHashCode();

        public async Task<List<Libro>> ObtenerLibrosDestacadosAsync()
            => await ExtraerLibrosDeSujeto("fiction");

        public async Task<List<Libro>> ObtenerLibrosPorCategoriaAsync(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria)) return new List<Libro>();

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

        private async Task<List<Libro>> ExtraerLibrosDeSujeto(string subject)
        {
            var libros = new List<Libro>();
            try
            {
                var response = await _httpClient.GetAsync($"https://openlibrary.org/subjects/{subject}.json?limit=40");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var works = doc.RootElement.GetProperty("works");

                    foreach (var work in works.EnumerateArray())
                    {
                        var titulo = work.GetProperty("title").GetString() ?? "Sin título";
                        var id = "OL-" + work.GetProperty("key").GetString()?.Split('/').Last();
                        var year = work.TryGetProperty("first_publish_year", out var y) ? y.GetInt32() : (int?)null;
                        var editions = work.TryGetProperty("edition_count", out var e) ? e.GetInt32() : (int?)null;
                        var seed = ObtenerSeed(titulo, id);

                        libros.Add(new Libro
                        {
                            Id = id,
                            Titulo = titulo,
                            Autor = work.TryGetProperty("authors", out var a) && a.GetArrayLength() > 0
                                    ? a[0].GetProperty("name").GetString() ?? "Anónimo" : "Anónimo",
                            PortadaUrl = work.TryGetProperty("cover_id", out var c)
                                ? $"https://covers.openlibrary.org/b/id/{c}-M.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                            Precio = CalcularPrecio(seed, year, editions),
                            Stock = _random.Next(1, 15)
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
                var response = await _httpClient.GetAsync($"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}&limit=30");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var docs = doc.RootElement.GetProperty("docs");

                    foreach (var d in docs.EnumerateArray())
                    {
                        var titulo = d.GetProperty("title").GetString() ?? "Sin título";
                        var id = "OL-" + d.GetProperty("key").GetString()?.Split('/').Last();
                        var year = d.TryGetProperty("first_publish_year", out var y) ? y.GetInt32() : (int?)null;
                        var editions = d.TryGetProperty("edition_count", out var e) ? e.GetInt32() : (int?)null;
                        var seed = ObtenerSeed(titulo, id);

                        libros.Add(new Libro
                        {
                            Id = id,
                            Titulo = titulo,
                            Autor = d.TryGetProperty("author_name", out var a) ? a[0].GetString() ?? "Anónimo" : "Anónimo",
                            PortadaUrl = d.TryGetProperty("cover_i", out var c)
                                ? $"https://covers.openlibrary.org/b/id/{c}-M.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                            Precio = CalcularPrecio(seed, year, editions),
                            Stock = _random.Next(1, 10)
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
                    var titulo = root.TryGetProperty("title", out var t) ? t.GetString() ?? "Sin título" : "Sin título";
                    var seed = ObtenerSeed(titulo, id);

                    return new Libro
                    {
                        Id = id,
                        Titulo = titulo,
                        Autor = "Autor de OpenLibrary",
                        PortadaUrl = root.TryGetProperty("covers", out var c) && c.GetArrayLength() > 0
                            ? $"https://covers.openlibrary.org/b/id/{c[0].GetInt32()}-L.jpg" : "https://via.placeholder.com/300x400?text=Sin+Portada",
                        Precio = CalcularPrecio(seed),
                        Stock = 100
                    };
                }
            }
            catch { }
            return null;
        }
    }
}
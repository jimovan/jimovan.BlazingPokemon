using BlazingPokemon.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazingPokemon.Services
{
    public interface IPokeApiClient
    {
        Task<Pokemon> GetPokemonById(int id);
        Task<List<Pokemon>> GetPokemonByRegionId(int regionId, int offset = 0);
        Task<List<Evolution>> GetPokemonEvolutions(string url);
        List<Region> GetRegions();
        string GetRegionName(int regionId);
    }

    public class PokeApiClient(HttpClient httpClient) : IPokeApiClient
    {
        private readonly HttpClient _client = httpClient;
        private readonly int _pageLimit = 24;

        public async Task<Pokemon> GetPokemonById(int id)
        {
            var response = await _client.GetAsync($"pokemon/{id}");
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Pokemon>(content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        private async Task<Pokemon> GetPokemonByUrl(string url)
        {
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Pokemon>(content,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        public async Task<List<Pokemon>> GetPokemonByRegionId(int regionId, int offset = 0)
        {
            var region = GetRegions().FirstOrDefault(x => x.Id == regionId);

            var pageLimit = _pageLimit;

            if (offset + _pageLimit > region.EndIndex)
            {
                pageLimit = (region.EndIndex - offset) + 1;
            }

            if (pageLimit > 0)
            {
                var response = await _client.GetFromJsonAsync<PokemonResponse>($"pokemon?limit={pageLimit}&offset={offset}",
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (response is not null)
                {
                    var pokemon = new List<Pokemon>();
                    var tasks = response.Results?.Select(x => GetPokemonByUrl(x.Url));

                    foreach (var t in await Task.WhenAll(tasks))
                    {
                        pokemon.Add(t);
                    }

                    return pokemon;
                }
            }

            return [];
        }



        public List<Region> GetRegions()
        {
            return
            [
                new() { Id = 1, Name = "Kanto", StartIndex = 0, EndIndex = 150 }, // 1-151
                new() { Id = 2, Name = "Johto", StartIndex = 151, EndIndex = 250 }, // 152-251
                new() { Id = 3, Name = "Hoenn", StartIndex = 251, EndIndex = 385 }, // 252-386
                new() { Id = 4, Name = "Sinnoh", StartIndex = 386, EndIndex = 492 }, // 387-493
                new() { Id = 5, Name = "Unova", StartIndex = 493, EndIndex = 648 }, // 494-649
                new() { Id = 6, Name = "Kalos", StartIndex = 649, EndIndex = 720 }, // 650-721
                new() { Id = 7, Name = "Alola", StartIndex = 721, EndIndex = 808 }, // 722-809
                new() { Id = 8, Name = "Galar", StartIndex = 809, EndIndex = 904 }, // 810-905
                new() { Id = 9, Name = "Paldea", StartIndex = 905, EndIndex = 1024 }, // 906-1025
            ];
        }

        public string GetRegionName(int regionId)
        {
            return GetRegions()?.FirstOrDefault(x => x.Id == regionId)?.Name ?? "";
        }

        public async Task<List<Evolution>> GetPokemonEvolutions(string url)
        {
            var speciesRespone = await _client.GetFromJsonAsync<Species>(url,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var evolutionRespone = await _client.GetAsync(speciesRespone?.ChainUrl.Url);
            var evolutionContent = await evolutionRespone.Content.ReadAsStringAsync();

            var evolutions = new List<Evolution>();

            using (JsonDocument document = JsonDocument.Parse(evolutionContent))
            {
                var species = new List<JsonElement>();

                JsonElement root = document.RootElement;
                JsonElement chainElement = root.GetProperty("chain");

                GetEvolutions(chainElement, species);

                var tasks = species.Select(x => {

                    x.TryGetProperty("url", out JsonElement speciesUrl);

                    var urlFragments = speciesUrl.ToString().Split('/');
                    
                    return GetPokemonById(Convert.ToInt32(urlFragments[^2]));
                });

                foreach (var t in await Task.WhenAll(tasks))
                {
                    evolutions.Add(new Evolution { Id = t.Id, Name = t.Name, Sprite = t.Sprite });
                }
            }

            return evolutions;
        }

        private void GetEvolutions(JsonElement jsonElement, List<JsonElement> species)
        {
            JsonElement evolvesElement = jsonElement.GetProperty("evolves_to");
            var evolutionCount = evolvesElement.GetArrayLength();

            if (evolutionCount > 0)
            {
                foreach (JsonElement chain in evolvesElement.EnumerateArray())
                {
                    FindEvolution(jsonElement, species);

                    if (chain.TryGetProperty("evolves_to", out JsonElement nestedEvolutionElement))
                    {                        
                        GetEvolutions(chain, species);
                    }
                }
            }
            else
            {
                FindEvolution(jsonElement, species);
            }

            return;
        }

        private void FindEvolution(JsonElement jsonElement, List<JsonElement> species)
        {
            if (jsonElement.TryGetProperty("species", out JsonElement speciesElement))
            {
                if (!species.Contains(speciesElement))
                {
                    species.Add(speciesElement);
                }                
            }
        }
    }
}
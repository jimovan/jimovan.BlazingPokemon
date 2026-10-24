using BlazingPokemon;
using BlazingPokemon.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<IPokeApiClient, PokeApiClient>(c =>
{
    c.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
});


await builder.Build().RunAsync();

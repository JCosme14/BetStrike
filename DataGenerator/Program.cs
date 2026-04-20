using System.Text;
using System.Text.Json;

Console.WriteLine("Waiting for API to start...");
await Task.Delay(5000);

var client = new HttpClient();
var baseUrl = "https://localhost:7023/api/jogos";

var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
client = new HttpClient(handler);

var equipas = new List<string>
{
    "Benfica", "Porto", "Sporting", "Braga", "Guimarães",
    "Famalicão", "Arouca", "Estoril", "Gil Vicente",
    "Moreirense", "Boavista", "Vizela", "Casa Pia",
    "Chaves", "Rio Ave", "Portimonense", "Santa Clara", "Farense"
};

// Phase 1 - Generate and publish calendar
Console.WriteLine("=== Phase 1 - Publishing Calendar ===");

var random = new Random();
var equipasDisponiveis = equipas.Take(18).ToList();
var emparelhamentos = new List<(string casa, string fora)>();

var shuffled = equipasDisponiveis.OrderBy(x => random.Next()).ToList();
for (int i = 0; i < shuffled.Count; i += 2)
{
    emparelhamentos.Add((shuffled[i], shuffled[i + 1]));
}

int jornada = 1;
int ano = DateTime.Now.Year;
var jogosGerados = new List<string>();

for (int i = 0; i < emparelhamentos.Count; i++)
{
    var codigo = $"FUT-{ano}-{jornada:D2}{(i + 1):D2}";
    var jogo = new
    {
        codigo_Jogo = codigo,
        data_Hora_Inicio = DateTime.Now.AddMinutes(i * 5),
        equipa_Casa = emparelhamentos[i].casa,
        equipa_Fora = emparelhamentos[i].fora,
        golos_Casa = 0,
        golos_Fora = 0,
        estado = 1
    };

    var json = JsonSerializer.Serialize(jogo);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await client.PostAsync(baseUrl, content);

    if (response.IsSuccessStatusCode)
        Console.WriteLine($"Game published: {codigo} - {emparelhamentos[i].casa} vs {emparelhamentos[i].fora}");
    else
        Console.WriteLine($"Error publishing: {codigo} - {await response.Content.ReadAsStringAsync()}");

    jogosGerados.Add(codigo);
}

// Phase 2 - Simulate games
Console.WriteLine("\n=== Phase 2 - Simulating Games ===");

var tasks = jogosGerados.Select(async codigo =>
{
    var r = new Random();
    int golosCasa = 0, golosFora = 0;

    // Start game
    await UpdateJogo(client, baseUrl, codigo, 2, golosCasa, golosFora);
    Console.WriteLine($"{codigo} - Started");

    // Simulate 9 intervals of 10 seconds (90 minutes)
    for (int minuto = 0; minuto < 9; minuto++)
    {
        await Task.Delay(10000); //TEMPO REDUZIDO PARA TESTES

        // Random chance of goal each interval
        if (r.Next(0, 10) < 2) golosCasa++;
        if (r.Next(0, 10) < 2) golosFora++;

        await UpdateJogo(client, baseUrl, codigo, 2, golosCasa, golosFora);
        Console.WriteLine($"{codigo} - {golosCasa}:{golosFora}");
    }

    // Finish game
    await UpdateJogo(client, baseUrl, codigo, 3, golosCasa, golosFora);
    Console.WriteLine($"{codigo} - Finished! Final: {golosCasa}:{golosFora}");
});

await Task.WhenAll(tasks);
Console.WriteLine("\n=== All games finished! ===");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

async Task UpdateJogo(HttpClient httpClient, string url, string codigo, int estado, int golosCasa, int golosFora)
{
    var jogo = new
    {
        codigo_Jogo = codigo,
        estado,
        golos_Casa = golosCasa,
        golos_Fora = golosFora
    };

    var json = JsonSerializer.Serialize(jogo);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var response = await httpClient.PutAsync($"{url}/{codigo}", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ERROR updating {codigo}: {response.StatusCode} - {error}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"EXCEPTION updating {codigo}: {ex.Message}");
    }
}
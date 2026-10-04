using System.Text.Encodings.Web;
using System.Text.Json;
using BudgetFrance.Importer;

// Usage (depuis la racine du repo) : dotnet run --project BudgetFrance.Importer -- <année> [fichier de sortie]
if (args.Length == 0 || !int.TryParse(args[0], out var year))
{
    Console.Error.WriteLine("Usage : dotnet run --project BudgetFrance.Importer -- <année> [fichier de sortie]");
    return 1;
}
var outputPath = args.Length > 1 ? args[1] : Path.Combine("BudgetFrance.Web", "wwwroot", "data", "baseline.json");

using var http = new HttpClient { BaseAddress = new Uri(EurostatClient.BaseAddress) };
var baseline = await new BaselineBuilder(new EurostatClient(http)).BuildAsync(year);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // accents lisibles dans le fichier versionné
};
await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(baseline, jsonOptions));

Console.WriteLine($"Année {baseline.Year} : dépenses {baseline.Spending.Sum(s => s.Amount):N1} Md€, " +
                  $"recettes {baseline.Revenues.Sum(r => r.Amount):N1} Md€, PIB {baseline.Gdp:N1} Md€ → {outputPath}");
return 0;

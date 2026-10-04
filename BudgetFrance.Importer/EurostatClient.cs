using System.Text.Json;

namespace BudgetFrance.Importer;

/// <summary>Une valeur observée, identifiée par son code dans chaque dimension (geo, cofog99, na_item, time…).</summary>
internal sealed record Observation(IReadOnlyDictionary<string, string> Codes, decimal Value);

internal sealed record EurostatDataset(IReadOnlyList<Observation> Observations, IReadOnlyDictionary<string, string> GeoLabels)
{
    /// <summary>Valeur unique correspondant aux codes donnés (les dimensions non citées sont libres).</summary>
    public decimal? Find(params (string Dimension, string Code)[] codes) =>
        Observations.SingleOrDefault(o => codes.All(c => o.Codes[c.Dimension] == c.Code))?.Value;
}

/// <summary>Client minimal de l'API de diffusion Eurostat (format JSON-stat 2.0).</summary>
internal sealed class EurostatClient(HttpClient http)
{
    public const string BaseAddress = "https://ec.europa.eu/eurostat/api/dissemination/statistics/1.0/data/";

    public async Task<EurostatDataset> GetAsync(string dataset, params (string Dimension, string Code)[] filters)
    {
        var query = string.Join("&", filters.Select(f => $"{f.Dimension}={Uri.EscapeDataString(f.Code)}"));
        using var document = JsonDocument.Parse(await http.GetStringAsync($"{dataset}?format=JSON&lang=fr&{query}"));
        var root = document.RootElement;

        if (root.TryGetProperty("error", out var error))
            throw new InvalidOperationException($"Eurostat {dataset} : {error}");

        var dimensionIds = root.GetProperty("id").EnumerateArray().Select(e => e.GetString()!).ToArray();
        var sizes = root.GetProperty("size").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        var categories = dimensionIds.Select(id => root.GetProperty("dimension").GetProperty(id).GetProperty("category")).ToArray();
        var codesByPosition = categories
            .Select(c => c.GetProperty("index").EnumerateObject().ToDictionary(p => p.Value.GetInt32(), p => p.Name))
            .ToArray();

        var observations = new List<Observation>();
        foreach (var value in root.GetProperty("value").EnumerateObject())
        {
            if (value.Value.ValueKind != JsonValueKind.Number)
                continue;

            // Index aplati JSON-stat : la dernière dimension varie le plus vite.
            var flatIndex = int.Parse(value.Name);
            var codes = new Dictionary<string, string>();
            for (var d = dimensionIds.Length - 1; d >= 0; d--)
            {
                codes[dimensionIds[d]] = codesByPosition[d][flatIndex % sizes[d]];
                flatIndex /= sizes[d];
            }
            observations.Add(new Observation(codes, value.Value.GetDecimal()));
        }

        var geoLabels = Array.IndexOf(dimensionIds, "geo") is var geo and >= 0
            ? categories[geo].GetProperty("label").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!)
            : new Dictionary<string, string>();

        return new EurostatDataset(observations, geoLabels);
    }
}

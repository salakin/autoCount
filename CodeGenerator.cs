namespace AutoClicker;

/// <summary>Builds a random but realistic-looking C# method. Every line stays within <see cref="ParagraphGenerator.MaxLineLength"/>.</summary>
internal static class CodeGenerator
{
    private static readonly string[] Nouns =
    {
        "user", "order", "item", "customer", "invoice", "report", "file", "account", "product", "payment",
        "session", "message", "record", "config", "request", "result",
    };

    private static readonly string[] Verbs = { "Load", "Save", "Update", "Validate", "Process", "Build", "Fetch", "Send", "Parse", "Check" };

    private static readonly string[] Types = { "int", "string", "bool", "var", "double", "decimal" };

    private static readonly Random Rng = new();

    public static string Create(int statements)
    {
        string noun = Pick(Nouns);
        string method = $"{Pick(Verbs)}{Cap(noun)}";
        var lines = new List<string>
        {
            $"public {Pick(new[] { "void", "bool", "int", "string" })} {method}({Cap(noun)} {noun})",
            "{",
        };

        for (int i = 0; i < statements; i++)
        {
            List<string> block;
            do block = Statement(noun).ToList(); // retry rather than cut a long line mid-word
            while (block.Any(l => l.Length > ParagraphGenerator.MaxLineLength));
            lines.AddRange(block);
        }

        lines.Add("}");
        return string.Join('\n', lines);
    }

    private static IEnumerable<string> Statement(string noun)
    {
        string other = Pick(Nouns);
        string name = $"{other}{Pick(new[] { "Count", "Id", "Name", "Total", "Status", "List" })}";
        switch (Rng.Next(7))
        {
            case 0:
                yield return $"    {Pick(Types)} {Lower(name)} = {noun}.{Cap(other)}.Length;";
                break;
            case 1:
                yield return $"    if ({noun} == null)";
                yield return "    {";
                yield return $"        throw new ArgumentNullException(nameof({noun}));";
                yield return "    }";
                break;
            case 2:
                yield return $"    foreach (var {other} in {noun}.{Cap(other)}s)";
                yield return "    {";
                yield return $"        {Pick(Verbs)}{Cap(other)}({other});";
                yield return "    }";
                break;
            case 3:
                yield return $"    var {Lower(name)} = await _{other}Service.{Pick(Verbs)}Async({noun}.Id);";
                break;
            case 4:
                yield return $"    _logger.LogInformation(\"{Pick(Verbs)} {other} {{Id}}\", {noun}.Id);";
                break;
            case 5:
                yield return $"    for (int i = 0; i < {noun}.{Cap(other)}s.Count; i++)";
                yield return "    {";
                yield return $"        total += {noun}.{Cap(other)}s[i].Amount;";
                yield return "    }";
                break;
            default:
                yield return $"    {noun}.{Cap(name)} = {Rng.Next(1, 500)};";
                break;
        }
    }

    private static string Pick(string[] items) => items[Rng.Next(items.Length)];
    private static string Cap(string s) => char.ToUpper(s[0]) + s[1..];
    private static string Lower(string s) => char.ToLower(s[0]) + s[1..];
}

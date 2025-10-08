using System.Text;
using System.Text.Json;
using System.Xml.Linq;

//Objectif: Charger XML -> Explorer -> Détecter champs -> Tri si on le veut -> Preview -> Export JSON sur le bureau

Console.OutputEncoding = Encoding.UTF8;
new App().Run();

sealed class App
{
    public void Run()
    {
        Console.WriteLine("=== Conversion XML -> JSON (LINQ) ===\n");
        Console.WriteLine("Entrez le chemin COMPLET du fichier XML d'entrée:");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Chemin vide. Fin.");
            return;
        }

        if (!File.Exists(input))
        {
            Console.WriteLine("Fichier introuvable. Fin.");
            return;
        }

        //Explore
        Console.WriteLine("Chargement du XML...");
        var table = XmlTableLoader.Load(input);
        if (table.Rows.Count == 0)
        {
            Console.WriteLine("Aucune donnée trouvée sous la racine.");
            return;
        }
        Console.WriteLine($"OK. {table.Rows.Count} élément(s) lu(s).\n");

        //Détecte
        Console.WriteLine("Champs détectés:");
        Console.WriteLine(string.Join(", ", table.Columns));

        // Tri
        Console.WriteLine("\nSouhaitez-vous trier ? (o/n)");
        var doSort = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (doSort is "o")
        {
            Console.WriteLine("Champ pour le tri: ");
            var field = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(field) && table.Columns.Contains(field))
            {
                Console.WriteLine("Ordre (c=croissant, d=décroissant) : ");
                var ord = Console.ReadLine()?.Trim().ToLowerInvariant();
                var desc = ord is "d";
                table = table.OrderBy(field, desc: desc);
            }
            else
            {
                Console.WriteLine("Champ invalide, tri ignoré.");
            }
        }

        //Preview
        Console.WriteLine("\nColonnes à afficher ou 'all' :");
        var colIn = Console.ReadLine()?.Trim();
        var previewColumns = string.IsNullOrWhiteSpace(colIn) || colIn.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? table.Columns.ToList()
            : colIn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(c => table.Columns.Contains(c)).ToList();
        if (previewColumns.Count == 0) previewColumns = table.Columns.Take(6).ToList();

        var pager = new Pager(pageSize: 10, table.Rows.Count);
        while (true)
        {
            Console.WriteLine($"\nAperçu page {pager.PageIndex + 1}/{pager.TotalPages}");
            TablePrinter.Print(table.Rows.Skip(pager.PageIndex * pager.PageSize).Take(pager.PageSize).ToList(), previewColumns);
            if (!pager.HasMoreThanOnePage) break;
            Console.WriteLine("n=suivante, p=précédente, q=quitter l'aperçu");
            var k = Console.ReadKey(intercept: true).KeyChar;
            if (k is 'q' or 'Q') break;
            if (k is 'n' or 'N') pager.Next();
            if (k is 'p' or 'P') pager.Previous();
        }

        //Expor
        Console.WriteLine("\nExporter en JSON ? (o/n)");
        var doExport = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (doExport is "o")
        {
            Console.WriteLine("Champs à exporter ou 'all' :");
            var expIn = Console.ReadLine()?.Trim();
            var exportColumns = string.IsNullOrWhiteSpace(expIn) || expIn.Equals("all", StringComparison.OrdinalIgnoreCase)
                ? table.Columns.ToList()
                : expIn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(c => table.Columns.Contains(c)).ToList();
            if (exportColumns.Count == 0) exportColumns = table.Columns.ToList();

            // Export auto sur le bureau de l'utilisateur
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var fileName = Path.GetFileNameWithoutExtension(input);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "export";
            var outPath = Path.Combine(desktop, fileName + ".json");

            Exporter.ExportJson(table, exportColumns, outPath);
            Console.WriteLine($"Fichier JSON créé sur votre bureau: {outPath}");
        }

        Console.WriteLine("\nTerminé.");
    }
}

sealed class Pager(int pageSize, int totalCount)
{
    public int PageSize { get; } = pageSize;
    public int TotalCount { get; } = totalCount;
    public int PageIndex { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public bool HasMoreThanOnePage => TotalPages > 1;
    public void Next() => PageIndex = Math.Min(PageIndex + 1, TotalPages - 1);
    public void Previous() => PageIndex = Math.Max(PageIndex - 1, 0);
}

sealed class Table
{
    public List<Dictionary<string, string?>> Rows { get; }
    public HashSet<string> Columns { get; }

    public Table(List<Dictionary<string, string?>> rows)
    {
        Rows = rows;
        Columns = rows
            .SelectMany(r => r.Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Table OrderBy(string field, bool desc)
    {
        static (int rank, double num, string? text) SortKey(Dictionary<string, string?> r, string f)
        {
            if (!r.TryGetValue(f, out var v) || v is null) return (2, double.NaN, null);
            if (double.TryParse(v.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                return (0, d, null);
            return (1, double.NaN, v);
        }

        var sorted = desc
            ? Rows.OrderByDescending(r => SortKey(r, field))
            : Rows.OrderBy(r => SortKey(r, field));

        return new Table(sorted.ToList());
    }
}

static class XmlTableLoader
{
    public static Table Load(string xmlPath)
    {
        var doc = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var root = doc.Root ?? throw new InvalidDataException("Document XML sans racine");
        var items = root.Elements().ToList();
        var rows = new List<Dictionary<string, string?>>(items.Count);
        foreach (var it in items)
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var attr in it.Attributes())
                AddOrAppend(row, $"@{attr.Name.LocalName}", attr.Value);
            foreach (var child in it.Elements())
                Flatten(child, null, row);

            var text = it.Nodes().OfType<XText>().Select(t => t.Value).DefaultIfEmpty(string.Empty).Aggregate((a, b) => a + b).Trim();
            if (!string.IsNullOrEmpty(text)) AddOrAppend(row, it.Name.LocalName, text);
            rows.Add(row);
        }
        return new Table(rows);
    }

    static void Flatten(XElement el, string? prefix, Dictionary<string, string?> row)
    {
        var keyBase = string.IsNullOrEmpty(prefix) ? el.Name.LocalName : $"{prefix}.{el.Name.LocalName}";
        foreach (var attr in el.Attributes())
            AddOrAppend(row, $"{keyBase}.@{attr.Name.LocalName}", attr.Value);

        var children = el.Elements().ToList();
        if (children.Count == 0)
        {
            var val = el.Value?.Trim();
            if (!string.IsNullOrEmpty(val)) AddOrAppend(row, keyBase, val);
            return;
        }
        foreach (var child in children)
            Flatten(child, keyBase, row);
    }

    static void AddOrAppend(Dictionary<string, string?> row, string key, string? value)
    {
        if (value is null) return;
        if (row.TryGetValue(key, out var existing) && !string.IsNullOrEmpty(existing))
            row[key] = existing + " | " + value;
        else
            row[key] = value;
    }
}

static class TablePrinter
{
    public static void Print(List<Dictionary<string, string?>> rows, List<string> columns)
    {
        if (rows.Count == 0)
        {
            Console.WriteLine("(aucune ligne)");
            return;
        }

        var widths = columns.ToDictionary(c => c, c => Math.Min(30, Math.Max(c.Length, rows
            .Select(r => r.TryGetValue(c, out var v) ? v : string.Empty)
            .Select(v => (v ?? string.Empty).Length)
            .DefaultIfEmpty(0)
            .Max())));

        foreach (var c in columns) Console.Write($"| {c.PadRight(widths[c])} ");
        Console.WriteLine("|");
        foreach (var c in columns) Console.Write($"|-{new string('-', widths[c])}-");
        Console.WriteLine("|");

        foreach (var row in rows)
        {
            foreach (var c in columns)
            {
                row.TryGetValue(c, out var val);
                var s = (val ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ');
                if (s.Length > widths[c]) s = s[..widths[c]];
                Console.Write($"| {s.PadRight(widths[c])} ");
            }
            Console.WriteLine("|");
        }
    }
}

static class Exporter
{
    public static void ExportJson(Table table, List<string> columns, string path)
    {
        var shaped = table.Rows.Select(r =>
        {
            var o = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in columns) o[c] = r.TryGetValue(c, out var v) ? v : null;
            return o;
        }).ToList();

        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(shaped, opts);
        File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

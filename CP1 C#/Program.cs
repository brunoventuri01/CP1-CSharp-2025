using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.RegularExpressions;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("=== Processador Assíncrono de Arquivos de Texto ===");
Console.Write("Informe o caminho de um diretório OU arquivo .zip com .txt (deixe vazio para usar C:/Users/labsfiap/Desktop/notas): ");

var input = Console.ReadLine();
var defaultPath = @"C:\Users\labsfiap\Desktop\notas";
var inputPath = string.IsNullOrWhiteSpace(input) ? defaultPath : input!.Trim('"', ' ');

string workDir;

if (File.Exists(inputPath) && Path.GetExtension(inputPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
{
    workDir = Path.Combine(Path.GetTempPath(), "TxtProc_" + Guid.NewGuid());
    Directory.CreateDirectory(workDir);
    Console.WriteLine($"Extraindo {inputPath} para {workDir} ...");
    ZipFile.ExtractToDirectory(inputPath, workDir);
}
else if (Directory.Exists(inputPath))
{
    workDir = inputPath;
}
else
{
    Console.WriteLine("Caminho inválido. Informe um diretório existente ou um .zip válido.");
    return;
}

var files = Directory.EnumerateFiles(workDir, "*.txt", SearchOption.TopDirectoryOnly).ToArray();

if (files.Length == 0)
{
    Console.WriteLine("Nenhum arquivo .txt encontrado no caminho informado.");
    return;
}

Console.WriteLine($"\nLocalizados {files.Length} arquivo(s). Iniciando processamento...\n");

var degreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1);

var results = new ConcurrentBag<(string FileName, long Lines, long Words)>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    await Parallel.ForEachAsync(
        files,
        new ParallelOptions
        {
            MaxDegreeOfParallelism = degreeOfParallelism,
            CancellationToken = cts.Token
        },
        async (file, token) =>
        {
            var name = Path.GetFileName(file);
            Console.WriteLine($"Processando: {name} ...");
            try
            {
                var (lines, words) = await CountLinesAndWordsAsync(file, token);
                results.Add((name, lines, words));
                Console.WriteLine($"Concluído : {name} - {lines} linha(s), {words} palavra(s).");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Cancelado  : {name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro       : {name} -> {ex.Message}");
            }
        }
    );

    var exportDir = Path.Combine(AppContext.BaseDirectory, "export");
    Directory.CreateDirectory(exportDir);
    var reportPath = Path.Combine(exportDir, "relatorio.txt");

    var ordered = results.OrderBy(r => r.FileName, StringComparer.OrdinalIgnoreCase).ToArray();

    var reportLines = new List<string> { "Arquivo - Linhas - Palavras" };
    reportLines.AddRange(ordered.Select(r => $"{r.FileName} - {r.Lines} linhas - {r.Words} palavras"));

    await File.WriteAllLinesAsync(reportPath, reportLines);

    Console.WriteLine("\nProcessamento concluído com sucesso!");
    Console.WriteLine($"Relatório gerado em: {reportPath}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nOperação cancelada pelo usuário.");
}

static async Task<(long Lines, long Words)> CountLinesAndWordsAsync(string path, CancellationToken ct)
{
    long lineCount = 0;
    long wordCount = 0;

    var wordRegex = new Regex(@"\b[\p{L}\p{N}_]+\b", RegexOptions.Compiled);

    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true);
    using var sr = new StreamReader(fs, detectEncodingFromByteOrderMarks: true);

    while (!sr.EndOfStream)
    {
        ct.ThrowIfCancellationRequested();
        var line = await sr.ReadLineAsync() ?? string.Empty;
        lineCount++;
        wordCount += wordRegex.Matches(line).Count;
    }

    return (lineCount, wordCount);
}
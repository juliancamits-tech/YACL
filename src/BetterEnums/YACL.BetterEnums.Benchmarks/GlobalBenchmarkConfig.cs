using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

namespace BetterEnums.YACL.Benchmarks
{
    public class GlobalBenchmarkConfig : ManualConfig
    {
        public GlobalBenchmarkConfig()
        {
            // 1. Agregamos los loggers básicos para ver el progreso en la consola
            AddLogger(ConsoleLogger.Default);

            // 2. Agregamos las columnas por defecto (Metodo, Media, Error, Ratio, etc.)
            AddColumnProvider(DefaultColumnProviders.Instance);

            // 3. AGREGAMOS SOLO los exportadores que queremos (excluyendo HTML y CSV)
            AddExporter(MarkdownExporter.GitHub); // Conserva el archivo .md para GitHub

            // El diagnosers, evaluadores, etc., se pueden mantener por defecto
        }
    }
}

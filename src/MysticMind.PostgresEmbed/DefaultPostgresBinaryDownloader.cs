using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MysticMind.PostgresEmbed;

internal class DefaultPostgresBinaryDownloader
{
    private readonly string _pgVersion;
    private readonly Platform _platform;
    private readonly Architecture? _architecture;
    private readonly string _destDir;

    /// <summary>
    /// The default Postgres binary downloader uses https://github.com/zonkyio/embedded-postgres-binaries
    /// for downloading binary for different platform and architecture
    /// </summary>
    /// <param name="destDir"></param>
    /// <param name="pgVersion"></param>
    /// <param name="platform"></param>
    /// <param name="architecture"></param>
    public DefaultPostgresBinaryDownloader(string pgVersion, string destDir, Platform platform, Architecture? architecture)
    {
        _destDir = destDir;
        _pgVersion = pgVersion;
        _platform = platform;
        _architecture = architecture;
    }

    public string Download()
    {
        var platform = _platform.ToString().ToLowerInvariant();
        var architecture = _architecture?.ToString().ToLowerInvariant();
        
        if (platform == "alpine")
        {
            platform = "linux";
            architecture = "alpine";
        } else if (platform == "alpinelitelinux")
        {
            platform = "linux";
            architecture = "alpine-lite";
        }
        
        var downloadUrl = $"https://repo1.maven.org/maven2/io/zonky/test/postgres/embedded-postgres-binaries-{platform}-{architecture}/{_pgVersion}/embedded-postgres-binaries-{platform}-{architecture}-{_pgVersion}.jar";
        var fileName = Path.GetFileName(downloadUrl);
        var destFile = Path.Join(_destDir, fileName);
        var zipFile = Path.Join(_destDir, Path.GetFileNameWithoutExtension(fileName) + ".txz");
        
        // check if txz file exists in the destination folder
        // return the file path and don't require to download again
        if (File.Exists(zipFile))
        {
            return zipFile;
        }

        var cts = new CancellationTokenSource();
        var progress = new Progress<double>();
        progress.ProgressChanged += (_, value) => Console.WriteLine("\r %{0:N0}", value);
        var downloadPath = Path.Combine(_destDir, $"embedded-postgres-binaries-{platform}-{architecture}-{_pgVersion}.jar");
        Utils.DownloadAsync(downloadUrl, downloadPath, progress, cts.Token).Wait();
        return ExtractContent(downloadPath, zipFile);
    }
    
    private string ExtractContent(string zipFile, string outputFile)
    {
        // extract the PG binary zip file
        using var archive = ZipFile.OpenRead(zipFile);
        var result = from entry in archive.Entries
            where Path.GetExtension(entry.FullName) == ".txz"
            where !string.IsNullOrEmpty(entry.Name)
            select entry;

        var pgBinaryTxzFile = result.FirstOrDefault();

        if (pgBinaryTxzFile == null) return string.Empty;
        pgBinaryTxzFile.ExtractToFile(outputFile, true);

        return outputFile;
    }
}
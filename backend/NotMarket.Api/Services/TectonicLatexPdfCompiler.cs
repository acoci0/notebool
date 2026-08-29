using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;

namespace NotMarket.Api.Services;

public sealed class TectonicLatexPdfCompiler(
    IOptions<NotePdfGenerationOptions> options,
    ILogger<TectonicLatexPdfCompiler> logger)
    : ILatexPdfCompiler
{
    private const int MaximumCompilerOutputLength =
        12000;

    private readonly NotePdfGenerationOptions
        _options =
            options.Value;

    public async Task<LatexPdfCompilationResult>
        CompileAsync(
            LatexPdfCompilationInput input,
            CancellationToken cancellationToken)
    {
        ValidateInput(
            input);

        var temporaryRoot =
            Path.Combine(
                Path.GetTempPath(),
                "notmarket-latex");

        Directory.CreateDirectory(
            temporaryRoot);

        var workingDirectory =
            Path.Combine(
                temporaryRoot,
                input.NoteSubmissionId
                    .ToString("N"),
                Guid.NewGuid()
                    .ToString("N"));

        Directory.CreateDirectory(
            workingDirectory);

        var sourcePath =
            Path.Combine(
                workingDirectory,
                "note.tex");

        var outputPath =
            Path.Combine(
                workingDirectory,
                "note.pdf");

        var compilationSucceeded =
            false;

        try
        {
            await File.WriteAllTextAsync(
                sourcePath,
                input.LatexSource,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier:
                        false),
                cancellationToken);

            var processStartInfo =
                CreateProcessStartInfo(
                    sourcePath,
                    workingDirectory);

            using var process =
                new Process
                {
                    StartInfo =
                        processStartInfo,

                    EnableRaisingEvents =
                        true
                };

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException(
                        "Tectonic PDF derleyicisi başlatılamadı.");
                }
            }
            catch (Exception exception)
                when (
                    exception is not
                        OperationCanceledException
                )
            {
                throw new InvalidOperationException(
                    $"Tectonic PDF derleyicisi başlatılamadı. " +
                    $"CompilerPath: {_options.CompilerPath}",
                    exception);
            }

            /*
             * Process çıktı tamponlarının dolarak
             * işlemi kilitlememesi için iki akış
             * eş zamanlı olarak okunur.
             */
            var standardOutputTask =
                process.StandardOutput
                    .ReadToEndAsync();

            var standardErrorTask =
                process.StandardError
                    .ReadToEndAsync();

            using var timeoutSource =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken);

            timeoutSource.CancelAfter(
                TimeSpan.FromSeconds(
                    _options.TimeoutSeconds));

            try
            {
                await process.WaitForExitAsync(
                    timeoutSource.Token);
            }
            catch (OperationCanceledException)
                when (
                    !cancellationToken
                        .IsCancellationRequested
                )
            {
                await TerminateProcessAsync(
                    process);

                throw new TimeoutException(
                    $"LaTeX PDF derleme işlemi " +
                    $"{_options.TimeoutSeconds} saniyelik zaman sınırını aştı.");
            }
            catch (OperationCanceledException)
            {
                await TerminateProcessAsync(
                    process);

                throw;
            }

            var standardOutput =
                await standardOutputTask;

            var standardError =
                await standardErrorTask;

            var compilerOutput =
                BuildCompilerOutput(
                    standardOutput,
                    standardError);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Tectonic PDF derleme işlemi başarısız oldu. " +
                    $"ExitCode: {process.ExitCode}. " +
                    $"Derleyici çıktısı: {compilerOutput}");
            }

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException(
                    "Tectonic başarılı sonuç döndürdü ancak PDF dosyası oluşturulmadı.");
            }

            var outputFile =
                new FileInfo(
                    outputPath);

            if (outputFile.Length == 0)
            {
                throw new InvalidOperationException(
                    "Tectonic boş PDF dosyası oluşturdu.");
            }

            if (
                outputFile.Length >
                _options.MaxGeneratedPdfBytes
            )
            {
                throw new InvalidOperationException(
                    "Oluşturulan PDF izin verilen dosya boyutunu aşıyor.");
            }

            var pdfBytes =
                await File.ReadAllBytesAsync(
                    outputPath,
                    cancellationToken);

            ValidatePdfBytes(
                pdfBytes);

            compilationSucceeded =
                true;

            return new LatexPdfCompilationResult(
                pdfBytes,
                Path.GetFileName(
                    _options.CompilerPath),
                compilerOutput,
                DateTimeOffset.UtcNow);
        }
        finally
        {
            /*
             * Başarılı işlemlerin geçici dosyaları
             * her zaman silinir.
             *
             * Başarısız işlemlerde KeepFailedArtifacts
             * true ise dosyalar hata incelemesi için
             * bırakılır.
             */
            if (
                compilationSucceeded ||
                !_options.KeepFailedArtifacts
            )
            {
                TryDeleteWorkingDirectory(
                    temporaryRoot,
                    workingDirectory);
            }
            else
            {
                logger.LogWarning(
                    "Başarısız LaTeX derleme dosyaları saklandı. Directory: {WorkingDirectory}",
                    workingDirectory);
            }
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(
        string sourcePath,
        string workingDirectory)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName =
                    _options.CompilerPath,

                WorkingDirectory =
                    workingDirectory,

                UseShellExecute =
                    false,

                RedirectStandardOutput =
                    true,

                RedirectStandardError =
                    true,

                CreateNoWindow =
                    true
            };

        /*
         * --untrusted:
         * Güvenilmeyen LaTeX girdisi için
         * Tectonic'in güvenli modunu etkinleştirir.
         */
        startInfo.ArgumentList.Add(
            "--untrusted");

        /*
         * Derleme hatası oluşursa log dosyasının
         * geçici klasörde bulunmasını sağlar.
         */
        startInfo.ArgumentList.Add(
            "--keep-logs");

        startInfo.ArgumentList.Add(
            "--outdir");

        startInfo.ArgumentList.Add(
            workingDirectory);

        startInfo.ArgumentList.Add(
            sourcePath);

        return startInfo;
    }

    private void ValidateInput(
        LatexPdfCompilationInput input)
    {
        ArgumentNullException.ThrowIfNull(
            input);

        if (
            input.NoteSubmissionId ==
            Guid.Empty
        )
        {
            throw new ArgumentException(
                "PDF derleme işlemi için not gönderim ID'si geçersiz.",
                nameof(input));
        }

        if (
            string.IsNullOrWhiteSpace(
                input.LatexSource)
        )
        {
            throw new ArgumentException(
                "PDF derleme işlemi için LaTeX kaynağı boş.",
                nameof(input));
        }

        if (
            input.LatexSource.Length >
            _options.MaxSourceCharacters
        )
        {
            throw new InvalidOperationException(
                "PDF derleme işlemi için LaTeX kaynağı izin verilen uzunluğu aşıyor.");
        }

        if (
            string.IsNullOrWhiteSpace(
                _options.CompilerPath)
        )
        {
            throw new InvalidOperationException(
                "LaTeX derleyici yolu tanımlı değil.");
        }

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "LaTeX derleme zaman aşımı pozitif olmalıdır.");
        }

        if (_options.MaxGeneratedPdfBytes <= 0)
        {
            throw new InvalidOperationException(
                "Maksimum oluşturulan PDF boyutu pozitif olmalıdır.");
        }
    }

    private static void ValidatePdfBytes(
        byte[] pdfBytes)
    {
        if (
            pdfBytes.Length < 5 ||
            pdfBytes[0] != '%' ||
            pdfBytes[1] != 'P' ||
            pdfBytes[2] != 'D' ||
            pdfBytes[3] != 'F' ||
            pdfBytes[4] != '-'
        )
        {
            throw new InvalidOperationException(
                "Tectonic çıktısının geçerli bir PDF imzası bulunmuyor.");
        }
    }

    private static string BuildCompilerOutput(
        string standardOutput,
        string standardError)
    {
        var output =
            new StringBuilder();

        if (
            !string.IsNullOrWhiteSpace(
                standardOutput)
        )
        {
            output.AppendLine(
                "STDOUT:");

            output.AppendLine(
                standardOutput.Trim());
        }

        if (
            !string.IsNullOrWhiteSpace(
                standardError)
        )
        {
            if (output.Length > 0)
            {
                output.AppendLine();
            }

            output.AppendLine(
                "STDERR:");

            output.AppendLine(
                standardError.Trim());
        }

        if (output.Length == 0)
        {
            return
                "Derleyici herhangi bir çıktı döndürmedi.";
        }

        var value =
            output.ToString();

        if (
            value.Length <=
            MaximumCompilerOutputLength
        )
        {
            return value;
        }

        return
            value[
                ..MaximumCompilerOutputLength
            ] +
            "...";
    }

    private static async Task
        TerminateProcessAsync(
            Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(
                    entireProcessTree:
                        true);
            }
        }
        catch
        {
            /*
             * Asıl timeout veya cancellation
             * hatasının korunması için process
             * sonlandırma hatası yutulur.
             */
        }

        try
        {
            await process.WaitForExitAsync(
                CancellationToken.None);
        }
        catch
        {
            /*
             * Asıl hata korunur.
             */
        }
    }

    private void TryDeleteWorkingDirectory(
        string temporaryRoot,
        string workingDirectory)
    {
        try
        {
            if (!Directory.Exists(
                    workingDirectory))
            {
                return;
            }

            var normalizedRoot =
                Path.GetFullPath(
                    temporaryRoot);

            var normalizedWorkingDirectory =
                Path.GetFullPath(
                    workingDirectory);

            var rootWithSeparator =
                normalizedRoot.EndsWith(
                    Path.DirectorySeparatorChar)
                    ? normalizedRoot
                    : normalizedRoot +
                      Path.DirectorySeparatorChar;

            if (
                !normalizedWorkingDirectory
                    .StartsWith(
                        rootWithSeparator,
                        StringComparison.Ordinal)
            )
            {
                logger.LogError(
                    "Geçici LaTeX klasörü güvenlik kontrolünden geçemedi. Directory: {WorkingDirectory}",
                    normalizedWorkingDirectory);

                return;
            }

            Directory.Delete(
                normalizedWorkingDirectory,
                recursive:
                    true);

            TryDeleteEmptyParentDirectory(
                normalizedRoot,
                Path.GetDirectoryName(
                    normalizedWorkingDirectory));
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Geçici LaTeX klasörü temizlenemedi. Directory: {WorkingDirectory}",
                workingDirectory);
        }
    }

    private static void TryDeleteEmptyParentDirectory(
        string normalizedRoot,
        string? parentDirectory)
    {
        if (
            string.IsNullOrWhiteSpace(
                parentDirectory) ||
            !Directory.Exists(
                parentDirectory)
        )
        {
            return;
        }

        if (
            string.Equals(
                normalizedRoot,
                parentDirectory,
                StringComparison.Ordinal)
        )
        {
            return;
        }

        if (
            Directory.EnumerateFileSystemEntries(
                    parentDirectory)
                .Any()
        )
        {
            return;
        }

        Directory.Delete(
            parentDirectory);
    }
}

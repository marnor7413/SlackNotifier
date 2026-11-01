using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SN.ConsoleApp.Diagnostics;

/// <summary>
/// Opt-in troubleshooting tool that writes every thrown exception of selected
/// types to standard error, including exceptions that are caught and handled
/// inside libraries.
/// </summary>
/// <remarks>
/// <para>
/// Why it exists: the metric dotnet_exceptions_total (meter System.Runtime)
/// shows how many exceptions of each type are thrown, but not where. This
/// logger answers where by writing the stack at the moment of the throw.
/// </para>
/// <para>
/// Configuration (section "Diagnostics:FirstChanceExceptions"):
/// Enabled turns the logging on. ExceptionTypes lists full type names to log,
/// matched exactly. "*" logs every type. An empty list logs nothing.
/// </para>
/// <para>
/// Why Console.Error and not ILogger: ILogger output is exported over OTLP/HTTP.
/// If that export throws, this handler would run again and could loop.
/// </para>
/// <para>
/// When disabled, the handler is never registered and has no cost.
/// </para>
/// </remarks>
internal static class FirstChanceExceptionLogger
{
    public const string SectionName = "Diagnostics:FirstChanceExceptions";

    // Guards against recursion if something inside the handler throws.
    [ThreadStatic]
    private static bool isHandling;

    private static HashSet<string> exceptionTypes = [];
    private static bool logAllTypes;

    public static void EnableIfConfigured(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<FirstChanceExceptionOptions>()
            ?? new FirstChanceExceptionOptions();

        if (!options.Enabled)
        {
            return;
        }

        exceptionTypes = new HashSet<string>(options.ExceptionTypes, StringComparer.Ordinal);

        if (exceptionTypes.Count == 0)
        {
            Console.Error.WriteLine(
                "---> First chance exception logging is enabled but no exception types are configured. " +
                "Nothing will be logged. Use \"*\" to log all types.");
            return;
        }

        logAllTypes = exceptionTypes.Contains("*");
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

        var scope = logAllTypes ? "all exception types" : string.Join(", ", exceptionTypes);
        Console.Error.WriteLine($"---> First chance exception logging enabled for: {scope}");
    }

    private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
    {
        if (isHandling)
        {
            return;
        }

        isHandling = true;
        try
        {
            var exception = e.Exception;
            var typeName = exception.GetType().FullName;

            if (!logAllTypes && !exceptionTypes.Contains(typeName))
            {
                return;
            }

            var text = new StringBuilder()
                .Append("---> [FirstChance] ")
                .Append(DateTimeOffset.UtcNow.ToString("O"))
                .Append(' ').Append(typeName).Append(": ").AppendLine(exception.Message);

            for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            {
                text.Append("     Inner: ").Append(inner.GetType().FullName)
                    .Append(": ").AppendLine(inner.Message);
            }

            text.AppendLine("     Stack at throw:").AppendLine(Environment.StackTrace);

            // One write per entry so that entries from different threads do not interleave.
            Console.Error.Write(text.ToString());
        }
        catch
        {
            // Diagnostics must never affect the application.
        }
        finally
        {
            // Also runs after the early return above, so the guard is always reset.
            isHandling = false;
        }
    }
}
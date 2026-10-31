using System.Diagnostics.Metrics;

namespace SN.Application.Metrics;

/// <summary>
/// Defines and records SlackNotifier's own application metrics: whether fetching email
/// from Gmail over IMAP and posting to Slack actually works, how much is processed and
/// which errors occur.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this class exists</b><br/>
/// A pod can be <c>Running</c> in Kubernetes without doing any useful work, for example
/// when the IMAP login is broken. Infrastructure metrics (pod status, CPU, memory) cannot
/// show that. These metrics describe the application's actual job, so that a failure is
/// visible in Prometheus and Grafana and not only in the log.
/// </para>
/// <para>
/// SlackNotifier pushes its metrics; Prometheus never scrapes the application. There is
/// therefore no <c>up</c> metric for the application. The last-success timestamps
/// compensate for that: if a timestamp stops advancing, the application has stopped
/// working, even if the pod is still running.
/// </para>
/// <para>
/// <b>How the metrics reach Prometheus</b><br/>
/// This class only uses the .NET metrics API in <see cref="System.Diagnostics.Metrics"/>
/// and has no dependency on OpenTelemetry. The OpenTelemetry SDK is configured in
/// SN.ConsoleApp, where it subscribes to the meter named <see cref="MeterName"/> through
/// <c>AddMeter</c>. The data then flows:
/// <code>
/// SlackNotifier --OTLP/HTTP--> OpenTelemetry Collector --OTLP/HTTP--> Prometheus
/// </code>
/// The SDK reads the instruments every 60 seconds by default and exports cumulative values.
/// </para>
/// <para>
/// <b>Lifetime and testability</b><br/>
/// Register as a singleton. The instruments must live as long as the application, and
/// short-lived services may depend on this class. The meter is created through
/// <see cref="IMeterFactory"/>, which the host registers automatically. In unit tests,
/// pass a test factory that scopes its meters to the factory instance and read the values
/// with <c>MetricCollector&lt;T&gt;</c>, so that parallel tests do not see each other's
/// measurements.
/// </para>
/// <para>
/// <b>Instruments</b><br/>
/// Names in code follow the OpenTelemetry guidelines: lowercase, dot-separated, with
/// <c>_</c> between words within a segment and UCUM units. Prometheus translates the
/// names with the <c>UnderscoreEscapingWithSuffixes</c> strategy: dots become
/// underscores, the unit is appended and counters get <c>_total</c>.
/// <list type="table">
///   <listheader>
///     <term>Name in code (type, unit)</term>
///     <description>Meaning, attributes and name in Prometheus</description>
///   </listheader>
///   <item>
///     <term><c>slacknotifier.imap.last_success_timestamp</c> (observable gauge, s)</term>
///     <description>
///     Unix time of the last successful IMAP fetch. Has no value until the first success
///     after start, so a missing series means the application has not succeeded since it
///     started. Prometheus: <c>slacknotifier_imap_last_success_timestamp_seconds</c>.
///     Seconds since the last success: <c>time() - slacknotifier_imap_last_success_timestamp_seconds</c>.
///     </description>
///   </item>
///   <item>
///     <term><c>slacknotifier.imap.emails.fetched</c> (counter, {email})</term>
///     <description>
///     Number of emails downloaded from IMAP, counted before validation and before
///     posting to Slack. Prometheus: <c>slacknotifier_imap_emails_fetched_total</c>.
///     </description>
///   </item>
///   <item>
///     <term><c>slacknotifier.imap.fetch.duration</c> (histogram, s)</term>
///     <description>
///     Duration of a complete IMAP fetch: connect, authenticate, download and disconnect.
///     Recorded for both successful and failed attempts. Uses custom bucket boundaries,
///     because the OpenTelemetry defaults are designed for milliseconds.
///     Prometheus: <c>slacknotifier_imap_fetch_duration_seconds_bucket</c>,
///     <c>_sum</c> and <c>_count</c>.
///     </description>
///   </item>
///   <item>
///     <term><c>slacknotifier.errors</c> (counter, {error})</term>
///     <description>
///     Number of failed operations. Attributes: <c>target</c> (<c>imap</c> or
///     <c>slack</c>) and <c>error.type</c>. Has no series until the first error.
///     Prometheus: <c>slacknotifier_errors_total</c> with labels <c>target</c> and
///     <c>error_type</c>.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Rules for attributes</b><br/>
/// Never put personal data in attributes: no senders, recipients, subjects or email
/// addresses. That protects the privacy of the housing association's members and keeps
/// cardinality low, since every unique attribute combination becomes its own time series
/// in Prometheus. <c>error.type</c> follows the OpenTelemetry semantic conventions and
/// must have low cardinality: the full type name of an exception, an HTTP status code or
/// a Slack error code. Never an exception message, which can contain arbitrary text.
/// </para>
/// <para>
/// <b>Thread safety</b><br/>
/// Values are recorded on the fetch thread, while the SDK invokes the gauge callback on
/// its export thread. The timestamp is therefore read and written with
/// <see cref="Interlocked"/>. The instruments themselves are thread-safe.
/// </para>
/// </remarks>
public sealed class SlackNotifierMetrics
{
    /// <summary>
    /// Name of the meter. SN.ConsoleApp must register exactly this name with
    /// <c>AddMeter</c>. A mismatch causes no error, only missing metrics.
    /// </summary>
    public const string MeterName = "SlackNotifier";
    private const string TargetImap = "imap";
    private readonly Counter<long> imapEmailsFetched;
    private readonly Histogram<double> imapFetchDuration;
    private readonly Counter<long> errors;
    private long imapLastSuccessUnixSeconds;

    /// <summary>
    /// Creates the meter and all instruments.
    /// </summary>
    /// <param name="meterFactory">
    /// Factory that owns the meter. Starting in .NET 8, hosts will automatically 
    /// register IMeterFactory in the service container or you can manually 
    /// register the type in any IServiceCollection by calling 
    /// AddMetrics. Meter can be  replaced by a test factory in unit tests.
    /// </param>
    public SlackNotifierMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(new MeterOptions(MeterName));

        meter.CreateObservableGauge(
            name: "slacknotifier.imap.last_success_timestamp",
            observeValues: ObserveImapLastSuccess,
            unit: "s",
            description: "Unix time of the last successful fetch from IMAP.");

        imapEmailsFetched = meter.CreateCounter<long>(
            name: "slacknotifier.imap.emails.fetched",
            unit: "{email}",
            description: "Number of emails downloaded from IMAP.");

        imapFetchDuration = meter.CreateHistogram<double>(
            name: "slacknotifier.imap.fetch.duration",
            unit: "s",
            description: "Duration of a complete IMAP fetch: connect, authenticate, download and disconnect.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 120] });

        errors = meter.CreateCounter<long>(
            name: "slacknotifier.errors",
            unit: "{error}",
            description: "Number of failed operations, by target and error type.");
    }

    /// <summary>
    /// Records a successful IMAP fetch: adds the number of downloaded emails and sets the
    /// last-success timestamp to the current time.
    /// </summary>
    /// <param name="emailCount">
    /// Number of downloaded emails. Zero is recorded too, so that the counter series exists
    /// from the first fetch.
    /// </param>
    public void ImapFetchSucceeded(int emailCount)
    {
        imapEmailsFetched.Add(emailCount);
        Interlocked.Exchange(ref imapLastSuccessUnixSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    /// <summary>
    /// Records a failed IMAP fetch in <c>slacknotifier.errors</c> with
    /// <c>target=imap</c> and the exception's full type name as <c>error.type</c>.
    /// </summary>
    /// <param name="exception">The exception that ended the fetch. Only its type is used.</param>
    public void ImapFetchFailed(Exception exception) =>
        RecordError(TargetImap, exception.GetType().FullName ?? exception.GetType().Name);

    /// <summary> Records the duration of an IMAP fetch, successful or not.</summary>
    /// <param name="duration">Elapsed time for the complete fetch.</param>
    public void RecordImapFetchDuration(TimeSpan duration) =>
        imapFetchDuration.Record(duration.TotalSeconds);

    private void RecordError(string target, string errorType) =>
        errors.Add(1,
            new KeyValuePair<string, object>("target", target),
            new KeyValuePair<string, object>("error.type", errorType));

    private IEnumerable<Measurement<long>> ObserveImapLastSuccess()
    {
        var value = Interlocked.Read(ref imapLastSuccessUnixSeconds);
        if (value == 0)
        {
            return [];
        }

        return [new Measurement<long>(value)];
    }
}
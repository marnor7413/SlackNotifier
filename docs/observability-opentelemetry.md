# Observability with OpenTelemetry: SlackNotifier

A summary of the work to make SlackNotifier observable with OpenTelemetry, Prometheus and Grafana in the k3s cluster. Completed on 2 November 2026.

## Purpose

- See whether SlackNotifier actually works, not just that the pod is `Running`.
- Learn OpenTelemetry properly as a basis for a proposal at work. Only stable components were therefore used: nothing beta, alpha or preview in the data path.
- Build a central solution that more applications can use later.

## Architecture

```
SlackNotifier (.NET 10)
  --OTLP/HTTP (protobuf)-->  OpenTelemetry Collector (namespace observability, port 4318)
  --OTLP/HTTP-->             Prometheus OTLP receiver (/api/v1/otlp/v1/metrics)
                             Grafana reads from Prometheus
```

The application pushes its metrics. Prometheus never scrapes the application, so there is no `up` metric for it. The last-success timestamps for fetching and posting take its place.

### Versions

| Component | Version |
|---|---|
| Prometheus | 3.15.1 (distroless) |
| OpenTelemetry Collector (core) | 0.164.0 |
| OpenTelemetry .NET (Extensions.Hosting, Exporter.OpenTelemetryProtocol) | 1.20.1 |
| Grafana | 13.2.4 (dashboards in V2 format) |

### Stability of the Collector components

Checked with the Collector's `components` command:

- The `otlp` receiver and the `otlp_http` exporter: Stable.
- `memory_limiter` and `batch`: Beta. Not used.
- `health_check`: Alpha. Not used. The readiness probe is a `tcpSocket` check instead.

## Configuration

### Prometheus

- The flag `--web.enable-otlp-receiver` in the Deployment.
- In `prometheus.yml`:
    - `otlp.translation_strategy: UnderscoreEscapingWithSuffixes`. This is the default, written explicitly so that the choice is visible in Git. Dots become underscores, the unit is appended as a suffix and counters get `_total`.
    - `otlp.promote_resource_attributes` with the list recommended by Prometheus. `service.name` becomes `job`, `service.instance.id` becomes `instance` and `service.version` becomes the label `service_version`.
- Out-of-order ingestion is disabled, since the feature is experimental.

### Collector

```yaml
receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318
exporters:
  otlp_http:
    endpoint: http://prometheus.observability.svc.cluster.local:9090/api/v1/otlp
service:
  pipelines:
    metrics:
      receivers: [otlp]
      exporters: [otlp_http]
```

There is only a metrics pipeline. Logs sent by the application get a 404 response until a logs pipeline exists (Loki).

### SlackNotifier

Environment variables in the Deployment:

| Variable | Value |
|---|---|
| `OTEL_SERVICE_NAME` | `slacknotifier` |
| `OTEL_RESOURCE_ATTRIBUTES` | `service.instance.id=$(POD_NAME)` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector.observability.svc.cluster.local:4318` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `http/protobuf` (the default is gRPC) |

In code (`ConfigureOpenTelemetry`):

- `AddMeter("System.Runtime")` for heap, memory and CPU.
- `AddMeter("System.Net.Http")` for HTTP calls to Slack.
- `AddMeter("SlackNotifier")` for the application's own metrics.
- `AddView("dotnet.exceptions", MetricStreamConfiguration.Drop)`, see the lessons learned below.
- `UseOtlpExporter()` for all signals.

## The application's own metrics

All are defined in `SlackNotifierMetrics` (SN.Application), which only uses `System.Diagnostics.Metrics` and has no dependency on OpenTelemetry. The class is registered as a singleton and creates its meter through `IMeterFactory`.

| Name in Prometheus | Type | Meaning |
|---|---|---|
| `slacknotifier_imap_last_success_timestamp_seconds` | gauge | Last successful IMAP fetch |
| `slacknotifier_imap_emails_fetched_total` | counter | Fetched emails |
| `slacknotifier_imap_fetch_duration_seconds` | histogram | Duration of a complete IMAP fetch |
| `slacknotifier_slack_last_success_timestamp_seconds` | gauge | Last email that Slack accepted in full |
| `slacknotifier_slack_emails_posted_total` | counter | Posted emails |
| `slacknotifier_slack_attachments_uploaded_total` | counter | Shared files |
| `slacknotifier_slack_attachments_size_bytes_total` | counter | Size of shared files |
| `slacknotifier_errors_total{target, error_type}` | counter | Failed operations against IMAP or Slack |
| `slacknotifier_runs_failed_total{error_type}` | counter | Aborted runs, including `strategy_not_found` |

Rules:

- No personal data in attributes: no senders, recipients, subjects or addresses.
- `error.type` has low cardinality: the exception type name, an HTTP status code or a Slack error code. Never an error message.
- Counters without attributes are initialized with `Add(0)`. Counters with `error.type` are not, since the values are not known in advance.

Failed operations and aborted runs measure different things. An IMAP error is counted in both. A Slack error does not abort the run and is only counted in `slacknotifier_errors_total`.

## Dashboard: SlackNotifier

Provisioned from file, `observability/dashboards/slacknotifier.json` in k8s-gitops.

1. **Is SlackNotifier working?** Time since the last IMAP fetch (yellow from 35 minutes, red from 45), time since the last Slack post, errors since the pod started, fetched, posted and fetched but not posted.
2. **Volume per day:** emails, attachments and attachment size per day. Errors per type and aborted runs per error type since the pod started.
3. **Response times:** average IMAP duration per hour, Slack calls per server and status code.
4. **.NET runtime:** heap after the last GC, working set and CPU.

## Lessons learned

### OpenTelemetry and Prometheus

- `increase()` and `rate()` cannot see the first increase in a series that is born with a value above 0. That is why counters are initialized to zero. Emails fetched in the first run after startup, before the first export, are still not counted in the volume panels. This is a deliberate choice to live with.
- Prometheus skips `target_info` when the resource only has identifying attributes (`service.name`, `service.namespace` and `service.instance.id`).
- A pod restart creates new series, because `instance` changes. During the 5-minute lookback, old and new series can overlap.
- `UseOtlpExporter` enables all signals. ILogger logs are therefore sent even though the Collector has no logs pipeline.

### Grafana

- Grafana aligns the query time range to the step. With a min step of `1d`, the points land on 00:00 UTC, so a day in progress is not shown until it has ended.
- The error line `[SHOULD NOT HAPPEN] failed to update managedFields` for file-provisioned V2 dashboards is a known bug in Grafana 13 and does not affect functionality.
- The Table tab in the Prometheus UI runs an instant query with a 5-minute lookback. Grafana runs a range query. Series from a stopped pod are therefore visible in Grafana but not in the Table tab.

### `dotnet.exceptions` is noise

- The metric counts every thrown exception, including those caught inside libraries. It cannot separate expected exceptions from real errors.
- An exception rethrown through several async layers is counted once per layer.
- The steady stream of IOException came from the HttpClient connection pool, which cleans up idle connections to the Collector (`CheckUsabilityOnScavenge`). This is completely normal.
- The metric is therefore dropped with a View. Real errors are counted where our code catches them.

### .NET and platform

- The generic host reads `DOTNET_ENVIRONMENT`, not `ASPNETCORE_ENVIRONMENT`.
- Linux file names are case sensitive. `appsettings.production.json` was never loaded in the pod. A rename that only changes case is not registered by Git on Windows (`core.ignorecase`) and requires a two-step `git mv`.
- From .NET 10, all of `BackgroundService.ExecuteAsync` runs in the background when `StartAsync` is called. Tests must not assume that the first iteration is done when `StartAsync` returns.
- xunit.v3 4.x uses Microsoft Testing Platform v2, which does not run under VSTest-based `dotnet test`. The project stays on xunit.v3 3.2.2.

## Investigating exceptions

`FirstChanceExceptionLogger` (SN.Console) writes the type, message, inner exceptions and the stack at the throw to `Console.Error`. It is disabled by default and controlled by configuration:

```
Diagnostics__FirstChanceExceptions__Enabled=true
```

`ExceptionTypes` in `appsettings.Production.json` lists the types to log (exact type name). `*` logs every type. An empty list logs nothing. The output is read with `kubectl logs`.

## Deferred

- Loki for logs. This removes the 404 responses. Keep in mind that log text can contain personal data.
- Emails that can be lost: they are marked as read in Gmail before they are posted to Slack.
- Traces.
- A NuGet package for the telemetry code when application number two arrives.
- Calling `ForceFlush` before the first run, so that emails fetched at startup are counted.

## Basis for a proposal at work

- The application code only depends on `System.Diagnostics.Metrics`, which is part of .NET. OpenTelemetry is configured in one place at startup and can be replaced.
- The Collector decouples the applications from the storage. A new backend requires a change in the Collector, not in the applications.
- Prometheus accepts OTLP directly with a flag, so no extra adapter is needed.
- Stable components exist for the entire metrics path: the OTLP receiver and OTLP/HTTP exporter in the Collector, the Prometheus OTLP receiver and OpenTelemetry .NET 1.20.x.
- What matters most is what is measured. Metrics from our own code, such as failed operations and aborted runs, could be acted on. Generic runtime metrics such as `dotnet.exceptions` cost an afternoon without result.

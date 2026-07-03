import { logs, SeverityNumber, type AnyValueMap } from '@opentelemetry/api-logs';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { BatchLogRecordProcessor, LoggerProvider } from '@opentelemetry/sdk-logs';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-http';

// Fixed localhost port, not read from config: the browser bundle can't resolve Aspire's
// container-network DNS (signoz-otel-collector) or read the dev-server process's env vars —
// it can only reach a published host port. Kept in sync with the fixed port on the
// otel-collector's "otlp-http" endpoint in GariKaagada.AppHost/AppHost.cs.
const OTLP_HTTP_LOGS_ENDPOINT = 'http://localhost:4318/v1/logs';

const SERVICE_NAME = 'gari-kagada-client';

export function initTelemetry(): void {
  const loggerProvider = new LoggerProvider({
    resource: resourceFromAttributes({ 'service.name': SERVICE_NAME }),
    processors: [new BatchLogRecordProcessor(new OTLPLogExporter({ url: OTLP_HTTP_LOGS_ENDPOINT }))],
  });

  logs.setGlobalLoggerProvider(loggerProvider);
}

export function logError(error: unknown, attributes: AnyValueMap = {}): void {
  const logger = logs.getLogger(SERVICE_NAME);
  logger.emit({
    severityNumber: SeverityNumber.ERROR,
    severityText: 'ERROR',
    body: error instanceof Error ? error.message : String(error),
    attributes: {
      ...attributes,
      ...(error instanceof Error && error.stack ? { 'exception.stacktrace': error.stack } : {}),
    },
  });
}

using System;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace BlazorApp1.Server
{
    public static class SeriLogConfiguration
    {
        public static Action<HostBuilderContext, LoggerConfiguration> ConfigureLogger =>

            (hostingContext, loggerConfiguration) =>
            {
                var env = hostingContext.HostingEnvironment;
                var applicationName = env.ApplicationName;
                var environmentName = env.EnvironmentName;
                var indexFormat = hostingContext.Configuration["IndexFormat"];

                loggerConfiguration.MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .Enrich.WithProperty("ApplicationName", applicationName)
                    .Enrich.WithProperty("EnvironmentName", environmentName)
                    .Enrich.With<ActivityEnricher>()
                    .WriteTo.Console();

                if (hostingContext.HostingEnvironment
                    .IsDevelopment())
                {
                    loggerConfiguration.MinimumLevel.Override("BlazorApp1", LogEventLevel.Debug);
                }

                var elasticUrl = "http://localhost:9200";
                if (!string.IsNullOrEmpty(elasticUrl))
                {
                    loggerConfiguration.WriteTo.Elasticsearch(

                        new ElasticsearchSinkOptions(new Uri(elasticUrl))
                        {
                            AutoRegisterTemplate = true,
                            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                            IndexFormat = indexFormat,
                            MinimumLogEventLevel = LogEventLevel.Debug
                        });
                }
            };
    }

    public class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;

            logEvent.AddPropertyIfAbsent(new LogEventProperty("SpanId", new ScalarValue(activity.GetSpanId())));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("TraceId", new ScalarValue(activity.GetTraceId())));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("ParentId", new ScalarValue(activity.GetParentId())));
        }
    }

    internal static class ActivityExtensions
    {
        public static string GetSpanId(this Activity activity)
        {
            if (activity == null) return string.Empty;

            switch (activity.IdFormat)
            {
                case ActivityIdFormat.Hierarchical:
                    return activity.Id;
                case ActivityIdFormat.W3C:
                    return activity.SpanId.ToHexString();
                default:
                    return string.Empty;
            }
        }

        public static string GetTraceId(this Activity activity)
        {
            if (activity == null) return string.Empty;

            switch (activity.IdFormat)
            {
                case ActivityIdFormat.Hierarchical:
                    return activity.RootId;
                case ActivityIdFormat.W3C:
                    return activity.TraceId.ToHexString();
                default:
                    return string.Empty;
            }
        }

        public static string GetParentId(this Activity activity)
        {
            if (activity == null) return string.Empty;

            switch (activity.IdFormat)
            {
                case ActivityIdFormat.Hierarchical:
                    return activity.ParentId;
                case ActivityIdFormat.W3C:
                    return activity.ParentSpanId.ToHexString();
                default:
                    return string.Empty;
            }
        }
    }
}
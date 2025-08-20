// File: /src/LimboDancer.MCP.Cli/Commands/VectorInitCommand.cs
// Purpose:
//   CLI command to initialize (create or update) the Azure AI Search index used by VectorStore.
//   Aligned with SearchIndexBuilder's static API and constants.
//
// Usage (examples):
//   ldm vector init --endpoint https://<search>.search.windows.net --api-key <key>
//   ldm vector init --endpoint ... --api-key ... --index-name my-index --vector-dimensions 3072
//   ldm vector init --endpoint ... --api-key ... --semantic-config my-semantic
//
// Exit codes: 0 = success; non-zero = error.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Core;
using LimboDancer.MCP.Vector.AzureSearch;

namespace LimboDancer.MCP.Cli.Commands
{
    public static class VectorInitCommand
    {
        public sealed class Options
        {
            public string? Endpoint { get; init; }
            public string? ApiKey { get; init; }

            public string IndexName { get; init; } = SearchIndexBuilder.DefaultIndexName;
            public int VectorDimensions { get; init; } = 1536;
            public string SemanticConfig { get; init; } = SearchIndexBuilder.DefaultSemanticConfig;

            public bool Quiet { get; init; } = false; // suppress non-error output
        }

        public static async Task<int> RunAsync(Options opts, CancellationToken ct = default)
        {
            try
            {
                Validate(opts);

                var endpoint = new Uri(opts.Endpoint!, UriKind.Absolute);
                var credential = new AzureKeyCredential(opts.ApiKey!);
                var idxClient = new SearchIndexClient(endpoint, credential);

                if (!opts.Quiet)
                {
                    Console.WriteLine($"[VectorInit] Endpoint        : {endpoint}");
                    Console.WriteLine($"[VectorInit] Index Name      : {opts.IndexName}");
                    Console.WriteLine($"[VectorInit] Vector Dimensions: {opts.VectorDimensions}");
                    Console.WriteLine($"[VectorInit] Semantic Config : {opts.SemanticConfig}");
                    Console.WriteLine($"[VectorInit] Vector Profile  : {SearchIndexBuilder.DefaultVectorProfile}");
                    Console.WriteLine();
                    Console.WriteLine("[VectorInit] Ensuring index exists and schema is aligned...");
                }

                await SearchIndexBuilder.EnsureIndexAsync(
                    client: idxClient,
                    indexName: opts.IndexName,
                    vectorDimensions: opts.VectorDimensions,
                    semanticConfigName: opts.SemanticConfig,
                    ct: ct);

                if (!opts.Quiet)
                {
                    Console.WriteLine("[VectorInit] ✅ Index ensured (create/update complete).");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[VectorInit] Canceled.");
                return 130; // typical Unix SIGINT
            }
            catch (RequestFailedException ex)
            {
                Console.Error.WriteLine($"[VectorInit] Azure request failed: {ex.Status} {ex.Message}");
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[VectorInit] Error: {ex.Message}");
                return 1;
            }
        }

        private static void Validate(Options opts)
        {
            if (string.IsNullOrWhiteSpace(opts.Endpoint))
                throw new ArgumentException("--endpoint is required (e.g., https://<service>.search.windows.net).");
            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new ArgumentException("--api-key is required (an admin key).");

            if (opts.VectorDimensions <= 0)
                throw new ArgumentException("--vector-dimensions must be a positive integer.");

            // Allow default index name & semantic config; user may override.
            if (string.IsNullOrWhiteSpace(opts.IndexName))
                throw new ArgumentException("--index-name must not be empty.");
            if (string.IsNullOrWhiteSpace(opts.SemanticConfig))
                throw new ArgumentException("--semantic-config must not be empty.");
        }
    }
}

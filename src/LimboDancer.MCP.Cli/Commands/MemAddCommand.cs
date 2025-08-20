// File: /src/LimboDancer.MCP.Cli/Commands/MemAddCommand.cs
// Purpose:
//   CLI command to ingest (upload) memory documents into Azure AI Search.
//   Ensures TenantId is set on all docs and aligns with VectorStore + SearchIndexBuilder.
//
// Usage (examples):
//   ldm mem add --endpoint https://<svc>.search.windows.net --api-key <key> --tenant t1 --file ./docs.json
//   ldm mem add --endpoint ... --api-key ... --tenant t1 --content "hello world" --id my-doc-1 --label "Hello"
//   ldm mem add --endpoint ... --api-key ... --tenant t1 --file ./single-doc.json --vector-dims 1536
//
// Notes:
//   - --file may contain a single MemoryDoc object or an array of MemoryDoc objects.
//   - If you pass --content/--label/etc., a single MemoryDoc will be constructed from flags.
//   - TenantId is required (from --tenant). ApplyTenant() enforces it.
//   - System.Text.Json is used throughout.

using System;
using System.Collections.Generic;
using System.CommandLine; // If you don't use System.CommandLine, you can remove and bind manually.
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Core;
using LimboDancer.MCP.Vector.AzureSearch;
using LimboDancer.MCP.Vector.AzureSearch.Models;

namespace LimboDancer.MCP.Cli.Commands
{
    public static class MemAddCommand
    {
        public sealed class Options
        {
            public string? Endpoint { get; init; }
            public string? ApiKey { get; init; }
            public string IndexName { get; init; } = SearchIndexBuilder.DefaultIndexName;

            public string? Tenant { get; init; } // REQUIRED for ingestion

            // Input options (choose one path)
            public string? File { get; init; }     // JSON file containing MemoryDoc or MemoryDoc[]
            public string? Content { get; init; }  // Inline content to build a single doc

            // Optional single-doc metadata (when using --content)
            public string? Id { get; init; }
            public string? Label { get; init; }
            public string? Kind { get; init; }
            public string? Status { get; init; }
            public string? Tags { get; init; }

            // Optional vector dimension sanity check
            public int? VectorDims { get; init; }

            // Quiet output
            public bool Quiet { get; init; } = false;
        }

        public static async Task<int> RunAsync(Options opts, CancellationToken ct = default)
        {
            try
            {
                Validate(opts);

                var endpoint = new Uri(opts.Endpoint!, UriKind.Absolute);
                var client = new SearchClient(endpoint, opts.IndexName, new AzureKeyCredential(opts.ApiKey!));
                var store = new VectorStore(client);

                // Build documents
                var docs = await LoadDocsAsync(opts, ct);

                // Apply tenant + validate
                ApplyTenant(opts.Tenant!, docs);
                ValidateDocs(docs, opts.VectorDims);

                if (!opts.Quiet)
                {
                    Console.WriteLine($"[MemAdd] Endpoint   : {endpoint}");
                    Console.WriteLine($"[MemAdd] Index Name : {opts.IndexName}");
                    Console.WriteLine($"[MemAdd] Tenant     : {opts.Tenant}");
                    Console.WriteLine($"[MemAdd] Count      : {docs.Count}");
                    if (opts.VectorDims.HasValue)
                        Console.WriteLine($"[MemAdd] VectorDims : {opts.VectorDims.Value}");
                    Console.WriteLine("[MemAdd] Uploading...");
                }

                await store.UploadAsync(docs, ct);

                if (!opts.Quiet)
                {
                    Console.WriteLine("[MemAdd] ✅ Upload complete.");
                }

                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("[MemAdd] Canceled.");
                return 130;
            }
            catch (RequestFailedException ex)
            {
                Console.Error.WriteLine($"[MemAdd] Azure request failed: {ex.Status} {ex.Message}");
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MemAdd] Error: {ex.Message}");
                return 1;
            }
        }

        // -----------------------------
        // Helpers
        // -----------------------------

        private static void Validate(Options o)
        {
            if (string.IsNullOrWhiteSpace(o.Endpoint))
                throw new ArgumentException("--endpoint is required (e.g., https://<service>.search.windows.net).");
            if (string.IsNullOrWhiteSpace(o.ApiKey))
                throw new ArgumentException("--api-key is required (admin key).");
            if (string.IsNullOrWhiteSpace(o.Tenant))
                throw new ArgumentException("--tenant is required for ingestion.");
            if (string.IsNullOrWhiteSpace(o.File) && string.IsNullOrWhiteSpace(o.Content))
                throw new ArgumentException("Provide either --file <path> or --content <text>.");
            if (!string.IsNullOrWhiteSpace(o.File) && !string.IsNullOrWhiteSpace(o.Content))
                throw new ArgumentException("Use either --file or --content, not both.");
            if (string.IsNullOrWhiteSpace(o.IndexName))
                throw new ArgumentException("--index-name must not be empty.");
            if (o.VectorDims.HasValue && o.VectorDims.Value <= 0)
                throw new ArgumentException("--vector-dims must be a positive integer when specified.");
        }

        private static async Task<List<MemoryDoc>> LoadDocsAsync(Options o, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(o.File))
            {
                var json = await File.ReadAllTextAsync(o.File!, Encoding.UTF8, ct);

                // Try array first; fall back to single object
                try
                {
                    var many = JsonSerializer.Deserialize<List<MemoryDoc>>(json, JsonOptions());
                    if (many != null)
                        return many;
                }
                catch
                {
                    // ignore and retry single
                }

                var single = JsonSerializer.Deserialize<MemoryDoc>(json, JsonOptions())
                             ?? throw new ArgumentException("Unable to parse JSON as MemoryDoc or MemoryDoc[].");
                return new List<MemoryDoc> { single };
            }

            // Build from flags (single doc)
            var id = !string.IsNullOrWhiteSpace(o.Id) ? o.Id! : Guid.NewGuid().ToString("n");
            var doc = MemoryDoc.Create(
                id: id,
                tenantId: o.Tenant!, // will be re-applied by ApplyTenant() anyway
                content: o.Content ?? string.Empty,
                contentVector: Array.Empty<float>(),
                label: o.Label,
                kind: o.Kind,
                status: o.Status,
                tags: o.Tags,
                createdUtc: DateTimeOffset.UtcNow,
                updatedUtc: DateTimeOffset.UtcNow
            );
            return new List<MemoryDoc> { doc };
        }

        /// <summary>
        /// Ensure every document has TenantId set to the provided tenant; throw if a conflicting value exists.
        /// </summary>
        private static void ApplyTenant(string tenantId, List<MemoryDoc> docs)
        {
            foreach (var d in docs)
            {
                if (string.IsNullOrWhiteSpace(d.TenantId))
                {
                    // MemoryDoc requires TenantId in its ctor for JSON paths; this covers programmatic paths or legacy docs.
                    var applied = new MemoryDoc(
                        id: d.Id,
                        tenantId: tenantId,
                        label: d.Label,
                        kind: d.Kind,
                        status: d.Status,
                        tags: d.Tags,
                        content: d.Content,
                        contentVector: d.ContentVector,
                        createdUtc: d.CreatedUtc,
                        updatedUtc: d.UpdatedUtc
                    );

                    // Copy back (replace reference)
                    var idx = docs.IndexOf(d);
                    docs[idx] = applied;
                }
                else if (!string.Equals(d.TenantId, tenantId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Document {d.Id} has TenantId='{d.TenantId}', which conflicts with provided --tenant='{tenantId}'.");
                }
            }
        }

        private static void ValidateDocs(List<MemoryDoc> docs, int? expectedVectorDims)
        {
            foreach (var d in docs)
            {
                d.Validate(expectedVectorDims);
            }
        }

        private static JsonSerializerOptions JsonOptions() => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }
}

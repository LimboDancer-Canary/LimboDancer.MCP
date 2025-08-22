# Vector Search Configuration

## Index Naming

The LimboDancer MCP platform uses `ldm-memory` as the default Azure AI Search index name. This name is consistent across all components:

- SearchIndexBuilder: `DefaultIndexName = "ldm-memory"`
- VectorStore: Uses the same default
- Configuration: Can override via `VectorOptions.IndexName`

The "ldm" prefix stands for "LimboDancer Memory" and helps identify platform-owned indices in shared Azure Search services.

## Vector Dimensions

Default vector dimensions: **1536** (compatible with OpenAI text-embedding-ada-002)

The dimension is configurable at index creation time via `SearchIndexBuilder.EnsureIndexAsync()` but must remain consistent across:
- Index schema
- Embedding generation
- Query vectors

## Configuration

```json
{
  "Vector": {
    "Endpoint": "https://<your-service>.search.windows.net",
    "ApiKey": "<admin-key>",
    "IndexName": "ldm-memory",
    "VectorDimensions": 1536
  }
}
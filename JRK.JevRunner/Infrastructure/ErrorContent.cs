using System.Text.Json.Nodes;

namespace JRK.JevRunner.Infrastructure;

/// <summary>Represents an API error body as parsed JSON or unparsed response text.</summary>
public union ErrorContent(JsonNode,string);
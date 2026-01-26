using Neo4j.Driver;
using System.Text.Json;

namespace CharityRabbit.Data;

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphEdge
{
    public string Id { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class GraphData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphStats
{
    public Dictionary<string, int> NodeCounts { get; set; } = new();
    public Dictionary<string, int> RelationshipCounts { get; set; } = new();
    public int TotalNodes { get; set; }
    public int TotalRelationships { get; set; }
}

public class GraphExplorerService
{
    private readonly IDriver _driver;

    public GraphExplorerService(IDriver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Get overall graph statistics
    /// </summary>
    public async Task<GraphStats> GetGraphStatsAsync()
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var stats = new GraphStats();

            // Get node counts by label
            var nodeQuery = @"
                MATCH (n)
                RETURN labels(n) as labels, count(n) as count";

            var nodeCursor = await tx.RunAsync(nodeQuery);
            await foreach (var record in nodeCursor)
            {
                var labels = record["labels"].As<List<string>>();
                var count = record["count"].As<int>();
                if (labels.Any())
                {
                    var label = labels.First();
                    stats.NodeCounts[label] = count;
                    stats.TotalNodes += count;
                }
            }

            // Get relationship counts by type
            var relQuery = @"
                MATCH ()-[r]->()
                RETURN type(r) as type, count(r) as count";

            var relCursor = await tx.RunAsync(relQuery);
            await foreach (var record in relCursor)
            {
                var type = record["type"].As<string>();
                var count = record["count"].As<int>();
                stats.RelationshipCounts[type] = count;
                stats.TotalRelationships += count;
            }

            return stats;
        });
    }

    /// <summary>
    /// Get a sample of the graph (limited nodes and relationships)
    /// </summary>
    public async Task<GraphData> GetGraphSampleAsync(int limit = 50, string? nodeType = null, string? relationshipType = null)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var graphData = new GraphData();

            // Build query based on filters
            var nodeFilter = string.IsNullOrEmpty(nodeType) ? "" : $":{nodeType}";
            var relFilter = string.IsNullOrEmpty(relationshipType) ? "" : $":{relationshipType}";

            // Use bidirectional pattern to capture both incoming and outgoing relationships
            var query = $@"
                MATCH (n{nodeFilter})-[r{relFilter}]-(m)
                RETURN n, r, m
                LIMIT {limit}";

            var cursor = await tx.RunAsync(query);

            var nodeIds = new HashSet<string>();
            var edgeIds = new HashSet<string>();

            await foreach (var record in cursor)
            {
                // Process source node
                var sourceNode = record["n"].As<INode>();
                var sourceId = $"n{sourceNode.Id}";
                if (!nodeIds.Contains(sourceId))
                {
                    graphData.Nodes.Add(MapNode(sourceNode));
                    nodeIds.Add(sourceId);
                }

                // Process target node
                var targetNode = record["m"].As<INode>();
                var targetId = $"n{targetNode.Id}";
                if (!nodeIds.Contains(targetId))
                {
                    graphData.Nodes.Add(MapNode(targetNode));
                    nodeIds.Add(targetId);
                }

                // Process relationship (avoid duplicates since bidirectional pattern returns each rel twice)
                var relationship = record["r"].As<IRelationship>();
                var edgeId = $"r{relationship.Id}";
                if (!edgeIds.Contains(edgeId))
                {
                    graphData.Edges.Add(MapRelationship(relationship));
                    edgeIds.Add(edgeId);
                }
            }

            return graphData;
        });
    }

    /// <summary>
    /// Get neighbors of a specific node
    /// </summary>
    public async Task<GraphData> GetNodeNeighborsAsync(string nodeId, int depth = 1)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var graphData = new GraphData();
            var id = long.Parse(nodeId.Replace("n", ""));

            var query = $@"
                MATCH path = (n)-[r*1..{depth}]-(m)
                WHERE id(n) = $nodeId
                RETURN nodes(path) as nodes, relationships(path) as relationships";

            var cursor = await tx.RunAsync(query, new { nodeId = id });

            var nodeIds = new HashSet<string>();

            await foreach (var record in cursor)
            {
                var nodes = record["nodes"].As<List<INode>>();
                var relationships = record["relationships"].As<List<IRelationship>>();

                // Add all nodes
                foreach (var node in nodes)
                {
                    var nId = $"n{node.Id}";
                    if (!nodeIds.Contains(nId))
                    {
                        graphData.Nodes.Add(MapNode(node));
                        nodeIds.Add(nId);
                    }
                }

                // Add all relationships
                foreach (var rel in relationships)
                {
                    var edge = MapRelationship(rel);
                    if (!graphData.Edges.Any(e => e.Id == edge.Id))
                    {
                        graphData.Edges.Add(edge);
                    }
                }
            }

            return graphData;
        });
    }

    /// <summary>
    /// Search for nodes by label and property value
    /// </summary>
    public async Task<List<GraphNode>> SearchNodesAsync(string? label = null, string? searchText = null, int limit = 20)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var nodes = new List<GraphNode>();
            var labelFilter = string.IsNullOrEmpty(label) ? "" : $":{label}";

            string query;
            Dictionary<string, object> parameters;

            if (!string.IsNullOrEmpty(searchText))
            {
                // Search across common text properties
                query = $@"
                    MATCH (n{labelFilter})
                    WHERE 
                        (n.name IS NOT NULL AND toLower(n.name) CONTAINS toLower($searchText)) OR
                        (n.title IS NOT NULL AND toLower(n.title) CONTAINS toLower($searchText)) OR
                        (n.description IS NOT NULL AND toLower(n.description) CONTAINS toLower($searchText)) OR
                        (n.email IS NOT NULL AND toLower(n.email) CONTAINS toLower($searchText))
                    RETURN n
                    LIMIT $limit";
                parameters = new Dictionary<string, object> { { "searchText", searchText }, { "limit", limit } };
            }
            else
            {
                query = $@"
                    MATCH (n{labelFilter})
                    RETURN n
                    LIMIT $limit";
                parameters = new Dictionary<string, object> { { "limit", limit } };
            }

            var cursor = await tx.RunAsync(query, parameters);

            await foreach (var record in cursor)
            {
                var node = record["n"].As<INode>();
                nodes.Add(MapNode(node));
            }

            return nodes;
        });
    }

    /// <summary>
    /// Get detailed information about a specific node
    /// </summary>
    public async Task<GraphNode?> GetNodeDetailsAsync(string nodeId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var id = long.Parse(nodeId.Replace("n", ""));

            var query = @"
                MATCH (n)
                WHERE id(n) = $nodeId
                RETURN n";

            var cursor = await tx.RunAsync(query, new { nodeId = id });

            if (await cursor.FetchAsync())
            {
                var node = cursor.Current["n"].As<INode>();
                return MapNode(node);
            }

            return null;
        });
    }

    /// <summary>
    /// Get detailed information about a specific relationship
    /// </summary>
    public async Task<GraphEdge?> GetRelationshipDetailsAsync(string relationshipId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var id = long.Parse(relationshipId.Replace("r", ""));

            var query = @"
                MATCH ()-[r]->()
                WHERE id(r) = $relId
                RETURN r";

            var cursor = await tx.RunAsync(query, new { relId = id });

            if (await cursor.FetchAsync())
            {
                var rel = cursor.Current["r"].As<IRelationship>();
                return MapRelationship(rel);
            }

            return null;
        });
    }

    private GraphNode MapNode(INode node)
    {
        var labels = node.Labels.ToList();
        var props = node.Properties.ToDictionary(p => p.Key, p => p.Value);

        // Try to get a display title from common properties
        string? title = null;

        if (labels.Contains("Location"))
        {
            var parts = new List<string>();
            
            if (props.TryGetValue("city", out var cityObj) && cityObj != null)
                parts.Add(cityObj.ToString() ?? "");
                
            if (props.TryGetValue("state", out var stateObj) && stateObj != null)
                parts.Add(stateObj.ToString() ?? "");

            if (parts.Any(p => !string.IsNullOrEmpty(p)))
            {
                title = string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
                
                if (props.TryGetValue("zip", out var zipObj) && zipObj != null && !string.IsNullOrEmpty(zipObj.ToString()))
                {
                    title += $" ({zipObj})";
                }
            }
            else if (props.TryGetValue("zip", out var zipOnly) && zipOnly != null)
            {
                title = zipOnly.ToString();
            }
        }

        if (string.IsNullOrEmpty(title))
        {
            title = props.ContainsKey("name") ? props["name"]?.ToString() 
                : props.ContainsKey("title") ? props["title"]?.ToString()
                : props.ContainsKey("email") ? props["email"]?.ToString()
                : $"{labels.FirstOrDefault() ?? "Node"} {node.Id}";
        }

        return new GraphNode
        {
            Id = $"n{node.Id}",
            Label = labels.FirstOrDefault() ?? "Unknown",
            Title = title ?? $"Node {node.Id}",
            Type = labels.FirstOrDefault() ?? "Unknown",
            Properties = props
        };
    }

    private GraphEdge MapRelationship(IRelationship relationship)
    {
        var props = relationship.Properties.ToDictionary(p => p.Key, p => p.Value);

        return new GraphEdge
        {
            Id = $"r{relationship.Id}",
            From = $"n{relationship.StartNodeId}",
            To = $"n{relationship.EndNodeId}",
            Type = relationship.Type,
            Label = relationship.Type,
            Properties = props
        };
    }
}

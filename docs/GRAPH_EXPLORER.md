# Graph Explorer - Interactive Neo4j Database Visualization

## Overview
The Graph Explorer is a powerful admin tool that provides an interactive, visual way to explore and navigate the Neo4j graph database. It allows you to see nodes, relationships, and their properties in a visual graph format.

## Features

### ?? Interactive Graph Visualization
- **Visual Graph Layout**: Nodes and edges rendered using vis-network library
- **Physics-Based Layout**: Automatic positioning with attractive force-directed layout
- **Color-Coded Nodes**: Different colors for each node type (GoodWork, Organization, User, etc.)
- **Relationship Arrows**: Directed edges showing relationship flow

### ?? Search & Filtering
- **Node Type Filter**: Filter by specific node labels (GoodWork, Organization, User, etc.)
- **Relationship Type Filter**: Show only specific relationship types
- **Text Search**: Search nodes by name, title, description, or email
- **Sample Size Control**: Limit number of nodes loaded (10-200)

### ??? Interactions
- **Click Node**: View detailed properties in side panel
- **Double-Click Node**: Expand neighbors and relationships
- **Click Edge**: View relationship details and properties
- **Drag to Pan**: Move around the graph
- **Scroll to Zoom**: Zoom in/out of graph
- **Focus Node**: Click search result to center and highlight node

### ?? Statistics Dashboard
- **Node Counts**: Total nodes by type
- **Relationship Counts**: Total relationships by type
- **Real-time Updates**: Stats refresh with each query

### ?? Details Panel
Shows complete information for selected nodes/edges:
- Node properties (all fields from database)
- Relationship properties and direction
- Ability to expand neighbors
- Formatted display with property tables

## How to Use

### Accessing the Graph Explorer
1. Navigate to **Admin ? Graph Explorer** (Development mode only)
2. The page loads with initial statistics and empty graph

### Loading Data
1. **Default Sample**: Click "Load Graph" to load 50 random nodes
2. **Filtered Sample**: 
   - Select a Node Type (e.g., "GoodWork")
   - Select a Relationship Type (e.g., "BELONGS_TO")
   - Click "Load Graph"
3. **Custom Size**: Adjust "Max Nodes" slider (10-200)

### Searching
1. **Text Search**:
   - Enter search term in search box
   - Click "Search" button
   - Results appear below search box
   - Click result to focus on node

2. **Type-Specific Search**:
   - Select Node Type filter
   - Enter search term
   - Only searches within that node type

### Exploring Relationships
1. **View Node Details**:
   - Click any node in graph
   - Details appear in right panel
   - See all properties and values

2. **Expand Neighbors**:
   - Double-click a node, OR
   - Click node, then click "Expand Neighbors" button
   - Connected nodes and relationships are added to graph

3. **View Relationship Details**:
   - Click any edge/arrow in graph
   - Relationship type and properties shown
   - Source and target node IDs displayed

### Navigation
- **Pan**: Click and drag on empty space
- **Zoom**: Mouse scroll wheel
- **Reset View**: Click "Load Graph" to reload
- **Focus**: Click search result or use manual focus

## Node Types & Colors

| Node Type | Color | Description |
|-----------|-------|-------------|
| GoodWork | ?? Red | Volunteer opportunities/events |
| Organization | ?? Teal | Nonprofit organizations |
| User | ?? Blue | Registered users |
| Category | ?? Orange | Event categories |
| Location | ?? Green | Physical locations |
| Contact | ?? Yellow | Contact information |
| Skill | ?? Purple | Skills/abilities |
| Tag | ?? Light Blue | Tags/labels |

## Use Cases

### 1. **Data Quality Verification**
- Visually inspect relationships
- Find orphaned nodes (no connections)
- Verify data consistency
- Check relationship directions

### 2. **Database Structure Understanding**
- See how different node types connect
- Understand relationship patterns
- Identify hub nodes (highly connected)
- Discover data clusters

### 3. **Debugging**
- Trace relationship paths
- Find missing connections
- Verify organization ? event links
- Check user ? goodwork relationships

### 4. **Data Exploration**
- Browse related entities
- Follow relationship chains
- Discover connected data
- Navigate graph structure

### 5. **Testing**
- Verify test data creation
- Check relationship integrity
- Validate data import
- Explore sample data

## Technical Details

### Architecture
```
Graph Explorer Components:
??? GraphExplorerService.cs     - Neo4j queries and data retrieval
??? GraphExplorer.razor          - UI and interaction handling  
??? graph-explorer.js            - vis-network visualization
??? vis-network library          - Graph rendering engine
```

### Data Flow
1. **Load Request** ? GraphExplorerService queries Neo4j
2. **Data Transform** ? Nodes/Edges mapped to visualization format
3. **JSON Serialization** ? Data sent to JavaScript
4. **vis-network Render** ? Graph drawn on canvas
5. **User Interaction** ? Events sent back to Blazor
6. **Details Fetch** ? Additional queries for node/edge details

### Performance Considerations
- **Pagination**: Default limit of 50 nodes
- **Lazy Loading**: Expand neighbors on-demand
- **Physics Optimization**: Physics disabled after initial layout
- **Memory Management**: Clear old data when loading new sample

### API Methods

**GraphExplorerService**:
```csharp
// Get database statistics
GetGraphStatsAsync() ? GraphStats

// Load graph sample
GetGraphSampleAsync(limit, nodeType, relType) ? GraphData

// Get node neighbors
GetNodeNeighborsAsync(nodeId, depth) ? GraphData

// Search nodes
SearchNodesAsync(label, searchText, limit) ? List<GraphNode>

// Get node details
GetNodeDetailsAsync(nodeId) ? GraphNode

// Get relationship details
GetRelationshipDetailsAsync(relationshipId) ? GraphEdge
```

**JavaScript Functions**:
```javascript
// Initialize graph canvas
initializeGraph()

// Render new graph data
renderGraph(nodesJson, edgesJson, dotNetRef)

// Add nodes to existing graph
addGraphData(nodesJson, edgesJson)

// Focus and highlight node
focusNode(nodeId)

// Clear all graph data
clearGraph()
```

## Keyboard Shortcuts
- **Arrow Keys**: Pan view
- **+/-**: Zoom in/out
- **Space + Drag**: Pan view
- **Double-Click**: Expand node neighbors
- **Escape**: Deselect node

## Limitations
1. **Max Nodes**: Recommended limit of 200 nodes for performance
2. **Depth**: Neighbor expansion limited to 1 level per click
3. **No Editing**: Read-only visualization (cannot modify data)
4. **Dev Only**: Only available in development environment

## Troubleshooting

**Graph Not Loading**
- Check browser console for errors
- Verify Neo4j connection in appsettings.json
- Ensure vis-network library loaded (check Network tab)

**Slow Performance**
- Reduce "Max Nodes" slider value
- Disable physics after initial layout (automatic)
- Clear graph and reload with filters

**Nodes Overlapping**
- Wait for physics stabilization
- Double-click to expand and reposition
- Manually drag nodes to separate

**Search No Results**
- Check node type filter (might be filtering out results)
- Try broader search term
- Verify data exists in database

## Future Enhancements
- [ ] Shortest path finder between two nodes
- [ ] Cluster analysis and grouping
- [ ] Export graph as image/SVG
- [ ] Cypher query builder
- [ ] Timeline view for temporal data
- [ ] Hierarchical layout options
- [ ] Node/edge editing capabilities
- [ ] Custom color schemes
- [ ] Save/load graph layouts
- [ ] Collaborative graph annotations

## Related Documentation
- [Neo4j Graph Database](https://neo4j.com/docs/)
- [vis-network Documentation](https://visjs.github.io/vis-network/docs/network/)
- [Test Data Management](./TEST_DATA_MANAGEMENT.md)
- [Organization Implementation](./ORGANIZATIONS.md)

## Security Notes
- **Authorization Required**: Must be authenticated to access
- **Development Only**: Not available in production
- **Read-Only**: Cannot modify database through this interface
- **No Sensitive Data Export**: Properties displayed only, not exported

---

**Access URL**: `/admin/graph-explorer`  
**Menu**: Admin ? Graph Explorer (Dev only)  
**Author**: Charity Rabbit Development Team  
**Version**: 1.0

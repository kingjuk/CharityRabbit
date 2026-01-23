# Search Enhancements - Pagination & Fuzzy Search

## Overview
Enhanced the `SearchGoodWorksAsync` method in `Neo4jService` to support pagination and fuzzy name search capabilities.

## New Features

### 1. Fuzzy Name Search ??
- **Field**: `SearchText` in `GoodWorksSearchCriteria`
- **Functionality**: Case-insensitive partial matching on both `name` and `description` fields
- **Implementation**: Uses Neo4j's `toLower()` and `CONTAINS` operators
- **Example**: Searching for "food" will match "Food Bank Event", "Community Food Drive", "food distribution"

```csharp
var criteria = new GoodWorksSearchCriteria
{
    SearchText = "food",
    Page = 1,
    PageSize = 20
};
var results = await _neo4jService.SearchGoodWorksAsync(criteria);
```

### 2. Skills Filtering ??
- **Field**: `RequiredSkills` in `GoodWorksSearchCriteria`
- **Functionality**: Filter events that require ALL specified skills
- **Implementation**: Uses Neo4j's `ALL` predicate with `:REQUIRES_SKILL` relationships
- **Example**: Find events that require both "Carpentry" AND "Painting"

```csharp
var criteria = new GoodWorksSearchCriteria
{
    RequiredSkills = new List<string> { "Carpentry", "Painting" }
};
```

### 3. Pagination Support ??
- **Fields**: 
  - `Page` (default: 1) - Current page number (1-indexed)
  - `PageSize` (default: 50) - Number of results per page
- **Implementation**: Uses Neo4j's `SKIP` and `LIMIT` clauses
- **Benefits**:
  - Improved performance for large result sets
  - Better user experience with manageable result sizes
  - Reduced memory consumption

```csharp
// Get page 2 with 25 results per page
var criteria = new GoodWorksSearchCriteria
{
    Category = "Environmental",
    Page = 2,
    PageSize = 25
};
```

### 4. Result Count Method ??
- **Method**: `CountSearchResultsAsync(GoodWorksSearchCriteria criteria)`
- **Purpose**: Get total count of results matching criteria (without pagination)
- **Use Case**: Display "Showing 26-50 of 237 results"

```csharp
var totalCount = await _neo4jService.CountSearchResultsAsync(criteria);
var totalPages = (int)Math.Ceiling((double)totalCount / criteria.PageSize);
```

## Query Optimizations

### Enhanced Search Query
The search now:
1. Collects tags and skills in a single pass
2. Counts sign-ups efficiently
3. Properly handles pagination with `SKIP` and `LIMIT`
4. Maintains proper ordering (by `startTime ASC`)

### Example Query Structure
```cypher
MATCH (g:GoodWork)
WHERE toLower(g.name) CONTAINS toLower($searchText)
  AND g.status = 'Active'
  AND EXISTS((g)-[:BELONGS_TO]->(:Category {name: $category}))
OPTIONAL MATCH (g)-[:REQUIRES_SKILL]->(s:Skill)
WITH g, collect(DISTINCT s.name) AS skills
RETURN id(g) AS Id, g, skills
ORDER BY g.startTime ASC
SKIP $skip
LIMIT $limit
```

## Updated Model

### GoodWorksSearchCriteria Properties
```csharp
public class GoodWorksSearchCriteria
{
    // Existing properties
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public List<string>? Tags { get; set; }
    public double? CenterLatitude { get; set; }
    public double? CenterLongitude { get; set; }
    public double? RadiusMiles { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public string? EffortLevel { get; set; }
    public bool? IsVirtual { get; set; }
    public bool? IsAccessible { get; set; }
    public bool? FamilyFriendly { get; set; }
    public bool? HasAvailableSpots { get; set; }
    
    // NEW: Search enhancements
    public string? SearchText { get; set; }          // Fuzzy name/description search
    public List<string>? RequiredSkills { get; set; } // Skills filtering
    public int Page { get; set; } = 1;               // Current page (1-indexed)
    public int PageSize { get; set; } = 50;          // Results per page
}
```

## Usage Examples

### Example 1: Basic Fuzzy Search
```csharp
// Search for "shelter" in name or description
var results = await _neo4jService.SearchGoodWorksAsync(new GoodWorksSearchCriteria
{
    SearchText = "shelter"
});
```

### Example 2: Paginated Search with Filters
```csharp
// Page 3 of family-friendly food bank events
var criteria = new GoodWorksSearchCriteria
{
    Category = "Food Bank",
    FamilyFriendly = true,
    Page = 3,
    PageSize = 20
};

var results = await _neo4jService.SearchGoodWorksAsync(criteria);
var totalCount = await _neo4jService.CountSearchResultsAsync(criteria);
```

### Example 3: Skills-Based Search
```csharp
// Find carpentry events in the next 30 days
var criteria = new GoodWorksSearchCriteria
{
    RequiredSkills = new List<string> { "Carpentry" },
    StartDateFrom = DateTime.Today,
    StartDateTo = DateTime.Today.AddDays(30),
    Page = 1,
    PageSize = 25
};
```

### Example 4: Combined Search
```csharp
// Complex search: Virtual events about "coding" requiring Python skills
var criteria = new GoodWorksSearchCriteria
{
    SearchText = "coding",
    IsVirtual = true,
    RequiredSkills = new List<string> { "Python" },
    Page = 1,
    PageSize = 10
};

var results = await _neo4jService.SearchGoodWorksAsync(criteria);
var totalCount = await _neo4jService.CountSearchResultsAsync(criteria);

Console.WriteLine($"Found {totalCount} results");
Console.WriteLine($"Showing page {criteria.Page} of {Math.Ceiling((double)totalCount / criteria.PageSize)}");
```

## Performance Considerations

1. **Indexing**: Consider adding indexes on frequently searched fields:
   ```cypher
   CREATE INDEX good_work_name_idx FOR (g:GoodWork) ON (g.name);
   CREATE INDEX good_work_status_idx FOR (g:GoodWork) ON (g.status);
   CREATE INDEX good_work_start_time_idx FOR (g:GoodWork) ON (g.startTime);
   ```

2. **Page Size**: Default of 50 provides good balance between:
   - Network transfer size
   - User experience (not overwhelming)
   - Database query performance

3. **Count Queries**: The `CountSearchResultsAsync` method uses the same filters as search but only returns the count, making it efficient for pagination UI.

## Migration Notes

### Existing Code Compatibility
- Existing searches without pagination will work as before
- Default `Page = 1` and `PageSize = 50` maintain reasonable behavior
- All existing filters continue to work unchanged

### Recommended UI Updates
1. Add search textbox for fuzzy name search
2. Add pagination controls (Previous/Next, Page numbers)
3. Display "Showing X-Y of Z results"
4. Show "No results found" when count = 0

## Benefits

? **Better Performance**: Pagination reduces query load and memory usage
? **Better UX**: Users can search by name/keywords
? **More Precise Filtering**: Skills-based matching for better volunteer/event fit
? **Scalability**: Handles large databases efficiently
? **Flexibility**: All search criteria can be combined

## Next Steps

Consider adding:
- Full-text search indexes for even faster fuzzy search
- Sort options (by date, name, popularity)
- Search result relevance scoring
- Autocomplete for search suggestions
- Search history/saved searches

using CharityRabbit.Models;
using System.Text.Json;
using Neo4j.Driver;

namespace CharityRabbit.Data;

public class TestDataService
{
    private readonly Neo4jService _neo4jService;
    private readonly OrganizationService _organizationService;
    private readonly IWebHostEnvironment _environment;
    private const string TEST_DATA_MARKER = "TEST_DATA";

    public TestDataService(Neo4jService neo4jService, OrganizationService organizationService, IWebHostEnvironment environment)
    {
        _neo4jService = neo4jService;
        _organizationService = organizationService;
        _environment = environment;
    }

    public async Task<int> ImportTestDataAsync(string? userId = null)
    {
        var jsonPath = Path.Combine(_environment.ContentRootPath, "Data", "TestData", "good-works-test-data.json");
        
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Test data file not found at: {jsonPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var testData = JsonSerializer.Deserialize<List<GoodWorksModel>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (testData == null || !testData.Any())
        {
            throw new InvalidOperationException("No test data found in JSON file");
        }

        int importedCount = 0;

        // First, create test organizations
        var organizations = await CreateTestOrganizationsAsync(userId ?? "test-user");
        
        // Assign events to organizations (80% of events) and keep 20% as individual posts
        var orgEventCount = 0;
        var individualEventCount = 0;
        
        foreach (var goodWork in testData)
        {
            try
            {
                // Mark as test data
                goodWork.Tags ??= new List<string>();
                if (!goodWork.Tags.Contains(TEST_DATA_MARKER))
                {
                    goodWork.Tags.Add(TEST_DATA_MARKER);
                }

                // Set created date if not specified
                if (goodWork.CreatedDate == default)
                {
                    goodWork.CreatedDate = DateTime.UtcNow;
                }

                // Set status if not specified
                if (string.IsNullOrEmpty(goodWork.Status))
                {
                    goodWork.Status = "Active";
                }

                // Assign to organization based on category (80% org, 20% individual)
                if (orgEventCount < testData.Count * 0.8)
                {
                    var org = GetOrganizationForCategory(goodWork.Category ?? "Community Service", organizations);
                    if (org != null)
                    {
                        goodWork.IsOrganizationPost = true;
                        goodWork.OrganizationId = org.Id;
                        goodWork.OrganizationSlug = org.Slug;
                        goodWork.OrganizationName = org.Name;
                        goodWork.CreatedBy = userId ?? "test-user";
                        orgEventCount++;
                    }
                }
                else
                {
                    goodWork.IsOrganizationPost = false;
                    goodWork.CreatedBy = userId ?? "test-user";
                    individualEventCount++;
                }

                await _neo4jService.CreateGoodWorkAsync(goodWork, goodWork.CreatedBy);
                
                // Create relationship to organization if it's an org post
                if (goodWork.IsOrganizationPost && goodWork.OrganizationId.HasValue)
                {
                    await CreateGoodWorkOrganizationRelationship(goodWork.Id!.Value, goodWork.OrganizationId.Value);
                }
                
                importedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing '{goodWork.Name}': {ex.Message}");
            }
        }

        Console.WriteLine($"Imported {importedCount} events: {orgEventCount} organization events, {individualEventCount} individual events");
        return importedCount;
    }

    private async Task<List<OrganizationModel>> CreateTestOrganizationsAsync(string userId)
    {
        var organizations = new List<OrganizationModel>();
        
        var testOrgs = new[]
        {
            new OrganizationModel
            {
                Name = "Huntsville Food Bank",
                Description = "Fighting hunger in our community through food distribution and education programs",
                Mission = "To eliminate hunger in Madison County by providing nutritious food and essential resources to those in need",
                OrganizationType = "Nonprofit",
                ContactEmail = "contact@huntsvillefoodbank.org",
                ContactPhone = "256-555-0100",
                Address = "100 Charity Street",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7304,
                Longitude = -86.5861,
                FocusAreas = new List<string> { "Food & Hunger", "Community Development" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Hope Homeless Shelter",
                Description = "Providing shelter, meals, and support services for individuals experiencing homelessness",
                Mission = "To offer a safe haven and pathway to self-sufficiency for our homeless neighbors",
                OrganizationType = "Nonprofit",
                ContactEmail = "info@hopeshelter.org",
                ContactPhone = "256-555-0101",
                Address = "250 Hope Avenue",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7281,
                Longitude = -86.5901,
                FocusAreas = new List<string> { "Housing & Homelessness", "Community Service" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Green Earth Initiative",
                Description = "Environmental conservation and sustainability education for a healthier planet",
                Mission = "To protect and restore our natural environment through community action and education",
                OrganizationType = "Nonprofit",
                ContactEmail = "contact@greenearthinitiative.org",
                ContactPhone = "256-555-0102",
                Address = "River Walk Park",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7465,
                Longitude = -86.5892,
                FocusAreas = new List<string> { "Environment", "Community Development" },
                Tags = new List<string> { TEST_DATA_MARKER },
                Website = "https://greenearthinitiative.org"
            },
            new OrganizationModel
            {
                Name = "Learning Together Foundation",
                Description = "Providing educational support and tutoring for students of all ages",
                Mission = "To ensure every child has access to quality education and reaches their full potential",
                OrganizationType = "Educational",
                ContactEmail = "help@learningtogether.org",
                ContactPhone = "256-555-0103",
                Address = "500 School Street",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7198,
                Longitude = -86.5843,
                FocusAreas = new List<string> { "Education & Literacy", "Youth & Children" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Community Health Network",
                Description = "Delivering healthcare services and wellness education to underserved communities",
                Mission = "To provide accessible, quality healthcare for all community members",
                OrganizationType = "Nonprofit",
                ContactEmail = "care@communityhealthnet.org",
                ContactPhone = "256-555-0104",
                Address = "300 Medical Plaza",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7256,
                Longitude = -86.5867,
                FocusAreas = new List<string> { "Healthcare", "Seniors" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Paws & Claws Animal Rescue",
                Description = "Rescuing, rehabilitating, and rehoming abandoned and abused animals",
                Mission = "To be a voice for animals in need and find loving homes for every pet",
                OrganizationType = "Nonprofit",
                ContactEmail = "adopt@pawsandclaws.org",
                ContactPhone = "256-555-0105",
                Address = "789 Pet Lane",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35802",
                Latitude = 34.7123,
                Longitude = -86.6012,
                FocusAreas = new List<string> { "Animal Welfare" },
                Tags = new List<string> { TEST_DATA_MARKER },
                Website = "https://pawsandclaws.org"
            },
            new OrganizationModel
            {
                Name = "Youth Empowerment Alliance",
                Description = "Mentoring and supporting young people to reach their full potential",
                Mission = "To empower youth through mentorship, education, and leadership development",
                OrganizationType = "Community",
                ContactEmail = "info@youthempower.org",
                ContactPhone = "256-555-0106",
                Address = "456 Hope Road",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7234,
                Longitude = -86.5978,
                FocusAreas = new List<string> { "Youth & Children", "Education & Literacy" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Golden Years Senior Services",
                Description = "Enhancing the lives of seniors through programs, activities, and support",
                Mission = "To ensure our seniors live with dignity, purpose, and joy",
                OrganizationType = "Nonprofit",
                ContactEmail = "care@goldenyears.org",
                ContactPhone = "256-555-0107",
                Address = "555 Elder Way",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7156,
                Longitude = -86.5834,
                FocusAreas = new List<string> { "Seniors", "Healthcare" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Emergency Response Team",
                Description = "Rapid response and disaster relief services for communities in crisis",
                Mission = "To provide immediate assistance and long-term recovery support during emergencies",
                OrganizationType = "Nonprofit",
                ContactEmail = "response@ertservices.org",
                ContactPhone = "256-555-0108",
                Address = "200 Main Street",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7289,
                Longitude = -86.5912,
                FocusAreas = new List<string> { "Disaster Relief", "Community Service" },
                Tags = new List<string> { TEST_DATA_MARKER }
            },
            new OrganizationModel
            {
                Name = "Neighborhood Unity Project",
                Description = "Building stronger communities through collaboration and shared resources",
                Mission = "To foster community connections and improve quality of life for all residents",
                OrganizationType = "Community",
                ContactEmail = "connect@neighborhoodunity.org",
                ContactPhone = "256-555-0109",
                Address = "450 Market Street",
                City = "Huntsville",
                State = "Alabama",
                Country = "United States",
                ZipCode = "35801",
                Latitude = 34.7334,
                Longitude = -86.5712,
                FocusAreas = new List<string> { "Community Development", "Community Service" },
                Tags = new List<string> { TEST_DATA_MARKER }
            }
        };

        foreach (var org in testOrgs)
        {
            try
            {
                var created = await _organizationService.CreateOrganizationAsync(org, userId);
                organizations.Add(created);
                Console.WriteLine($"Created test organization: {created.Name} (ID: {created.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating test organization '{org.Name}': {ex.Message}");
            }
        }

        return organizations;
    }

    private OrganizationModel? GetOrganizationForCategory(string category, List<OrganizationModel> organizations)
    {
        // Map categories to organizations
        var categoryMapping = new Dictionary<string, string>
        {
            { "Food Bank", "Huntsville Food Bank" },
            { "Homeless Shelter", "Hope Homeless Shelter" },
            { "Environmental", "Green Earth Initiative" },
            { "Education", "Learning Together Foundation" },
            { "Healthcare", "Community Health Network" },
            { "Animal Welfare", "Paws & Claws Animal Rescue" },
            { "Youth Programs", "Youth Empowerment Alliance" },
            { "Senior Care", "Golden Years Senior Services" },
            { "Disaster Relief", "Emergency Response Team" },
            { "Community Service", "Neighborhood Unity Project" }
        };

        if (categoryMapping.TryGetValue(category, out var orgName))
        {
            return organizations.FirstOrDefault(o => o.Name == orgName);
        }

        // Default to first organization if no mapping found
        return organizations.FirstOrDefault();
    }

    private async Task CreateGoodWorkOrganizationRelationship(long goodWorkId, long organizationId)
    {
        // This creates the POSTED_BY relationship in Neo4j
        var query = @"
            MATCH (g:GoodWork), (o:Organization)
            WHERE id(g) = $goodWorkId AND id(o) = $organizationId
            MERGE (g)-[:POSTED_BY]->(o)";

        await using var session = _neo4jService.GetSession();
        await session.ExecuteWriteAsync(async tx =>
        {
            await tx.RunAsync(query, new { goodWorkId, organizationId });
        });
    }

    public async Task<int> DeleteAllTestDataAsync()
    {
        // Delete test good works
        var deletedGoodWorks = await _neo4jService.DeleteTestDataAsync(TEST_DATA_MARKER);
        
        // Delete test organizations
        var deletedOrgs = await DeleteTestOrganizationsAsync();
        
        Console.WriteLine($"Deleted {deletedGoodWorks} test good works and {deletedOrgs} test organizations");
        return deletedGoodWorks + deletedOrgs;
    }

    private async Task<int> DeleteTestOrganizationsAsync()
    {
        // Delete all organizations tagged with TEST_DATA
        var query = @"
            MATCH (o:Organization)
            WHERE $testMarker IN o.tags
            OPTIONAL MATCH (o)-[r]-()
            DELETE r, o
            RETURN count(DISTINCT o) as deletedCount";

        await using var session = _neo4jService.GetSession();
        var result = await session.ExecuteWriteAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { testMarker = TEST_DATA_MARKER });
            var record = await cursor.SingleAsync();
            return record["deletedCount"].As<int>();
        });

        return result;
    }

    public async Task<List<GoodWorksModel>> GetTestDataPreviewAsync()
    {
        var jsonPath = Path.Combine(_environment.ContentRootPath, "Data", "TestData", "good-works-test-data.json");
        
        if (!File.Exists(jsonPath))
        {
            return new List<GoodWorksModel>();
        }

        var jsonContent = await File.ReadAllTextAsync(jsonPath);
        var testData = JsonSerializer.Deserialize<List<GoodWorksModel>>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return testData ?? new List<GoodWorksModel>();
    }

    public async Task<int> GetTestDataCountInDatabaseAsync()
    {
        var goodWorksCount = await _neo4jService.CountTestDataAsync(TEST_DATA_MARKER);
        var orgsCount = await CountTestOrganizationsAsync();
        return goodWorksCount + orgsCount;
    }

    private async Task<int> CountTestOrganizationsAsync()
    {
        var query = @"
            MATCH (o:Organization)
            WHERE $testMarker IN o.tags
            RETURN count(o) as count";

        await using var session = _neo4jService.GetSession();
        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { testMarker = TEST_DATA_MARKER });
            var record = await cursor.SingleAsync();
            return record["count"].As<int>();
        });

        return result;
    }
}

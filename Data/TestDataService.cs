using CharityRabbit.Models;
using System.Text.Json;

namespace CharityRabbit.Data;

public class TestDataService
{
    private readonly Neo4jService _neo4jService;
    private readonly IWebHostEnvironment _environment;
    private const string TEST_DATA_MARKER = "TEST_DATA";

    public TestDataService(Neo4jService neo4jService, IWebHostEnvironment environment)
    {
        _neo4jService = neo4jService;
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

                await _neo4jService.CreateGoodWorkAsync(goodWork, userId);
                importedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing '{goodWork.Name}': {ex.Message}");
                // Continue with next item even if one fails
            }
        }

        return importedCount;
    }

    public async Task<int> DeleteAllTestDataAsync()
    {
        var deletedCount = await _neo4jService.DeleteTestDataAsync(TEST_DATA_MARKER);
        return deletedCount;
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
        return await _neo4jService.CountTestDataAsync(TEST_DATA_MARKER);
    }
}

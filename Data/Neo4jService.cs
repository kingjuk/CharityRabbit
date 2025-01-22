using CharityRabbit.Data;
using CharityRabbit.Models;
using MLS.Api.Services;
using Neo4j.Driver;

public class Neo4jService : IDisposable
{
    private readonly IDriver _driver;
    private readonly GeocodingService _locationServices;

    public Neo4jService(IDriver driver, GeocodingService locationServices)
    {
        _driver = driver;
        _locationServices = locationServices;
    }
    public Neo4jService(string uri, string username, string password, GeocodingService locationServices)
    {
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
        _locationServices = locationServices;
    }

    public async Task CreateGoodWorkAsync(GoodWorksModel goodWork)
    {
        //var (lat,lon) = await _locationServices.GetCoordinatesAsync(goodWork.Address);
        var (city, state, country, zip) = await _locationServices.GetLocationDetailsAsync(goodWork.Latitude, goodWork.Longitude);

        var query = @"
            CREATE (g:GoodWork { 
                name: $name, 
                description: $description, 
                latitude: $latitude, 
                longitude: $longitude,
                startTime: datetime($startTime),
                endTime: datetime($endTime),
                effortLevel: $effortLevel,
                isAccessible: $isAccessible,
                estimatedDuration: $estimatedDuration,
                isVirtual: $isVirtual
            })
            WITH g
            MERGE (c:Contact { email: $contactEmail })
            ON CREATE SET 
                c.name = $contactName, 
                c.phone = $contactPhone
            MERGE (g)-[:HAS_CONTACT]->(c)
            MERGE (cat:Category { name: $category })
            MERGE (g)-[:BELONGS_TO]->(cat)
            MERGE (l:Location { city: $city, state: $state, country: $country, zip: $zip })
            MERGE (g)-[:LOCATED_IN]->(l)
            RETURN g, c, cat, l";

        using var session = _driver.AsyncSession();

        await session.RunAsync(query, new
        {
            name = goodWork.Name,
            description = goodWork.Description ?? string.Empty,
            latitude = goodWork.Latitude,
            longitude = goodWork.Longitude,
            startTime = goodWork.StartTime?.ToString("o"),
            endTime = goodWork.EndTime?.ToString("o"),
            effortLevel = goodWork.EffortLevel ?? "Moderate",
            isAccessible = goodWork.IsAccessible,
            estimatedDuration = goodWork.EstimatedDuration?.TotalMinutes ?? 0,
            isVirtual = goodWork.IsVirtual,
            contactName = goodWork.ContactName ?? string.Empty,
            contactEmail = goodWork.ContactEmail ?? string.Empty,
            contactPhone = goodWork.ContactPhone ?? string.Empty,
            category = goodWork.Category ?? string.Empty,
            city = city ?? string.Empty,
            state = state ?? string.Empty,
            country = country ?? string.Empty,
            zip = zip ?? string.Empty
        });
    }

    public async Task<List<GoodWorksModel>> GetGoodWorksByCategoryAsync(string category)
    {
        var query = @"
            MATCH (g:GoodWork)-[:BELONGS_TO]->(c:Category {name: $category})
            RETURN g.name AS name, g.description AS description, 
                   g.latitude AS latitude, g.longitude AS longitude";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { category });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(new GoodWorksModel
            {
                Name = record["name"].As<string>(),
                Description = record["description"].As<string>(),
                Latitude = record["latitude"].As<double>(),
                Longitude = record["longitude"].As<double>()
            });
        });

        return goodWorks;
    }

    public async Task<List<GoodWorksModel>> GetGoodWorksByZipAsync(string zip)
    {
        var query = @"
            MATCH (g:GoodWork)-[:LOCATED_IN]->(l:Location {zip: $zip})
            RETURN g.name AS name, g.description AS description, 
                   g.latitude AS latitude, g.longitude AS longitude";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { zip });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(new GoodWorksModel
            {
                Name = record["name"].As<string>(),
                Description = record["description"].As<string>(),
                Latitude = record["latitude"].As<double>(),
                Longitude = record["longitude"].As<double>()
            });
        });

        return goodWorks;
    }

    public async Task<List<GoodWorksModel>> GetGoodWorksByLocationAsync(string city, string state, string country)
    {
        var query = @"
            MATCH (g:GoodWork)-[:LOCATED_IN]->(l:Location {city: $city, state: $state, country: $country})
            RETURN g.name AS name, g.description AS description, 
                   g.latitude AS latitude, g.longitude AS longitude";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { city, state, country });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(new GoodWorksModel
            {
                Name = record["name"].As<string>(),
                Description = record["description"].As<string>(),
                Latitude = record["latitude"].As<double>(),
                Longitude = record["longitude"].As<double>()
            });
        });

        return goodWorks;
    }
    public async Task<List<GoodWorksModel>> GetGoodWorksInBoundsAsync(double minLat, double maxLat, double minLng, double maxLng)
    {
        var query = @"
            MATCH (g:GoodWork)
            WHERE g.latitude >= $minLat AND g.latitude <= $maxLat
                  AND g.longitude >= $minLng AND g.longitude <= $maxLng
            RETURN g.name AS name, g.description AS description,
                   g.latitude AS latitude, g.longitude AS longitude";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { minLat, maxLat, minLng, maxLng });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(new GoodWorksModel
            {
                Name = record["name"].As<string>(),
                Description = record["description"].As<string>(),
                Latitude = record["latitude"].As<double>(),
                Longitude = record["longitude"].As<double>()
            });
        });

        return goodWorks;
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}

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

    public async Task CreateGoodWorkAsync(GoodWorksModel goodWork, string? userId = null)
    {
        var (city, state, country, zip) = await _locationServices.GetLocationDetailsAsync(goodWork.Latitude, goodWork.Longitude);

        var query = @"
            CREATE (g:GoodWork { 
                name: $name, 
                description: $description,
                detailedDescription: $detailedDescription,
                latitude: $latitude, 
                longitude: $longitude,
                startTime: datetime($startTime),
                endTime: datetime($endTime),
                effortLevel: $effortLevel,
                isAccessible: $isAccessible,
                estimatedDuration: $estimatedDuration,
                isVirtual: $isVirtual,
                maxParticipants: $maxParticipants,
                currentParticipants: $currentParticipants,
                minimumAge: $minimumAge,
                familyFriendly: $familyFriendly,
                isRecurring: $isRecurring,
                recurrencePattern: $recurrencePattern,
                organizationName: $organizationName,
                organizationWebsite: $organizationWebsite,
                parkingAvailable: $parkingAvailable,
                publicTransitAccessible: $publicTransitAccessible,
                specialInstructions: $specialInstructions,
                impactDescription: $impactDescription,
                estimatedPeopleHelped: $estimatedPeopleHelped,
                status: $status,
                outdoorActivity: $outdoorActivity,
                weatherDependent: $weatherDependent,
                createdDate: datetime($createdDate),
                createdBy: $createdBy
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
            
            // Create subcategory if provided
            FOREACH (sc IN CASE WHEN $subCategory IS NOT NULL THEN [1] ELSE [] END |
                MERGE (subcat:SubCategory { name: $subCategory })
                MERGE (g)-[:HAS_SUBCATEGORY]->(subcat)
            )
            
            // Create tags
            FOREACH (tag IN $tags |
                MERGE (t:Tag { name: tag })
                MERGE (g)-[:TAGGED_WITH]->(t)
            )
            
            // Create skills
            FOREACH (skill IN $requiredSkills |
                MERGE (s:Skill { name: skill })
                MERGE (g)-[:REQUIRES_SKILL]->(s)
            )
            
            RETURN id(g) AS id";

        using var session = _driver.AsyncSession();

        var result = await session.RunAsync(query, new
        {
            name = goodWork.Name,
            description = goodWork.Description ?? string.Empty,
            detailedDescription = goodWork.DetailedDescription ?? string.Empty,
            latitude = goodWork.Latitude,
            longitude = goodWork.Longitude,
            startTime = goodWork.StartTime?.ToString("o"),
            endTime = goodWork.EndTime?.ToString("o"),
            effortLevel = goodWork.EffortLevel ?? "Moderate",
            isAccessible = goodWork.IsAccessible,
            estimatedDuration = goodWork.EstimatedDuration?.TotalMinutes ?? 0,
            isVirtual = goodWork.IsVirtual,
            maxParticipants = goodWork.MaxParticipants,
            currentParticipants = goodWork.CurrentParticipants,
            minimumAge = goodWork.MinimumAge,
            familyFriendly = goodWork.FamilyFriendly,
            isRecurring = goodWork.IsRecurring,
            recurrencePattern = goodWork.RecurrencePattern ?? string.Empty,
            organizationName = goodWork.OrganizationName ?? string.Empty,
            organizationWebsite = goodWork.OrganizationWebsite ?? string.Empty,
            parkingAvailable = goodWork.ParkingAvailable,
            publicTransitAccessible = goodWork.PublicTransitAccessible,
            specialInstructions = goodWork.SpecialInstructions ?? string.Empty,
            impactDescription = goodWork.ImpactDescription ?? string.Empty,
            estimatedPeopleHelped = goodWork.EstimatedPeopleHelped,
            status = goodWork.Status ?? "Active",
            outdoorActivity = goodWork.OutdoorActivity,
            weatherDependent = goodWork.WeatherDependent,
            createdDate = goodWork.CreatedDate.ToString("o"),
            createdBy = userId ?? string.Empty,
            contactName = goodWork.ContactName ?? string.Empty,
            contactEmail = goodWork.ContactEmail ?? string.Empty,
            contactPhone = goodWork.ContactPhone ?? string.Empty,
            category = goodWork.Category ?? string.Empty,
            subCategory = goodWork.SubCategory,
            tags = goodWork.Tags ?? new List<string>(),
            requiredSkills = goodWork.RequiredSkills ?? new List<string>(),
            city = city ?? string.Empty,
            state = state ?? string.Empty,
            country = country ?? string.Empty,
            zip = zip ?? string.Empty
        });
    }

    public async Task<GoodWorksModel?> GetGoodWorkByIdAsync(long id, string? userId = null)
    {
        var query = @"
            MATCH (g:GoodWork)
            WHERE id(g) = $id
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:HAS_SUBCATEGORY]->(subcat:SubCategory)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            OPTIONAL MATCH (g)-[:TAGGED_WITH]->(t:Tag)
            OPTIONAL MATCH (g)-[:REQUIRES_SKILL]->(s:Skill)
            OPTIONAL MATCH (g)<-[int:INTERESTED_IN]-(u:User)
            OPTIONAL MATCH (g)<-[signup:SIGNED_UP_FOR]-(u2:User)
            
            WITH g, c, cat, subcat, l, 
                 collect(DISTINCT t.name) AS tags,
                 collect(DISTINCT s.name) AS skills,
                 count(DISTINCT int) AS interestedCount,
                 count(DISTINCT signup) AS signedUpCount,
                 CASE WHEN $userId IS NOT NULL THEN 
                    EXISTS((g)<-[:INTERESTED_IN]-(:User {userId: $userId}))
                 ELSE false END AS userInterested,
                 CASE WHEN $userId IS NOT NULL THEN 
                    EXISTS((g)<-[:SIGNED_UP_FOR]-(:User {userId: $userId}))
                 ELSE false END AS userSignedUp
            
            RETURN id(g) AS Id,
                   g.name AS Name,
                   g.description AS Description,
                   g.detailedDescription AS DetailedDescription,
                   g.category AS Category,
                   g.latitude AS Latitude,
                   g.longitude AS Longitude,
                   g.address AS Address,
                   g.startTime AS StartTime,
                   g.endTime AS EndTime,
                   g.estimatedDuration AS EstimatedDuration,
                   g.effortLevel AS EffortLevel,
                   g.isAccessible AS IsAccessible,
                   g.isVirtual AS IsVirtual,
                   g.maxParticipants AS MaxParticipants,
                   g.currentParticipants AS CurrentParticipants,
                   g.minimumAge AS MinimumAge,
                   g.familyFriendly AS FamilyFriendly,
                   g.isRecurring AS IsRecurring,
                   g.recurrencePattern AS RecurrencePattern,
                   g.organizationName AS OrganizationName,
                   g.organizationWebsite AS OrganizationWebsite,
                   g.parkingAvailable AS ParkingAvailable,
                   g.publicTransitAccessible AS PublicTransitAccessible,
                   g.specialInstructions AS SpecialInstructions,
                   g.impactDescription AS ImpactDescription,
                   g.estimatedPeopleHelped AS EstimatedPeopleHelped,
                   g.status AS Status,
                   g.outdoorActivity AS OutdoorActivity,
                   g.weatherDependent AS WeatherDependent,
                   g.createdDate AS CreatedDate,
                   c.name AS ContactName,
                   c.email AS ContactEmail,
                   c.phone AS ContactPhone,
                   cat.name AS CategoryName,
                   subcat.name AS SubCategory,
                   l.city AS City,
                   l.state AS State,
                   l.country AS Country,
                   l.zip AS Zip,
                   tags, skills,
                   interestedCount, signedUpCount,
                   userInterested, userSignedUp";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { id, userId });

        var record = await result.SingleOrDefaultAsync();
        if (record == null) return null;

        return MapRecordToGoodWork(record);
    }

    public async Task<List<GoodWorksModel>> SearchGoodWorksAsync(GoodWorksSearchCriteria criteria, string? userId = null)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        // Build dynamic query based on criteria
        if (!string.IsNullOrEmpty(criteria.Category))
        {
            conditions.Add("EXISTS((g)-[:BELONGS_TO]->(:Category {name: $category}))");
            parameters["category"] = criteria.Category;
        }

        if (!string.IsNullOrEmpty(criteria.SubCategory))
        {
            conditions.Add("EXISTS((g)-[:HAS_SUBCATEGORY]->(:SubCategory {name: $subCategory}))");
            parameters["subCategory"] = criteria.SubCategory;
        }

        if (criteria.Tags != null && criteria.Tags.Any())
        {
            conditions.Add("ANY(tag IN $tags WHERE EXISTS((g)-[:TAGGED_WITH]->(:Tag {name: tag})))");
            parameters["tags"] = criteria.Tags;
        }

        if (criteria.CenterLatitude.HasValue && criteria.CenterLongitude.HasValue && criteria.RadiusMiles.HasValue)
        {
            // Convert miles to degrees (approximate)
            var latDelta = criteria.RadiusMiles.Value / 69.0;
            var lonDelta = criteria.RadiusMiles.Value / (69.0 * Math.Cos(criteria.CenterLatitude.Value * Math.PI / 180));
            
            conditions.Add(@"g.latitude >= $minLat AND g.latitude <= $maxLat AND 
                           g.longitude >= $minLng AND g.longitude <= $maxLng");
            parameters["minLat"] = criteria.CenterLatitude.Value - latDelta;
            parameters["maxLat"] = criteria.CenterLatitude.Value + latDelta;
            parameters["minLng"] = criteria.CenterLongitude.Value - lonDelta;
            parameters["maxLng"] = criteria.CenterLongitude.Value + lonDelta;
        }

        if (criteria.StartDateFrom.HasValue)
        {
            conditions.Add("g.startTime >= datetime($startFrom)");
            parameters["startFrom"] = criteria.StartDateFrom.Value.ToString("o");
        }

        if (criteria.StartDateTo.HasValue)
        {
            conditions.Add("g.startTime <= datetime($startTo)");
            parameters["startTo"] = criteria.StartDateTo.Value.ToString("o");
        }

        if (!string.IsNullOrEmpty(criteria.EffortLevel))
        {
            conditions.Add("g.effortLevel = $effortLevel");
            parameters["effortLevel"] = criteria.EffortLevel;
        }

        if (criteria.IsVirtual.HasValue)
        {
            conditions.Add("g.isVirtual = $isVirtual");
            parameters["isVirtual"] = criteria.IsVirtual.Value;
        }

        if (criteria.IsAccessible.HasValue)
        {
            conditions.Add("g.isAccessible = $isAccessible");
            parameters["isAccessible"] = criteria.IsAccessible.Value;
        }

        if (criteria.FamilyFriendly.HasValue)
        {
            conditions.Add("g.familyFriendly = $familyFriendly");
            parameters["familyFriendly"] = criteria.FamilyFriendly.Value;
        }

        if (criteria.HasAvailableSpots == true)
        {
            conditions.Add("(g.maxParticipants IS NULL OR g.currentParticipants < g.maxParticipants)");
        }

        conditions.Add("g.status = 'Active'");

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        var query = $@"
            MATCH (g:GoodWork)
            {whereClause}
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            RETURN id(g) AS Id, g, c, cat, l
            ORDER BY g.startTime ASC
            LIMIT 100";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, parameters);

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public async Task<List<GoodWorksModel>> GetSimilarGoodWorksAsync(long goodWorkId, string? userId = null, int limit = 10)
    {
        var query = @"
            MATCH (original:GoodWork)
            WHERE id(original) = $goodWorkId
            
            // Find similar works by category, tags, skills
            MATCH (similar:GoodWork)
            WHERE id(similar) <> $goodWorkId 
              AND similar.status = 'Active'
            OPTIONAL MATCH (original)-[:BELONGS_TO]->(cat:Category)<-[:BELONGS_TO]-(similar)
            OPTIONAL MATCH (original)-[:TAGGED_WITH]->(tag:Tag)<-[:TAGGED_WITH]-(similar)
            OPTIONAL MATCH (original)-[:REQUIRES_SKILL]->(skill:Skill)<-[:REQUIRES_SKILL]-(similar)
            OPTIONAL MATCH (original)-[:LOCATED_IN]->(loc:Location)<-[:LOCATED_IN]-(similar)
            
            WITH similar, 
                 count(DISTINCT cat) AS categoryMatch,
                 count(DISTINCT tag) AS tagMatch,
                 count(DISTINCT skill) AS skillMatch,
                 count(DISTINCT loc) AS locationMatch,
                 CASE WHEN original.effortLevel = similar.effortLevel THEN 1 ELSE 0 END AS effortMatch,
                 abs(duration.between(original.startTime, similar.startTime).days) AS daysDiff
            
            // Calculate similarity score
            WITH similar, 
                 (categoryMatch * 3 + tagMatch * 2 + skillMatch * 2 + locationMatch * 2 + effortMatch) AS similarityScore,
                 daysDiff
            WHERE similarityScore > 0
            
            OPTIONAL MATCH (similar)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (similar)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (similar)-[:LOCATED_IN]->(l:Location)
            
            RETURN id(similar) AS Id, similar AS g, c, cat, l, similarityScore
            ORDER BY similarityScore DESC, daysDiff ASC
            LIMIT $limit";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { goodWorkId, limit });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public async Task MarkUserInterestedAsync(string userId, long goodWorkId, bool interested)
    {
        var query = interested
            ? @"MERGE (u:User {userId: $userId})
                WITH u
                MATCH (g:GoodWork) WHERE id(g) = $goodWorkId
                MERGE (u)-[r:INTERESTED_IN]->(g)
                ON CREATE SET r.timestamp = datetime()
                RETURN count(r) AS count"
            : @"MATCH (u:User {userId: $userId})-[r:INTERESTED_IN]->(g:GoodWork)
                WHERE id(g) = $goodWorkId
                DELETE r
                RETURN count(r) AS count";

        using var session = _driver.AsyncSession();
        await session.RunAsync(query, new { userId, goodWorkId });
    }

    public async Task SignUpUserAsync(string userId, long goodWorkId, bool signUp)
    {
        var query = signUp
            ? @"MERGE (u:User {userId: $userId})
                WITH u
                MATCH (g:GoodWork) WHERE id(g) = $goodWorkId
                MERGE (u)-[r:SIGNED_UP_FOR]->(g)
                ON CREATE SET r.timestamp = datetime()
                WITH g
                SET g.currentParticipants = g.currentParticipants + 1
                RETURN g.currentParticipants AS count"
            : @"MATCH (u:User {userId: $userId})-[r:SIGNED_UP_FOR]->(g:GoodWork)
                WHERE id(g) = $goodWorkId
                DELETE r
                WITH g
                SET g.currentParticipants = CASE WHEN g.currentParticipants > 0 
                    THEN g.currentParticipants - 1 ELSE 0 END
                RETURN g.currentParticipants AS count";

        using var session = _driver.AsyncSession();
        await session.RunAsync(query, new { userId, goodWorkId });
    }

    public async Task<List<GoodWorksModel>> GetUserInterestedGoodWorksAsync(string userId)
    {
        var query = @"
            MATCH (u:User {userId: $userId})-[:INTERESTED_IN]->(g:GoodWork)
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            RETURN id(g) AS Id, g, c, cat, l
            ORDER BY g.startTime ASC";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { userId });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public async Task<List<GoodWorksModel>> GetUserSignedUpGoodWorksAsync(string userId)
    {
        var query = @"
            MATCH (u:User {userId: $userId})-[:SIGNED_UP_FOR]->(g:GoodWork)
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            RETURN id(g) AS Id, g, c, cat, l
            ORDER BY g.startTime ASC";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { userId });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public async Task<List<GoodWorksModel>> GetUserCreatedGoodWorksAsync(string userId)
    {
        var query = @"
            MATCH (g:GoodWork)
            WHERE g.createdBy = $userId
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            RETURN id(g) AS Id, g, c, cat, l
            ORDER BY g.createdDate DESC";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { userId });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public async Task UpdateGoodWorkAsync(long id, GoodWorksModel goodWork, string userId)
    {
        var (city, state, country, zip) = await _locationServices.GetLocationDetailsAsync(goodWork.Latitude, goodWork.Longitude);

        var query = @"
            MATCH (g:GoodWork)
            WHERE id(g) = $id AND g.createdBy = $userId
            SET g.name = $name,
                g.description = $description,
                g.detailedDescription = $detailedDescription,
                g.latitude = $latitude,
                g.longitude = $longitude,
                g.startTime = datetime($startTime),
                g.endTime = datetime($endTime),
                g.effortLevel = $effortLevel,
                g.isAccessible = $isAccessible,
                g.estimatedDuration = $estimatedDuration,
                g.isVirtual = $isVirtual,
                g.maxParticipants = $maxParticipants,
                g.minimumAge = $minimumAge,
                g.familyFriendly = $familyFriendly,
                g.isRecurring = $isRecurring,
                g.recurrencePattern = $recurrencePattern,
                g.organizationName = $organizationName,
                g.organizationWebsite = $organizationWebsite,
                g.parkingAvailable = $parkingAvailable,
                g.publicTransitAccessible = $publicTransitAccessible,
                g.specialInstructions = $specialInstructions,
                g.impactDescription = $impactDescription,
                g.estimatedPeopleHelped = $estimatedPeopleHelped,
                g.status = $status,
                g.outdoorActivity = $outdoorActivity,
                g.weatherDependent = $weatherDependent
            
            WITH g
            MERGE (c:Contact { email: $contactEmail })
            ON CREATE SET c.name = $contactName, c.phone = $contactPhone
            ON MATCH SET c.name = $contactName, c.phone = $contactPhone
            MERGE (g)-[:HAS_CONTACT]->(c)
            
            WITH g
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(oldCat:Category)
            DELETE oldCat
            MERGE (cat:Category { name: $category })
            MERGE (g)-[:BELONGS_TO]->(cat)
            
            WITH g
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(oldLoc:Location)
            DELETE oldLoc
            MERGE (l:Location { city: $city, state: $state, country: $country, zip: $zip })
            MERGE (g)-[:LOCATED_IN]->(l)
            
            RETURN id(g) AS id";

        using var session = _driver.AsyncSession();
        await session.RunAsync(query, new
        {
            id,
            userId,
            name = goodWork.Name,
            description = goodWork.Description ?? string.Empty,
            detailedDescription = goodWork.DetailedDescription ?? string.Empty,
            latitude = goodWork.Latitude,
            longitude = goodWork.Longitude,
            startTime = goodWork.StartTime?.ToString("o"),
            endTime = goodWork.EndTime?.ToString("o"),
            effortLevel = goodWork.EffortLevel ?? "Moderate",
            isAccessible = goodWork.IsAccessible,
            estimatedDuration = goodWork.EstimatedDuration?.TotalMinutes ?? 0,
            isVirtual = goodWork.IsVirtual,
            maxParticipants = goodWork.MaxParticipants,
            minimumAge = goodWork.MinimumAge,
            familyFriendly = goodWork.FamilyFriendly,
            isRecurring = goodWork.IsRecurring,
            recurrencePattern = goodWork.RecurrencePattern ?? string.Empty,
            organizationName = goodWork.OrganizationName ?? string.Empty,
            organizationWebsite = goodWork.OrganizationWebsite ?? string.Empty,
            parkingAvailable = goodWork.ParkingAvailable,
            publicTransitAccessible = goodWork.PublicTransitAccessible,
            specialInstructions = goodWork.SpecialInstructions ?? string.Empty,
            impactDescription = goodWork.ImpactDescription ?? string.Empty,
            estimatedPeopleHelped = goodWork.EstimatedPeopleHelped,
            status = goodWork.Status ?? "Active",
            outdoorActivity = goodWork.OutdoorActivity,
            weatherDependent = goodWork.WeatherDependent,
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

    public async Task DeleteGoodWorkAsync(long id, string userId)
    {
        var query = @"
            MATCH (g:GoodWork)
            WHERE id(g) = $id AND g.createdBy = $userId
            OPTIONAL MATCH (g)-[r]-()
            DELETE r, g";

        using var session = _driver.AsyncSession();
        await session.RunAsync(query, new { id, userId });
    }

    private GoodWorksModel MapRecordToGoodWork(IRecord record)
    {
        // Helper to safely get node from record
        INode? GetNode(string key)
        {
            if (record.ContainsKey(key))
            {
                var value = record[key];
                return value as INode;
            }
            return null;
        }

        // Helper to safely get property from node
        T? GetNodeProperty<T>(INode? node, string propertyName, T? defaultValue = default)
        {
            if (node == null) return defaultValue;
            if (!node.Properties.ContainsKey(propertyName)) return defaultValue;
            try
            {
                return node[propertyName].As<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        var gNode = GetNode("g");
        var cNode = GetNode("c");
        var catNode = GetNode("cat");
        var lNode = GetNode("l");

        var goodWork = new GoodWorksModel
        {
            Id = record.ContainsKey("Id") ? record["Id"].As<long?>() : null,
            Name = record.ContainsKey("Name") ? record["Name"].As<string>() : GetNodeProperty<string>(gNode, "name") ?? string.Empty,
            Description = record.ContainsKey("Description") ? record["Description"].As<string>() : GetNodeProperty<string>(gNode, "description") ?? string.Empty,
            DetailedDescription = record.ContainsKey("DetailedDescription") 
                ? record["DetailedDescription"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "detailedDescription"),
            Category = record.ContainsKey("CategoryName") 
                ? record["CategoryName"].As<string?>() ?? string.Empty
                : GetNodeProperty<string>(catNode, "name") ?? string.Empty,
            SubCategory = record.ContainsKey("SubCategory") ? record["SubCategory"].As<string?>() : null,
            Latitude = record.ContainsKey("Latitude") ? record["Latitude"].As<double>() : GetNodeProperty<double>(gNode, "latitude", 0),
            Longitude = record.ContainsKey("Longitude") ? record["Longitude"].As<double>() : GetNodeProperty<double>(gNode, "longitude", 0),
            Address = record.ContainsKey("Address") ? record["Address"].As<string?>() : GetNodeProperty<string?>(gNode, "address"),
            EffortLevel = record.ContainsKey("EffortLevel") 
                ? record["EffortLevel"].As<string>() 
                : GetNodeProperty<string>(gNode, "effortLevel") ?? "Moderate",
            IsAccessible = record.ContainsKey("IsAccessible") 
                ? record["IsAccessible"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "isAccessible") ?? false,
            IsVirtual = record.ContainsKey("IsVirtual") 
                ? record["IsVirtual"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "isVirtual") ?? false,
            MaxParticipants = record.ContainsKey("MaxParticipants") 
                ? record["MaxParticipants"].As<int?>() 
                : GetNodeProperty<int?>(gNode, "maxParticipants"),
            CurrentParticipants = record.ContainsKey("CurrentParticipants") 
                ? record["CurrentParticipants"].As<int?>() ?? 0
                : GetNodeProperty<int?>(gNode, "currentParticipants") ?? 0,
            MinimumAge = record.ContainsKey("MinimumAge") 
                ? record["MinimumAge"].As<int?>() 
                : GetNodeProperty<int?>(gNode, "minimumAge"),
            FamilyFriendly = record.ContainsKey("FamilyFriendly") 
                ? record["FamilyFriendly"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "familyFriendly") ?? false,
            IsRecurring = record.ContainsKey("IsRecurring") 
                ? record["IsRecurring"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "isRecurring") ?? false,
            RecurrencePattern = record.ContainsKey("RecurrencePattern") 
                ? record["RecurrencePattern"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "recurrencePattern"),
            OrganizationName = record.ContainsKey("OrganizationName") 
                ? record["OrganizationName"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "organizationName"),
            OrganizationWebsite = record.ContainsKey("OrganizationWebsite") 
                ? record["OrganizationWebsite"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "organizationWebsite"),
            ParkingAvailable = record.ContainsKey("ParkingAvailable") 
                ? record["ParkingAvailable"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "parkingAvailable") ?? false,
            PublicTransitAccessible = record.ContainsKey("PublicTransitAccessible") 
                ? record["PublicTransitAccessible"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "publicTransitAccessible") ?? false,
            SpecialInstructions = record.ContainsKey("SpecialInstructions") 
                ? record["SpecialInstructions"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "specialInstructions"),
            ImpactDescription = record.ContainsKey("ImpactDescription") 
                ? record["ImpactDescription"].As<string?>() 
                : GetNodeProperty<string?>(gNode, "impactDescription"),
            EstimatedPeopleHelped = record.ContainsKey("EstimatedPeopleHelped") 
                ? record["EstimatedPeopleHelped"].As<int?>() 
                : GetNodeProperty<int?>(gNode, "estimatedPeopleHelped"),
            Status = record.ContainsKey("Status") 
                ? record["Status"].As<string>() 
                : GetNodeProperty<string>(gNode, "status") ?? "Active",
            OutdoorActivity = record.ContainsKey("OutdoorActivity") 
                ? record["OutdoorActivity"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "outdoorActivity") ?? false,
            WeatherDependent = record.ContainsKey("WeatherDependent") 
                ? record["WeatherDependent"].As<bool?>() ?? false
                : GetNodeProperty<bool?>(gNode, "weatherDependent") ?? false,
            ContactName = record.ContainsKey("ContactName") 
                ? record["ContactName"].As<string?>() ?? string.Empty
                : GetNodeProperty<string>(cNode, "name") 
                    ?? GetNodeProperty<string>(gNode, "contactName") 
                    ?? string.Empty,
            ContactEmail = record.ContainsKey("ContactEmail")
                ? record["ContactEmail"].As<string?>() ?? string.Empty
                : GetNodeProperty<string>(cNode, "email") 
                    ?? GetNodeProperty<string>(gNode, "contactEmail") 
                    ?? string.Empty,
            ContactPhone = record.ContainsKey("ContactPhone")
                ? record["ContactPhone"].As<string?>() ?? string.Empty
                : GetNodeProperty<string?>(cNode, "phone") 
                    ?? GetNodeProperty<string?>(gNode, "contactPhone") 
                    ?? string.Empty
        };

        // Handle date/time fields
        if (record.ContainsKey("StartTime"))
        {
            goodWork.StartTime = record["StartTime"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime;
        }
        else if (gNode != null && gNode.Properties.ContainsKey("startTime"))
        {
            goodWork.StartTime = GetNodeProperty<ZonedDateTime?>(gNode, "startTime")?.ToDateTimeOffset().DateTime;
        }

        if (record.ContainsKey("EndTime"))
        {
            goodWork.EndTime = record["EndTime"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime;
        }
        else if (gNode != null && gNode.Properties.ContainsKey("endTime"))
        {
            goodWork.EndTime = GetNodeProperty<ZonedDateTime?>(gNode, "endTime")?.ToDateTimeOffset().DateTime;
        }

        if (record.ContainsKey("EstimatedDuration"))
        {
            var duration = record["EstimatedDuration"].As<double?>();
            if (duration.HasValue && duration.Value > 0)
            {
                goodWork.EstimatedDuration = TimeSpan.FromMinutes(duration.Value);
            }
        }
        else if (gNode != null && gNode.Properties.ContainsKey("estimatedDuration"))
        {
            var duration = GetNodeProperty<double?>(gNode, "estimatedDuration");
            if (duration.HasValue && duration.Value > 0)
            {
                goodWork.EstimatedDuration = TimeSpan.FromMinutes(duration.Value);
            }
        }

        if (record.ContainsKey("CreatedDate"))
        {
            goodWork.CreatedDate = record["CreatedDate"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime ?? DateTime.UtcNow;
        }
        else if (gNode != null && gNode.Properties.ContainsKey("createdDate"))
        {
            goodWork.CreatedDate = GetNodeProperty<ZonedDateTime?>(gNode, "createdDate")?.ToDateTimeOffset().DateTime ?? DateTime.UtcNow;
        }

        // Handle collections
        if (record.ContainsKey("tags"))
        {
            var tags = record["tags"].As<List<object>>();
            goodWork.Tags = tags?.Select(t => t.ToString() ?? string.Empty).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>();
        }

        if (record.ContainsKey("skills"))
        {
            var skills = record["skills"].As<List<object>>();
            goodWork.RequiredSkills = skills?.Select(s => s.ToString() ?? string.Empty).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();
        }

        // Handle user engagement counts
        if (record.ContainsKey("interestedCount"))
        {
            goodWork.InterestedCount = record["interestedCount"].As<int?>() ?? 0;
        }

        if (record.ContainsKey("signedUpCount"))
        {
            goodWork.SignedUpCount = record["signedUpCount"].As<int?>() ?? 0;
        }

        if (record.ContainsKey("userInterested"))
        {
            goodWork.IsUserInterested = record["userInterested"].As<bool?>() ?? false;
        }

        if (record.ContainsKey("userSignedUp"))
        {
            goodWork.IsUserSignedUp = record["userSignedUp"].As<bool?>() ?? false;
        }

        // Handle location if provided separately
        if (record.ContainsKey("City") && record.ContainsKey("State") && record.ContainsKey("Country"))
        {
            var city = record["City"].As<string?>();
            var state = record["State"].As<string?>();
            var country = record["Country"].As<string?>();
            var zip = record.ContainsKey("Zip") ? record["Zip"].As<string?>() : null;

            if (!string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(state))
            {
                goodWork.Address = $"{city}, {state}, {country}, {zip}".Trim(' ', ',');
            }
        }
        else if (lNode != null)
        {
            var city = GetNodeProperty<string?>(lNode, "city");
            var state = GetNodeProperty<string?>(lNode, "state");
            var country = GetNodeProperty<string?>(lNode, "country");
            var zip = GetNodeProperty<string?>(lNode, "zip");

            if (!string.IsNullOrEmpty(city) || !string.IsNullOrEmpty(state))
            {
                goodWork.Address = $"{city}, {state}, {country}, {zip}".Trim(' ', ',');
            }
        }

        return goodWork;
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
    
    public async Task<List<GoodWorksModel>> GetAllGoodWorksWithRelationshipsAsync()
    {
        var query = @"
        MATCH (g:GoodWork)-[r]-(n)
        RETURN id(g) AS Id, 
               g.name AS Name, 
               g.description AS Description, 
               g.category AS Category,
               g.latitude AS Latitude, 
               g.longitude AS Longitude, 
               g.startTime AS StartTime, 
               g.endTime AS EndTime, 
               g.effortLevel AS EffortLevel, 
               g.isAccessible AS IsAccessible, 
               g.estimatedDuration AS EstimatedDuration, 
               g.isVirtual AS IsVirtual, 
               g.address AS Address,
               n AS RelatedNode, 
               type(r) AS RelationshipType";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query);

        var goodWorksDictionary = new Dictionary<long, GoodWorksModel>();

        await result.ForEachAsync(record =>
        {
            var id = record["Id"].As<long>();

            if (!goodWorksDictionary.TryGetValue(id, out var goodWork))
            {
                goodWork = new GoodWorksModel
                {
                    Id = id,
                    Name = record["Name"].As<string>(),
                    Description = record["Description"].As<string>(),
                    Category = record["Category"].As<string>(),
                    Latitude = record["Latitude"].As<double>(),
                    Longitude = record["Longitude"].As<double>(),
                    Address = record["Address"].As<string>(),
                    StartTime = record["StartTime"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime,
                    EndTime = record["EndTime"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime,
                    EstimatedDuration = record["EstimatedDuration"].As<double?>() != null
                                        ? TimeSpan.FromMinutes(record["EstimatedDuration"].As<double>())
                                        : null,
                    EffortLevel = record["EffortLevel"].As<string>(),
                    IsAccessible = record["IsAccessible"].As<bool>(),
                    IsVirtual = record["IsVirtual"].As<bool>(),
                    ContactName = string.Empty,
                    ContactEmail = string.Empty,
                    ContactPhone = string.Empty
                };
                goodWorksDictionary[id] = goodWork;
            }

            // Handle relationships
            var relationshipType = record["RelationshipType"].As<string>();
            var relatedNode = record["RelatedNode"].As<INode>();

            switch (relationshipType)
            {
                case "HAS_CONTACT":
                    goodWork.ContactName = relatedNode["name"].As<string>();
                    goodWork.ContactEmail = relatedNode["email"].As<string>();
                    goodWork.ContactPhone = relatedNode["phone"].As<string>();
                    break;
                case "LOCATED_IN":
                    goodWork.Address = $"{relatedNode["city"].As<string>()}, {relatedNode["state"].As<string>()}, {relatedNode["country"].As<string>()}, {relatedNode["zip"].As<string>()}";
                    break;
                case "BELONGS_TO":
                    goodWork.Category = relatedNode["name"].As<string>();
                    break;
            }
        });

        return goodWorksDictionary.Values.ToList();
    }

    public async Task<List<GoodWorksModel>> GetGoodWorksInBoundsAsync(double minLat, double maxLat, double minLng, double maxLng)
    {
        var query = @"
            MATCH (g:GoodWork)
            WHERE g.latitude >= $minLat AND g.latitude <= $maxLat
                  AND g.longitude >= $minLng AND g.longitude <= $maxLng
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            RETURN id(g) AS Id, g, c, cat, l
            ORDER BY g.startTime ASC
            LIMIT 500";

        using var session = _driver.AsyncSession();
        var result = await session.RunAsync(query, new { minLat, maxLat, minLng, maxLng });

        var goodWorks = new List<GoodWorksModel>();
        await result.ForEachAsync(record =>
        {
            goodWorks.Add(MapRecordToGoodWork(record));
        });

        return goodWorks;
    }

    public void Dispose()
    {
        _driver?.Dispose();
    }
}

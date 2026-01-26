using CharityRabbit.Models;
using Neo4j.Driver;
using System.Text.RegularExpressions;

namespace CharityRabbit.Data;

public class OrganizationService
{
    private readonly IDriver _driver;

    public OrganizationService(IDriver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Generates a URL-friendly slug from organization name
    /// </summary>
    private string GenerateSlug(string name)
    {
        // Convert to lowercase and replace spaces with hyphens
        var slug = name.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", ""); // Remove special characters
        slug = Regex.Replace(slug, @"\s+", "-"); // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"-+", "-"); // Replace multiple hyphens with single
        return slug;
    }

    /// <summary>
    /// Check if slug is already in use
    /// </summary>
    public async Task<bool> IsSlugAvailableAsync(string slug, long? excludeOrgId = null)
    {
        await using var session = _driver.AsyncSession();
        
        var query = excludeOrgId.HasValue
            ? "MATCH (o:Organization {slug: $slug}) WHERE id(o) <> $excludeId RETURN count(o) as count"
            : "MATCH (o:Organization {slug: $slug}) RETURN count(o) as count";

        var result = await session.ExecuteReadAsync(async tx =>
        {
            var cursor = await tx.RunAsync(query, new { slug, excludeId = excludeOrgId });
            var record = await cursor.SingleAsync();
            return record["count"].As<int>();
        });

        return result == 0;
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    public async Task<OrganizationModel> CreateOrganizationAsync(OrganizationModel organization, string userId)
    {
        await using var session = _driver.AsyncSession();

        // Generate slug if not provided
        if (string.IsNullOrEmpty(organization.Slug))
        {
            organization.Slug = GenerateSlug(organization.Name);
            
            // Ensure uniqueness
            var baseSlug = organization.Slug;
            var counter = 1;
            while (!await IsSlugAvailableAsync(organization.Slug))
            {
                organization.Slug = $"{baseSlug}-{counter}";
                counter++;
            }
        }

        organization.CreatedBy = userId;
        organization.CreatedDate = DateTime.UtcNow;
        organization.Status = "Active";

        return await session.ExecuteWriteAsync(async tx =>
        {
            // Create organization node
            var orgQuery = @"
                CREATE (o:Organization {
                    name: $name,
                    slug: $slug,
                    description: $description,
                    mission: $mission,
                    vision: $vision,
                    contactEmail: $contactEmail,
                    contactPhone: $contactPhone,
                    website: $website,
                    address: $address,
                    city: $city,
                    state: $state,
                    country: $country,
                    zipCode: $zipCode,
                    latitude: $latitude,
                    longitude: $longitude,
                    location: point({latitude: $latitude, longitude: $longitude}),
                    organizationType: $organizationType,
                    taxId: $taxId,
                    foundedDate: $foundedDate,
                    logoUrl: $logoUrl,
                    coverImageUrl: $coverImageUrl,
                    facebookUrl: $facebookUrl,
                    twitterUrl: $twitterUrl,
                    instagramUrl: $instagramUrl,
                    linkedInUrl: $linkedInUrl,
                    focusAreas: $focusAreas,
                    tags: $tags,
                    createdBy: $createdBy,
                    createdDate: $createdDate,
                    status: $status,
                    isVerified: $isVerified
                })
                RETURN id(o) as id, o";

            var orgCursor = await tx.RunAsync(orgQuery, new
            {
                name = organization.Name,
                slug = organization.Slug,
                description = organization.Description,
                mission = organization.Mission,
                vision = organization.Vision,
                contactEmail = organization.ContactEmail,
                contactPhone = organization.ContactPhone,
                website = organization.Website,
                address = organization.Address,
                city = organization.City,
                state = organization.State,
                country = organization.Country,
                zipCode = organization.ZipCode,
                latitude = organization.Latitude,
                longitude = organization.Longitude,
                organizationType = organization.OrganizationType,
                taxId = organization.TaxId,
                foundedDate = organization.FoundedDate,
                logoUrl = organization.LogoUrl,
                coverImageUrl = organization.CoverImageUrl,
                facebookUrl = organization.FacebookUrl,
                twitterUrl = organization.TwitterUrl,
                instagramUrl = organization.InstagramUrl,
                linkedInUrl = organization.LinkedInUrl,
                focusAreas = organization.FocusAreas,
                tags = organization.Tags,
                createdBy = organization.CreatedBy,
                createdDate = organization.CreatedDate,
                status = organization.Status,
                isVerified = organization.IsVerified
            });

            var orgRecord = await orgCursor.SingleAsync();
            organization.Id = orgRecord["id"].As<long>();

            // Create admin relationship
            var adminQuery = @"
                MATCH (o:Organization), (u:User {userId: $userId})
                WHERE id(o) = $orgId
                MERGE (u)-[r:ADMIN_OF {since: $since}]->(o)
                RETURN r";

            await tx.RunAsync(adminQuery, new
            {
                orgId = organization.Id,
                userId,
                since = DateTime.UtcNow
            });

            return organization;
        });
    }

    /// <summary>
    /// Get organization by slug
    /// </summary>
    public async Task<OrganizationModel?> GetOrganizationBySlugAsync(string slug, string? userId = null)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (o:Organization {slug: $slug})
                OPTIONAL MATCH (o)<-[:ADMIN_OF]-(admin:User)
                OPTIONAL MATCH (o)<-[:MEMBER_OF]-(member:User)
                OPTIONAL MATCH (o)<-[:POSTED_BY]-(gw:GoodWork)
                OPTIONAL MATCH (gw)<-[:SIGNED_UP_FOR]-(volunteer:User)
                WITH o, count(DISTINCT admin) + count(DISTINCT member) as memberCount,
                     count(DISTINCT gw) as eventCount,
                     count(DISTINCT volunteer) as volunteerCount
                RETURN o, memberCount, eventCount, volunteerCount";

            var cursor = await tx.RunAsync(query, new { slug });
            
            if (!await cursor.FetchAsync())
                return null;

            var record = cursor.Current;
            var node = record["o"].As<INode>();
            var props = node.Properties;

            var org = new OrganizationModel
            {
                Id = node.Id,
                Name = props["name"].As<string>(),
                Slug = props["slug"].As<string>(),
                Description = props["description"].As<string>(),
                Mission = props.ContainsKey("mission") ? props["mission"].As<string?>() : null,
                Vision = props.ContainsKey("vision") ? props["vision"].As<string?>() : null,
                ContactEmail = props["contactEmail"].As<string>(),
                ContactPhone = props.ContainsKey("contactPhone") ? props["contactPhone"].As<string?>() : null,
                Website = props.ContainsKey("website") ? props["website"].As<string?>() : null,
                Address = props.ContainsKey("address") ? props["address"].As<string?>() : null,
                City = props.ContainsKey("city") ? props["city"].As<string?>() : null,
                State = props.ContainsKey("state") ? props["state"].As<string?>() : null,
                Country = props.ContainsKey("country") ? props["country"].As<string?>() : null,
                ZipCode = props.ContainsKey("zipCode") ? props["zipCode"].As<string?>() : null,
                Latitude = props.ContainsKey("latitude") ? props["latitude"].As<double?>() : null,
                Longitude = props.ContainsKey("longitude") ? props["longitude"].As<double?>() : null,
                OrganizationType = props.ContainsKey("organizationType") ? props["organizationType"].As<string?>() : null,
                TaxId = props.ContainsKey("taxId") ? props["taxId"].As<string?>() : null,
                FoundedDate = props.ContainsKey("foundedDate") ? props["foundedDate"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime : null,
                LogoUrl = props.ContainsKey("logoUrl") ? props["logoUrl"].As<string?>() : null,
                CoverImageUrl = props.ContainsKey("coverImageUrl") ? props["coverImageUrl"].As<string?>() : null,
                FacebookUrl = props.ContainsKey("facebookUrl") ? props["facebookUrl"].As<string?>() : null,
                TwitterUrl = props.ContainsKey("twitterUrl") ? props["twitterUrl"].As<string?>() : null,
                InstagramUrl = props.ContainsKey("instagramUrl") ? props["instagramUrl"].As<string?>() : null,
                LinkedInUrl = props.ContainsKey("linkedInUrl") ? props["linkedInUrl"].As<string?>() : null,
                FocusAreas = props.ContainsKey("focusAreas") ? props["focusAreas"].As<List<string>>() : null,
                Tags = props.ContainsKey("tags") ? props["tags"].As<List<string>>() : null,
                CreatedBy = props["createdBy"].As<string>(),
                CreatedDate = props["createdDate"].As<ZonedDateTime>().ToDateTimeOffset().DateTime,
                LastModifiedDate = props.ContainsKey("lastModifiedDate") ? props["lastModifiedDate"].As<ZonedDateTime?>()?.ToDateTimeOffset().DateTime : null,
                Status = props["status"].As<string>(),
                IsVerified = props.ContainsKey("isVerified") && props["isVerified"].As<bool>(),
                MemberCount = record["memberCount"].As<int>(),
                EventCount = record["eventCount"].As<int>(),
                VolunteerCount = record["volunteerCount"].As<int>()
            };

            // Check if user is admin or member
            if (!string.IsNullOrEmpty(userId))
            {
                var userQuery = @"
                    MATCH (o:Organization {slug: $slug})
                    MATCH (u:User {userId: $userId})
                    OPTIONAL MATCH (u)-[admin:ADMIN_OF]->(o)
                    OPTIONAL MATCH (u)-[member:MEMBER_OF]->(o)
                    RETURN admin IS NOT NULL as isAdmin, (admin IS NOT NULL OR member IS NOT NULL) as isMember";

                var userCursor = await tx.RunAsync(userQuery, new { slug, userId });
                if (await userCursor.FetchAsync())
                {
                    var userRecord = userCursor.Current;
                    org.IsUserAdmin = userRecord["isAdmin"].As<bool>();
                    org.IsUserMember = userRecord["isMember"].As<bool>();
                }
            }

            return org;
        });
    }

    /// <summary>
    /// Get all organizations (with pagination)
    /// </summary>
    public async Task<List<OrganizationModel>> GetOrganizationsAsync(int skip = 0, int limit = 20, string? searchTerm = null)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var whereClause = string.IsNullOrEmpty(searchTerm)
                ? "WHERE o.status = 'Active'"
                : "WHERE o.status = 'Active' AND (toLower(o.name) CONTAINS toLower($searchTerm) OR toLower(o.description) CONTAINS toLower($searchTerm) OR toLower(o.city) CONTAINS toLower($searchTerm))";

            var query = $@"
                MATCH (o:Organization)
                {whereClause}
                OPTIONAL MATCH (o)<-[:MEMBER_OF|ADMIN_OF]-(member:User)
                OPTIONAL MATCH (o)<-[:POSTED_BY]-(event:GoodWork)
                WITH o, count(DISTINCT member) as memberCount, count(DISTINCT event) as eventCount
                ORDER BY o.createdDate DESC
                SKIP $skip
                LIMIT $limit
                RETURN o, memberCount, eventCount";

            var cursor = await tx.RunAsync(query, new { skip, limit, searchTerm });
            var organizations = new List<OrganizationModel>();

            await foreach (var record in cursor)
            {
                var node = record["o"].As<INode>();
                var props = node.Properties;

                organizations.Add(new OrganizationModel
                {
                    Id = node.Id,
                    Name = props["name"].As<string>(),
                    Slug = props["slug"].As<string>(),
                    Description = props["description"].As<string>(),
                    LogoUrl = props.ContainsKey("logoUrl") ? props["logoUrl"].As<string?>() : null,
                    OrganizationType = props.ContainsKey("organizationType") ? props["organizationType"].As<string?>() : null,
                    City = props.ContainsKey("city") ? props["city"].As<string?>() : null,
                    State = props.ContainsKey("state") ? props["state"].As<string?>() : null,
                    FocusAreas = props.ContainsKey("focusAreas") ? props["focusAreas"].As<List<string>>() : null,
                    MemberCount = record["memberCount"].As<int>(),
                    EventCount = record["eventCount"].As<int>(),
                    IsVerified = props.ContainsKey("isVerified") && props["isVerified"].As<bool>(),
                    CreatedDate = props["createdDate"].As<ZonedDateTime>().ToDateTimeOffset().DateTime
                });
            }

            return organizations;
        });
    }

    /// <summary>
    /// Count organizations (with optional search)
    /// </summary>
    public async Task<int> CountOrganizationsAsync(string? searchTerm = null)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var whereClause = string.IsNullOrEmpty(searchTerm)
                ? "WHERE o.status = 'Active'"
                : "WHERE o.status = 'Active' AND (toLower(o.name) CONTAINS toLower($searchTerm) OR toLower(o.description) CONTAINS toLower($searchTerm) OR toLower(o.city) CONTAINS toLower($searchTerm))";

            var query = $@"
                MATCH (o:Organization)
                {whereClause}
                RETURN count(o) as totalCount";

            var cursor = await tx.RunAsync(query, new { searchTerm });
            var record = await cursor.SingleAsync();
            return record["totalCount"].As<int>();
        });
    }

    /// <summary>
    /// Get user's organizations (where user is admin or member)
    /// </summary>
    public async Task<List<OrganizationModel>> GetUserOrganizationsAsync(string userId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})-[r:ADMIN_OF|MEMBER_OF]->(o:Organization)
                WHERE o.status = 'Active'
                OPTIONAL MATCH (o)<-[:MEMBER_OF|ADMIN_OF]-(member:User)
                OPTIONAL MATCH (o)<-[:POSTED_BY]-(event:GoodWork)
                WITH o, type(r) as relationship, count(DISTINCT member) as memberCount, count(DISTINCT event) as eventCount
                ORDER BY o.name
                RETURN o, relationship, memberCount, eventCount";

            var cursor = await tx.RunAsync(query, new { userId });
            var organizations = new List<OrganizationModel>();

            await foreach (var record in cursor)
            {
                var node = record["o"].As<INode>();
                var props = node.Properties;
                var relationship = record["relationship"].As<string>();

                organizations.Add(new OrganizationModel
                {
                    Id = node.Id,
                    Name = props["name"].As<string>(),
                    Slug = props["slug"].As<string>(),
                    Description = props["description"].As<string>(),
                    LogoUrl = props.ContainsKey("logoUrl") ? props["logoUrl"].As<string?>() : null,
                    MemberCount = record["memberCount"].As<int>(),
                    EventCount = record["eventCount"].As<int>(),
                    IsUserAdmin = relationship == "ADMIN_OF",
                    IsUserMember = true,
                    CreatedDate = props["createdDate"].As<ZonedDateTime>().ToDateTimeOffset().DateTime
                });
            }

            return organizations;
        });
    }

    /// <summary>
    /// Add member to organization
    /// </summary>
    public async Task<bool> AddMemberAsync(long organizationId, string userId, string role = "Member")
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (o:Organization), (u:User {userId: $userId})
                WHERE id(o) = $orgId
                MERGE (u)-[r:MEMBER_OF {role: $role, joinedDate: $joinedDate}]->(o)
                RETURN r";

            var cursor = await tx.RunAsync(query, new
            {
                orgId = organizationId,
                userId,
                role,
                joinedDate = DateTime.UtcNow
            });

            return await cursor.FetchAsync();
        });
    }

    /// <summary>
    /// Remove member from organization
    /// </summary>
    public async Task<bool> RemoveMemberAsync(long organizationId, string userId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})-[r:MEMBER_OF]->(o:Organization)
                WHERE id(o) = $orgId
                DELETE r
                RETURN count(r) as deleted";

            var cursor = await tx.RunAsync(query, new { orgId = organizationId, userId });
            var record = await cursor.SingleAsync();
            return record["deleted"].As<int>() > 0;
        });
    }

    /// <summary>
    /// Get organization members
    /// </summary>
    public async Task<List<OrganizationMemberModel>> GetOrganizationMembersAsync(long organizationId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (u:User)-[r:ADMIN_OF|MEMBER_OF]->(o:Organization)
                WHERE id(o) = $orgId
                OPTIONAL MATCH (u)-[:CREATED]->(gw:GoodWork)-[:POSTED_BY]->(o)
                WITH u, r, count(DISTINCT gw) as eventCount
                RETURN u, type(r) as relationship, 
                       CASE WHEN type(r) = 'ADMIN_OF' THEN r.since ELSE r.joinedDate END as joinedDate,
                       CASE WHEN type(r) = 'ADMIN_OF' THEN 'Admin' ELSE coalesce(r.role, 'Member') END as role,
                       eventCount
                ORDER BY type(r) DESC, joinedDate ASC";

            var cursor = await tx.RunAsync(query, new { orgId = organizationId });
            var members = new List<OrganizationMemberModel>();

            await foreach (var record in cursor)
            {
                var node = record["u"].As<INode>();
                var props = node.Properties;

                members.Add(new OrganizationMemberModel
                {
                    UserId = props["userId"].As<string>(),
                    Name = props.ContainsKey("name") ? props["name"].As<string>() : "Unknown",
                    Email = props.ContainsKey("email") ? props["email"].As<string>() : "",
                    Phone = props.ContainsKey("phone") ? props["phone"].As<string?>() : null,
                    Role = record["role"].As<string>(),
                    JoinedDate = record["joinedDate"].As<ZonedDateTime>().ToDateTimeOffset().DateTime,
                    ContributedEvents = record["eventCount"].As<int>()
                });
            }

            return members;
        });
    }

    /// <summary>
    /// Update organization
    /// </summary>
    public async Task<bool> UpdateOrganizationAsync(OrganizationModel organization)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            organization.LastModifiedDate = DateTime.UtcNow;

            var query = @"
                MATCH (o:Organization)
                WHERE id(o) = $id
                SET o.name = $name,
                    o.description = $description,
                    o.mission = $mission,
                    o.vision = $vision,
                    o.contactEmail = $contactEmail,
                    o.contactPhone = $contactPhone,
                    o.website = $website,
                    o.address = $address,
                    o.city = $city,
                    o.state = $state,
                    o.country = $country,
                    o.zipCode = $zipCode,
                    o.latitude = $latitude,
                    o.longitude = $longitude,
                    o.location = point({latitude: $latitude, longitude: $longitude}),
                    o.organizationType = $organizationType,
                    o.logoUrl = $logoUrl,
                    o.coverImageUrl = $coverImageUrl,
                    o.facebookUrl = $facebookUrl,
                    o.twitterUrl = $twitterUrl,
                    o.instagramUrl = $instagramUrl,
                    o.linkedInUrl = $linkedInUrl,
                    o.focusAreas = $focusAreas,
                    o.tags = $tags,
                    o.lastModifiedDate = $lastModifiedDate
                RETURN o";

            var cursor = await tx.RunAsync(query, new
            {
                id = organization.Id,
                name = organization.Name,
                description = organization.Description,
                mission = organization.Mission,
                vision = organization.Vision,
                contactEmail = organization.ContactEmail,
                contactPhone = organization.ContactPhone,
                website = organization.Website,
                address = organization.Address,
                city = organization.City,
                state = organization.State,
                country = organization.Country,
                zipCode = organization.ZipCode,
                latitude = organization.Latitude,
                longitude = organization.Longitude,
                organizationType = organization.OrganizationType,
                logoUrl = organization.LogoUrl,
                coverImageUrl = organization.CoverImageUrl,
                facebookUrl = organization.FacebookUrl,
                twitterUrl = organization.TwitterUrl,
                instagramUrl = organization.InstagramUrl,
                linkedInUrl = organization.LinkedInUrl,
                focusAreas = organization.FocusAreas,
                tags = organization.Tags,
                lastModifiedDate = organization.LastModifiedDate
            });

            return await cursor.FetchAsync();
        });
    }

    /// <summary>
    /// Delete organization (soft delete - set status to Inactive)
    /// </summary>
    public async Task<bool> DeleteOrganizationAsync(long organizationId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (o:Organization)
                WHERE id(o) = $id
                SET o.status = 'Inactive'
                RETURN o";

            var cursor = await tx.RunAsync(query, new { id = organizationId });
            return await cursor.FetchAsync();
        });
    }

    /// <summary>
    /// Check if user is admin of organization
    /// </summary>
    public async Task<bool> IsUserAdminAsync(long organizationId, string userId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})-[r:ADMIN_OF]->(o:Organization)
                WHERE id(o) = $orgId
                RETURN count(r) > 0 as isAdmin";

            var cursor = await tx.RunAsync(query, new { orgId = organizationId, userId });
            var record = await cursor.SingleAsync();
            return record["isAdmin"].As<bool>();
        });
    }

    /// <summary>
    /// Promote member to admin
    /// </summary>
    public async Task<bool> PromoteToAdminAsync(long organizationId, string userId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            // Remove existing membership relationship
            await tx.RunAsync(@"
                MATCH (u:User {userId: $userId})-[r:MEMBER_OF]->(o:Organization)
                WHERE id(o) = $orgId
                DELETE r", new { orgId = organizationId, userId });

            // Create admin relationship
            var query = @"
                MATCH (u:User {userId: $userId}), (o:Organization)
                WHERE id(o) = $orgId
                MERGE (u)-[r:ADMIN_OF {since: $since}]->(o)
                RETURN r";

            var cursor = await tx.RunAsync(query, new
            {
                orgId = organizationId,
                userId,
                since = DateTime.UtcNow
            });

            return await cursor.FetchAsync();
        });
    }
}

using CharityRabbit.Models;
using Microsoft.EntityFrameworkCore;

namespace CharityRabbit.Data;

/// <summary>Projection for endpoint authorization checks (see GetOwnershipAsync).</summary>
public sealed record GoodWorkOwnership(string? CreatedBy, long? OrganizationId);

// GoodWorks (volunteer events), interest/signup, participants & recommendations — PostgreSQL (EF Core).
// Public API identical to the former neo4j-backed service (former Neo4jService, renamed).
// Notes: distance search uses a bounding-box approximation (no PostGIS, per decision); the CREATED and
// FRIENDS_WITH relationships were never written in the graph, so their contributions stay zero (preserved).
public class GoodWorksService(CharityDbContext db, IGeocodingService locationServices) : IGoodWorksService
{
    private static DateTimeOffset? ToDto(DateTime? v) =>
        v.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null;

    private async Task<Contact> GetOrCreateContactAsync(string email, string? name, string? phone)
    {
        var c = await db.Contacts.FirstOrDefaultAsync(x => x.Email == email);
        if (c is null) { c = new Contact { Email = email }; db.Contacts.Add(c); }
        c.Name = name; c.Phone = phone;
        return c;
    }

    private async Task<Location> GetOrCreateLocationAsync(string city, string state, string country, string zip)
    {
        var l = await db.Locations.FirstOrDefaultAsync(x => x.City == city && x.State == state && x.Country == country && x.Zip == zip);
        if (l is null) { l = new Location { City = city, State = state, Country = country, Zip = zip }; db.Locations.Add(l); }
        return l;
    }

    private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> names)
    {
        var result = new List<Tag>();
        foreach (var n in names.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            result.Add(await db.Tags.FirstOrDefaultAsync(t => t.Name == n) ?? db.Tags.Add(new Tag { Name = n }).Entity);
        return result;
    }

    private async Task<List<Skill>> ResolveSkillsAsync(IEnumerable<string> names)
    {
        var result = new List<Skill>();
        foreach (var n in names.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
            result.Add(await db.Skills.FirstOrDefaultAsync(s => s.Name == n) ?? db.Skills.Add(new Skill { Name = n }).Entity);
        return result;
    }

    public async Task CreateGoodWorkAsync(GoodWorksModel goodWork, string? userId = null)
    {
        var (city, state, country, zip) = await locationServices.GetLocationDetailsAsync(goodWork.Latitude, goodWork.Longitude);

        var g = new GoodWork
        {
            Name = goodWork.Name,
            Description = goodWork.Description ?? "",
            DetailedDescription = goodWork.DetailedDescription,
            Category = goodWork.Category,
            SubCategory = goodWork.SubCategory,
            Address = goodWork.Address,
            Latitude = goodWork.Latitude,
            Longitude = goodWork.Longitude,
            StartTime = ToDto(goodWork.StartTime),
            EndTime = ToDto(goodWork.EndTime),
            EstimatedDurationMinutes = goodWork.EstimatedDuration?.TotalMinutes,
            EffortLevel = goodWork.EffortLevel ?? "Moderate",
            IsAccessible = goodWork.IsAccessible,
            IsVirtual = goodWork.IsVirtual,
            MaxParticipants = goodWork.MaxParticipants,
            CurrentParticipants = goodWork.CurrentParticipants,
            MinimumAge = goodWork.MinimumAge,
            FamilyFriendly = goodWork.FamilyFriendly,
            IsRecurring = goodWork.IsRecurring,
            RecurrencePattern = goodWork.RecurrencePattern,
            OrganizationName = goodWork.OrganizationName,
            OrganizationWebsite = goodWork.OrganizationWebsite,
            ParkingAvailable = goodWork.ParkingAvailable,
            PublicTransitAccessible = goodWork.PublicTransitAccessible,
            SpecialInstructions = goodWork.SpecialInstructions,
            ImpactDescription = goodWork.ImpactDescription,
            EstimatedPeopleHelped = goodWork.EstimatedPeopleHelped,
            Status = goodWork.Status ?? "Active",
            OutdoorActivity = goodWork.OutdoorActivity,
            WeatherDependent = goodWork.WeatherDependent,
            CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(goodWork.CreatedDate, DateTimeKind.Utc)),
            CreatedBy = userId,
            Contact = await GetOrCreateContactAsync(goodWork.ContactEmail ?? "", goodWork.ContactName, goodWork.ContactPhone),
            Location = await GetOrCreateLocationAsync(city ?? "", state ?? "", country ?? "", zip ?? ""),
            Tags = await ResolveTagsAsync(goodWork.Tags ?? new()),
            Skills = await ResolveSkillsAsync(goodWork.RequiredSkills ?? new()),
        };
        db.GoodWorks.Add(g);
        await db.SaveChangesAsync();
        goodWork.Id = g.Id;
    }

    /// <summary>Cheap single-row read for endpoint-level ownership/authorization checks —
    /// avoids materializing the full aggregate (4 Includes + participation counts).</summary>
    public Task<GoodWorkOwnership?> GetOwnershipAsync(long id) =>
        db.GoodWorks.Where(g => g.Id == id)
            .Select(g => new GoodWorkOwnership(g.CreatedBy, g.OrganizationId))
            .FirstOrDefaultAsync();

    private IQueryable<GoodWork> GoodWorksWithDetails() =>
        db.GoodWorks.Include(g => g.Contact).Include(g => g.Location).Include(g => g.Tags).Include(g => g.Skills);

    public async Task<GoodWorksModel?> GetGoodWorkByIdAsync(long id, string? userId = null)
    {
        var g = await GoodWorksWithDetails().FirstOrDefaultAsync(x => x.Id == id);
        if (g is null) return null;
        var m = Map(g);
        m.InterestedCount = await db.Interested.CountAsync(x => x.GoodWorkId == id);
        m.SignedUpCount = await db.Signups.CountAsync(x => x.GoodWorkId == id);
        m.IsUserInterested = userId != null && await db.Interested.AnyAsync(x => x.GoodWorkId == id && x.UserId == userId);
        m.IsUserSignedUp = userId != null && await db.Signups.AnyAsync(x => x.GoodWorkId == id && x.UserId == userId);
        return m;
    }

    public Task<GoodWorksModel> GetGoodWorkWithDetailsAsync(long id, string? userId = null) =>
        GetGoodWorkByIdAsync(id, userId)!;

    // Shared predicate builder for search + count.
    private IQueryable<GoodWork> ApplySearch(IQueryable<GoodWork> q, GoodWorksSearchCriteria c)
    {
        if (!string.IsNullOrEmpty(c.Category)) q = q.Where(g => g.Category == c.Category);
        if (!string.IsNullOrEmpty(c.SubCategory)) q = q.Where(g => g.SubCategory == c.SubCategory);
        if (c.Tags is { Count: > 0 }) q = q.Where(g => g.Tags.Any(t => c.Tags!.Contains(t.Name)));
        if (c.RequiredSkills is { Count: > 0 })
            q = q.Where(g => c.RequiredSkills!.All(name => g.Skills.Any(s => s.Name == name)));
        if (!string.IsNullOrEmpty(c.SearchText))
        {
            var t = c.SearchText.ToLower();
            q = q.Where(g => g.Name.ToLower().Contains(t) || g.Description.ToLower().Contains(t));
        }
        if (c.CenterLatitude is { } lat0 && c.CenterLongitude is { } lng0 && c.RadiusMiles is { } miles)
        {
            // Bounding-box approximation of a radius (no PostGIS). ~69 mi per degree latitude.
            var dLat = miles / 69.0;
            var dLng = miles / (69.0 * Math.Cos(lat0 * Math.PI / 180.0));
            q = q.Where(g => g.Latitude >= lat0 - dLat && g.Latitude <= lat0 + dLat
                          && g.Longitude >= lng0 - dLng && g.Longitude <= lng0 + dLng);
        }
        if (c.StartDateFrom is { } sf) { var v = ToDto(sf); q = q.Where(g => g.StartTime >= v); }
        if (c.StartDateTo is { } st) { var v = ToDto(st); q = q.Where(g => g.StartTime <= v); }
        if (!string.IsNullOrEmpty(c.EffortLevel)) q = q.Where(g => g.EffortLevel == c.EffortLevel);
        if (c.IsVirtual is { } iv) q = q.Where(g => g.IsVirtual == iv);
        if (c.IsAccessible is { } ia) q = q.Where(g => g.IsAccessible == ia);
        if (c.FamilyFriendly is { } ff) q = q.Where(g => g.FamilyFriendly == ff);
        if (c.HasAvailableSpots == true) q = q.Where(g => g.MaxParticipants == null || g.CurrentParticipants < g.MaxParticipants);
        return q.Where(g => g.Status == "Active");
    }

    public async Task<List<GoodWorksModel>> SearchGoodWorksAsync(GoodWorksSearchCriteria criteria, string? userId = null)
    {
        var q = ApplySearch(GoodWorksWithDetails(), criteria).OrderBy(g => g.StartTime);
        var skip = criteria.Page > 0 ? (criteria.Page - 1) * criteria.PageSize : 0;
        var take = criteria.PageSize > 0 ? criteria.PageSize : 100;
        var rows = await q.Skip(skip).Take(take).ToListAsync();
        var models = new List<GoodWorksModel>();
        foreach (var g in rows)
        {
            var m = Map(g);
            m.SignedUpCount = await db.Signups.CountAsync(s => s.GoodWorkId == g.Id);
            models.Add(m);
        }
        return models;
    }

    public async Task<int> CountSearchResultsAsync(GoodWorksSearchCriteria criteria) =>
        await ApplySearch(db.GoodWorks, criteria).CountAsync();

    public async Task<List<GoodWorksModel>> GetSimilarGoodWorksAsync(long goodWorkId, string? userId = null, int limit = 10)
    {
        var orig = await GoodWorksWithDetails().FirstOrDefaultAsync(g => g.Id == goodWorkId);
        if (orig is null) return new();
        var catName = orig.Category;
        var tagNames = orig.Tags.Select(t => t.Name).ToHashSet();
        var skillNames = orig.Skills.Select(s => s.Name).ToHashSet();
        var locId = orig.LocationId;
        var effort = orig.EffortLevel;

        var candidates = await GoodWorksWithDetails().Where(g => g.Id != goodWorkId && g.Status == "Active").ToListAsync();
        var scored = candidates.Select(g => new
        {
            g,
            score = (g.Category == catName && catName != null ? 3 : 0)
                  + g.Tags.Count(t => tagNames.Contains(t.Name)) * 2
                  + g.Skills.Count(s => skillNames.Contains(s.Name)) * 2
                  + (g.LocationId == locId && locId != null ? 2 : 0)
                  + (g.EffortLevel == effort ? 1 : 0),
            daysDiff = orig.StartTime.HasValue && g.StartTime.HasValue ? Math.Abs((g.StartTime.Value - orig.StartTime.Value).TotalDays) : double.MaxValue,
        }).Where(x => x.score > 0)
          .OrderByDescending(x => x.score).ThenBy(x => x.daysDiff).Take(limit);
        return scored.Select(x => Map(x.g)).ToList();
    }

    private async Task EnsureUserAsync(string userId, string? name, string? email)
    {
        var u = await db.Users.FindAsync(userId);
        if (u is null) { u = new User { UserId = userId, Name = name, Email = email }; db.Users.Add(u); }
        else { u.Name ??= name; u.Email ??= email; }
    }

    public async Task MarkUserInterestedAsync(string userId, long goodWorkId, bool interested, string? userName = null, string? userEmail = null)
    {
        if (interested)
        {
            // Guard like SignUpUserAsync: an unknown id would otherwise FK-fault on save.
            if (!await db.GoodWorks.AnyAsync(g => g.Id == goodWorkId)) return;
            await EnsureUserAsync(userId, userName, userEmail);
            if (!await db.Interested.AnyAsync(x => x.UserId == userId && x.GoodWorkId == goodWorkId))
                db.Interested.Add(new UserInterested { UserId = userId, GoodWorkId = goodWorkId, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        else
        {
            await db.Interested.Where(x => x.UserId == userId && x.GoodWorkId == goodWorkId).ExecuteDeleteAsync();
        }
    }

    public async Task SignUpUserAsync(string userId, long goodWorkId, bool signUp, string? userName = null, string? userEmail = null)
    {
        var g = await db.GoodWorks.FindAsync(goodWorkId);
        if (g is null) return;
        if (signUp)
        {
            await EnsureUserAsync(userId, userName, userEmail);
            if (!await db.Signups.AnyAsync(x => x.UserId == userId && x.GoodWorkId == goodWorkId))
            {
                db.Signups.Add(new UserSignup { UserId = userId, GoodWorkId = goodWorkId, CreatedAt = DateTimeOffset.UtcNow });
                g.CurrentParticipants += 1;
            }
            await db.SaveChangesAsync();
        }
        else
        {
            var removed = await db.Signups.Where(x => x.UserId == userId && x.GoodWorkId == goodWorkId).ExecuteDeleteAsync();
            if (removed > 0) { g.CurrentParticipants = Math.Max(0, g.CurrentParticipants - 1); await db.SaveChangesAsync(); }
        }
    }

    // Note: Include() can't follow a Select() projection — filter GoodWorks via the join table instead.
    public Task<List<GoodWorksModel>> GetUserInterestedGoodWorksAsync(string userId) =>
        db.GoodWorks.Where(g => db.Interested.Any(x => x.UserId == userId && x.GoodWorkId == g.Id))
            .Include(g => g.Contact).Include(g => g.Location)
            .OrderBy(g => g.StartTime).ToListAsync().ContinueWith(t => t.Result.Select(Map).ToList());

    public Task<List<GoodWorksModel>> GetUserSignedUpGoodWorksAsync(string userId) =>
        db.GoodWorks.Where(g => db.Signups.Any(x => x.UserId == userId && x.GoodWorkId == g.Id))
            .Include(g => g.Contact).Include(g => g.Location)
            .OrderBy(g => g.StartTime).ToListAsync().ContinueWith(t => t.Result.Select(Map).ToList());

    public async Task<List<GoodWorksModel>> GetUserCreatedGoodWorksAsync(string userId)
    {
        var rows = await GoodWorksWithDetails().Where(g => g.CreatedBy == userId)
            .OrderByDescending(g => g.CreatedDate).ToListAsync();
        var models = new List<GoodWorksModel>();
        foreach (var g in rows) { var m = Map(g); m.SignedUpCount = await db.Signups.CountAsync(s => s.GoodWorkId == g.Id); models.Add(m); }
        return models;
    }

    public async Task<List<GoodWorksModel>> GetUserCreatedGoodWorksPagedAsync(string userId, int page = 1, int pageSize = 10, string? searchText = null)
    {
        var q = GoodWorksWithDetails().Where(g => g.CreatedBy == userId);
        if (!string.IsNullOrEmpty(searchText))
        {
            var t = searchText.ToLower();
            q = q.Where(g => g.Name.ToLower().Contains(t) || g.Description.ToLower().Contains(t) || (g.Category != null && g.Category.ToLower().Contains(t)));
        }
        var rows = await q.OrderByDescending(g => g.CreatedDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var models = new List<GoodWorksModel>();
        foreach (var g in rows) { var m = Map(g); m.SignedUpCount = await db.Signups.CountAsync(s => s.GoodWorkId == g.Id); models.Add(m); }
        return models;
    }

    public async Task<int> CountUserCreatedGoodWorksAsync(string userId, string? searchText = null)
    {
        var q = db.GoodWorks.Where(g => g.CreatedBy == userId);
        if (!string.IsNullOrEmpty(searchText))
        {
            var t = searchText.ToLower();
            q = q.Where(g => g.Name.ToLower().Contains(t) || g.Description.ToLower().Contains(t) || (g.Category != null && g.Category.ToLower().Contains(t)));
        }
        return await q.CountAsync();
    }

    public async Task UpdateGoodWorkAsync(long id, GoodWorksModel goodWork, string userId)
    {
        var g = await db.GoodWorks.Include(x => x.Contact).FirstOrDefaultAsync(x => x.Id == id && x.CreatedBy == userId);
        if (g is null) return;
        var (city, state, country, zip) = await locationServices.GetLocationDetailsAsync(goodWork.Latitude, goodWork.Longitude);

        g.Name = goodWork.Name;
        g.Description = goodWork.Description ?? "";
        g.DetailedDescription = goodWork.DetailedDescription;
        g.Address = goodWork.Address;
        g.Latitude = goodWork.Latitude;
        g.Longitude = goodWork.Longitude;
        g.StartTime = ToDto(goodWork.StartTime);
        g.EndTime = ToDto(goodWork.EndTime);
        g.EffortLevel = goodWork.EffortLevel ?? "Moderate";
        g.IsAccessible = goodWork.IsAccessible;
        g.EstimatedDurationMinutes = goodWork.EstimatedDuration?.TotalMinutes;
        g.IsVirtual = goodWork.IsVirtual;
        g.MaxParticipants = goodWork.MaxParticipants;
        g.MinimumAge = goodWork.MinimumAge;
        g.FamilyFriendly = goodWork.FamilyFriendly;
        g.IsRecurring = goodWork.IsRecurring;
        g.RecurrencePattern = goodWork.RecurrencePattern;
        g.OrganizationName = goodWork.OrganizationName;
        g.OrganizationWebsite = goodWork.OrganizationWebsite;
        g.ParkingAvailable = goodWork.ParkingAvailable;
        g.PublicTransitAccessible = goodWork.PublicTransitAccessible;
        g.SpecialInstructions = goodWork.SpecialInstructions;
        g.ImpactDescription = goodWork.ImpactDescription;
        g.EstimatedPeopleHelped = goodWork.EstimatedPeopleHelped;
        g.Status = goodWork.Status ?? "Active";
        g.OutdoorActivity = goodWork.OutdoorActivity;
        g.WeatherDependent = goodWork.WeatherDependent;
        g.Category = goodWork.Category;              // denormalized (was re-pointed BELONGS_TO edge)
        g.Contact = await GetOrCreateContactAsync(goodWork.ContactEmail ?? "", goodWork.ContactName, goodWork.ContactPhone);
        g.Location = await GetOrCreateLocationAsync(city ?? "", state ?? "", country ?? "", zip ?? "");
        // (Tags/skills were not modified by the original update, so they're left as-is.)
        await db.SaveChangesAsync();
    }

    public async Task DeleteGoodWorkAsync(long id, string userId) =>
        await db.GoodWorks.Where(g => g.Id == id && g.CreatedBy == userId).ExecuteDeleteAsync();

    // Active good works posted by an organization (was the POSTED_BY edge). Used by the org profile page.
    public async Task<List<GoodWorksModel>> GetOrganizationEventsAsync(long organizationId)
    {
        var rows = await GoodWorksWithDetails()
            .Where(g => g.OrganizationId == organizationId && g.Status == "Active")
            .OrderByDescending(g => g.StartTime).ToListAsync();
        return rows.Select(Map).ToList();
    }

    // Link a good work to its posting organization (was the POSTED_BY edge). Used by test-data import.
    public async Task SetGoodWorkOrganizationAsync(long goodWorkId, long organizationId)
    {
        var g = await db.GoodWorks.FindAsync(goodWorkId);
        if (g is null) return;
        g.OrganizationId = organizationId;
        await db.SaveChangesAsync();
    }

    public async Task<int> DeleteTestDataAsync(string testDataMarker)
    {
        var ids = await db.GoodWorks.Where(g => g.Tags.Any(t => t.Name == testDataMarker)).Select(g => g.Id).ToListAsync();
        await db.GoodWorks.Where(g => ids.Contains(g.Id)).ExecuteDeleteAsync();
        return ids.Count;
    }

    public Task<int> CountTestDataAsync(string testDataMarker) =>
        db.GoodWorks.CountAsync(g => g.Tags.Any(t => t.Name == testDataMarker));

    public Task<List<GoodWorksModel>> GetGoodWorksByCategoryAsync(string category) =>
        db.GoodWorks.Where(g => g.Category == category)
            .Select(g => new GoodWorksModel { Name = g.Name, Description = g.Description, Latitude = g.Latitude, Longitude = g.Longitude })
            .ToListAsync();

    public Task<List<GoodWorksModel>> GetGoodWorksByZipAsync(string zip) =>
        db.GoodWorks.Where(g => g.Location != null && g.Location.Zip == zip)
            .Select(g => new GoodWorksModel { Name = g.Name, Description = g.Description, Latitude = g.Latitude, Longitude = g.Longitude })
            .ToListAsync();

    public Task<List<GoodWorksModel>> GetGoodWorksByLocationAsync(string city, string state, string country) =>
        db.GoodWorks.Where(g => g.Location != null && g.Location.City == city && g.Location.State == state && g.Location.Country == country)
            .Select(g => new GoodWorksModel { Name = g.Name, Description = g.Description, Latitude = g.Latitude, Longitude = g.Longitude })
            .ToListAsync();

    public async Task<List<GoodWorksModel>> GetAllGoodWorksWithRelationshipsAsync()
    {
        var rows = await GoodWorksWithDetails().ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task<List<GoodWorksModel>> GetGoodWorksInBoundsAsync(double minLat, double maxLat, double minLng, double maxLng)
    {
        var rows = await GoodWorksWithDetails()
            .Where(g => g.Latitude >= minLat && g.Latitude <= maxLat && g.Longitude >= minLng && g.Longitude <= maxLng)
            .OrderBy(g => g.StartTime).Take(500).ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task<List<GoodWorksModel>> GetUpcomingUserEventsAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var signedIds = await db.Signups.Where(s => s.UserId == userId).Select(s => s.GoodWorkId).ToListAsync();
        var interestedIds = await db.Interested.Where(i => i.UserId == userId).Select(i => i.GoodWorkId).ToListAsync();
        var ids = signedIds.Concat(interestedIds).Distinct().ToList();
        var rows = await GoodWorksWithDetails()
            .Where(g => ids.Contains(g.Id) && g.StartTime >= now && g.Status == "Active")
            .OrderBy(g => g.StartTime).Take(20).ToListAsync();
        return rows.Select(g =>
        {
            var m = Map(g);
            m.IsUserSignedUp = signedIds.Contains(g.Id);
            m.IsUserInterested = interestedIds.Contains(g.Id);
            return m;
        }).ToList();
    }

    public async Task<List<GoodWorksModel>> GetRecommendedGoodWorksAsync(string userId, int limit = 10)
    {
        var now = DateTimeOffset.UtcNow;
        var myIds = await db.Signups.Where(s => s.UserId == userId).Select(s => s.GoodWorkId)
            .Concat(db.Interested.Where(i => i.UserId == userId).Select(i => i.GoodWorkId)).Distinct().ToListAsync();
        var myCats = await db.GoodWorks.Where(g => myIds.Contains(g.Id)).Select(g => g.Category).Distinct().ToListAsync();
        var myTagNames = await db.GoodWorks.Where(g => myIds.Contains(g.Id)).SelectMany(g => g.Tags.Select(t => t.Name)).Distinct().ToListAsync();
        // FRIENDS_WITH was never populated, so the friend-interest term is always 0 (preserved).

        var candidates = await GoodWorksWithDetails()
            .Where(g => !myIds.Contains(g.Id) && g.StartTime >= now && g.Status == "Active").ToListAsync();
        var scored = candidates.Select(g => new
        {
            g,
            score = (g.Category != null && myCats.Contains(g.Category) ? 3 : 0)
                  + (g.Tags.Any(t => myTagNames.Contains(t.Name)) ? 2 : 0),
        }).Where(x => x.score > 0).OrderByDescending(x => x.score).ThenBy(x => x.g.StartTime).Take(limit);
        return scored.Select(x => Map(x.g)).ToList();
    }

    public async Task<List<GoodWorksModel>> GetNewOpportunitiesAsync(int daysBack = 30, int limit = 12)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-daysBack);
        var rows = await GoodWorksWithDetails()
            .Where(g => g.CreatedDate >= cutoff && g.Status == "Active" && g.StartTime >= now)
            .OrderByDescending(g => g.CreatedDate).Take(limit).ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task<List<DoGooderModel>> GetActiveDoGoodersAsync(int limit = 10)
    {
        // Original counted SIGNED_UP_FOR (weight 3) and CREATED (weight 5); CREATED was never written,
        // so only signups contribute. Preserved.
        var rows = await db.Signups
            .GroupBy(s => s.UserId)
            .Select(grp => new { UserId = grp.Key, EventCount = grp.Select(x => x.GoodWorkId).Distinct().Count(), Signups = grp.Count() })
            .OrderByDescending(x => x.EventCount).Take(limit).ToListAsync();

        var result = new List<DoGooderModel>();
        foreach (var r in rows)
        {
            var u = await db.Users.FindAsync(r.UserId);
            result.Add(new DoGooderModel
            {
                UserId = r.UserId,
                Name = u?.Name ?? u?.Email ?? "Anonymous User",
                EventCount = r.EventCount,
                CarrotsEarned = r.Signups * 3,
            });
        }
        return result;
    }

    public async Task<List<ParticipantModel>> GetGoodWorkParticipantsAsync(long goodWorkId)
    {
        var signups = await db.Signups.Where(s => s.GoodWorkId == goodWorkId)
            .Select(s => new ParticipantModel
            {
                UserId = s.UserId, Name = s.User.Name ?? s.User.Email ?? "Anonymous User",
                Email = s.User.Email ?? "", Phone = s.User.Phone,
                RelationshipType = "SIGNED_UP_FOR", EngagementDate = s.CreatedAt.UtcDateTime,
            }).ToListAsync();
        var interested = await db.Interested.Where(i => i.GoodWorkId == goodWorkId)
            .Select(i => new ParticipantModel
            {
                UserId = i.UserId, Name = i.User.Name ?? i.User.Email ?? "Anonymous User",
                Email = i.User.Email ?? "", Phone = i.User.Phone,
                RelationshipType = "INTERESTED_IN", EngagementDate = i.CreatedAt.UtcDateTime,
            }).ToListAsync();
        return signups.Concat(interested)
            .OrderByDescending(p => p.RelationshipType).ThenByDescending(p => p.EngagementDate).ToList();
    }

    // ---- entity -> model ----
    private static GoodWorksModel Map(GoodWork g)
    {
        var m = new GoodWorksModel
        {
            Id = g.Id, Name = g.Name, Description = g.Description, DetailedDescription = g.DetailedDescription,
            Category = g.Category ?? "", SubCategory = g.SubCategory, Latitude = g.Latitude, Longitude = g.Longitude,
            Address = g.Address, StartTime = g.StartTime?.UtcDateTime, EndTime = g.EndTime?.UtcDateTime,
            EstimatedDuration = g.EstimatedDurationMinutes is > 0 ? TimeSpan.FromMinutes(g.EstimatedDurationMinutes.Value) : null,
            EffortLevel = g.EffortLevel ?? "Moderate", IsAccessible = g.IsAccessible, IsVirtual = g.IsVirtual,
            MaxParticipants = g.MaxParticipants, CurrentParticipants = g.CurrentParticipants, MinimumAge = g.MinimumAge,
            FamilyFriendly = g.FamilyFriendly, IsRecurring = g.IsRecurring, RecurrencePattern = g.RecurrencePattern,
            OrganizationName = g.OrganizationName, OrganizationWebsite = g.OrganizationWebsite,
            ParkingAvailable = g.ParkingAvailable, PublicTransitAccessible = g.PublicTransitAccessible,
            SpecialInstructions = g.SpecialInstructions, ImpactDescription = g.ImpactDescription,
            EstimatedPeopleHelped = g.EstimatedPeopleHelped, Status = g.Status ?? "Active",
            OutdoorActivity = g.OutdoorActivity, WeatherDependent = g.WeatherDependent, CreatedDate = g.CreatedDate.UtcDateTime,
            CreatedBy = g.CreatedBy ?? "",
            ContactName = g.Contact?.Name ?? "", ContactEmail = g.Contact?.Email ?? "", ContactPhone = g.Contact?.Phone ?? "",
            Tags = g.Tags?.Select(t => t.Name).ToList() ?? new(),
            RequiredSkills = g.Skills?.Select(s => s.Name).ToList() ?? new(),
        };
        if (g.Location is { } l && (!string.IsNullOrEmpty(l.City) || !string.IsNullOrEmpty(l.State)))
            m.Address = $"{l.City}, {l.State}, {l.Country}, {l.Zip}".Trim(' ', ',');
        return m;
    }
}

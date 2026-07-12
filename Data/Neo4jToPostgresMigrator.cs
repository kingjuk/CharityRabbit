using Microsoft.EntityFrameworkCore;
using Neo4j.Driver;

namespace CharityRabbit.Data;

/// <summary>
/// One-shot copy of the neo4j graph into the Postgres schema. Invoke with the "migrate-neo4j"
/// argument. Neo4j internal id()s don't survive, so it builds id maps (neo4j id -> new serial id)
/// for Organization and GoodWork and rewrites the relationships through them. Users key off the
/// OIDC subject string, which is stable. Safe to re-run on an empty target.
/// </summary>
public static class Neo4jToPostgresMigrator
{
    public static async Task RunAsync(Neo4jSettings settings, CharityDbContext db)
    {
        using var driver = GraphDatabase.Driver(settings.Uri, AuthTokens.Basic(settings.Username, settings.Password));
        await using var session = driver.AsyncSession();

        var orgMap = new Dictionary<long, long>();
        var gwMap = new Dictionary<long, long>();
        var skills = new Dictionary<string, Skill>();
        var tags = new Dictionary<string, Tag>();
        var contacts = new Dictionary<string, Contact>();
        var locations = new Dictionary<string, Location>();

        // 1) Users (keyed by OIDC subject)
        await ForEach(session, "MATCH (u:User) RETURN u.userId AS id, u.name AS name, u.email AS email, u.phone AS phone", async r =>
        {
            var id = r["id"].As<string?>();
            if (string.IsNullOrEmpty(id) || await db.Users.FindAsync(id) is not null) return;
            db.Users.Add(new User { UserId = id, Name = r["name"].As<string?>(), Email = r["email"].As<string?>(), Phone = r["phone"].As<string?>() });
        });
        await db.SaveChangesAsync();

        // 2) Organizations
        await ForEach(session, "MATCH (o:Organization) RETURN id(o) AS nid, o", async r =>
        {
            var nid = r["nid"].As<long>();
            var o = r["o"].As<INode>();
            var org = new Organization
            {
                Name = Str(o, "name") ?? "", Slug = Str(o, "slug") ?? $"org-{nid}", Description = Str(o, "description") ?? "",
                Mission = Str(o, "mission"), Vision = Str(o, "vision"), ContactEmail = Str(o, "contactEmail") ?? "",
                ContactPhone = Str(o, "contactPhone"), Website = Str(o, "website"), Address = Str(o, "address"),
                City = Str(o, "city"), State = Str(o, "state"), Country = Str(o, "country"), ZipCode = Str(o, "zipCode"),
                Latitude = Dbl(o, "latitude"), Longitude = Dbl(o, "longitude"), OrganizationType = Str(o, "organizationType"),
                TaxId = Str(o, "taxId"), FoundedDate = Dto(o, "foundedDate"), LogoUrl = Str(o, "logoUrl"), CoverImageUrl = Str(o, "coverImageUrl"),
                FacebookUrl = Str(o, "facebookUrl"), TwitterUrl = Str(o, "twitterUrl"), InstagramUrl = Str(o, "instagramUrl"), LinkedInUrl = Str(o, "linkedInUrl"),
                FocusAreas = StrList(o, "focusAreas"), Tags = StrList(o, "tags"), CreatedBy = Str(o, "createdBy"),
                CreatedDate = Dto(o, "createdDate") ?? DateTimeOffset.UtcNow, LastModifiedDate = Dto(o, "lastModifiedDate"),
                Status = Str(o, "status") ?? "Active", IsVerified = Bool(o, "isVerified"),
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
            orgMap[nid] = org.Id;
        });

        // 3) GoodWorks (+ single-valued edges + tags/skills + posting org)
        var gwCypher = @"MATCH (g:GoodWork)
            OPTIONAL MATCH (g)-[:HAS_CONTACT]->(c:Contact)
            OPTIONAL MATCH (g)-[:LOCATED_IN]->(l:Location)
            OPTIONAL MATCH (g)-[:BELONGS_TO]->(cat:Category)
            OPTIONAL MATCH (g)-[:HAS_SUBCATEGORY]->(sub:SubCategory)
            OPTIONAL MATCH (g)-[:POSTED_BY]->(o:Organization)
            RETURN id(g) AS nid, g, c, l, cat.name AS cat, sub.name AS sub, id(o) AS orgNid,
                   [(g)-[:TAGGED_WITH]->(t:Tag) | t.name] AS tags,
                   [(g)-[:REQUIRES_SKILL]->(s:Skill) | s.name] AS skills";
        await ForEach(session, gwCypher, async r =>
        {
            var nid = r["nid"].As<long>();
            var g = r["g"].As<INode>();
            var loc = r["g"].As<INode>();
            var gw = new GoodWork
            {
                Name = Str(g, "name") ?? "", Description = Str(g, "description") ?? "", DetailedDescription = Str(g, "detailedDescription"),
                Category = r["cat"].As<string?>(), SubCategory = r["sub"].As<string?>(), Address = Str(g, "address"),
                Latitude = Point(g, "latitude"), Longitude = Point(g, "longitude"),
                StartTime = Dto(g, "startTime"), EndTime = Dto(g, "endTime"),
                EstimatedDurationMinutes = Dbl(g, "estimatedDuration"), EffortLevel = Str(g, "effortLevel") ?? "Moderate",
                IsAccessible = Bool(g, "isAccessible"), IsVirtual = Bool(g, "isVirtual"),
                MaxParticipants = Int(g, "maxParticipants"), CurrentParticipants = Int(g, "currentParticipants") ?? 0,
                MinimumAge = Int(g, "minimumAge"), FamilyFriendly = Bool(g, "familyFriendly"),
                IsRecurring = Bool(g, "isRecurring"), RecurrencePattern = Str(g, "recurrencePattern"),
                OrganizationName = Str(g, "organizationName"), OrganizationWebsite = Str(g, "organizationWebsite"),
                ParkingAvailable = Bool(g, "parkingAvailable"), PublicTransitAccessible = Bool(g, "publicTransitAccessible"),
                SpecialInstructions = Str(g, "specialInstructions"), ImpactDescription = Str(g, "impactDescription"),
                EstimatedPeopleHelped = Int(g, "estimatedPeopleHelped"), Status = Str(g, "status") ?? "Active",
                OutdoorActivity = Bool(g, "outdoorActivity"), WeatherDependent = Bool(g, "weatherDependent"),
                CreatedDate = Dto(g, "createdDate") ?? DateTimeOffset.UtcNow, CreatedBy = Str(g, "createdBy"),
            };
            if (r["c"].As<INode?>() is { } c && Str(c, "email") is { Length: > 0 } email)
                gw.Contact = GetOrCreate(contacts, email, () => new Contact { Email = email, Name = Str(c, "name"), Phone = Str(c, "phone") });
            if (r["l"].As<INode?>() is { } l)
            {
                var key = $"{Str(l, "city")}|{Str(l, "state")}|{Str(l, "country")}|{Str(l, "zip")}";
                gw.Location = GetOrCreate(locations, key, () => new Location { City = Str(l, "city") ?? "", State = Str(l, "state") ?? "", Country = Str(l, "country") ?? "", Zip = Str(l, "zip") ?? "" });
            }
            foreach (var t in (r["tags"].As<List<object>>() ?? new()).Select(x => x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)))
                gw.Tags.Add(GetOrCreate(tags, t!, () => new Tag { Name = t! }));
            foreach (var s in (r["skills"].As<List<object>>() ?? new()).Select(x => x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)))
                gw.Skills.Add(GetOrCreate(skills, s!, () => new Skill { Name = s! }));
            if (r["orgNid"].As<long?>() is { } onid && orgMap.TryGetValue(onid, out var pgOrg)) gw.OrganizationId = pgOrg;

            db.GoodWorks.Add(gw);
            await db.SaveChangesAsync();
            gwMap[nid] = gw.Id;
        });

        // 4) User -> GoodWork (SIGNED_UP_FOR / INTERESTED_IN)
        await ForEach(session, "MATCH (u:User)-[r]->(g:GoodWork) WHERE type(r) IN ['SIGNED_UP_FOR','INTERESTED_IN'] RETURN u.userId AS uid, id(g) AS gnid, type(r) AS t, r.timestamp AS ts", async r =>
        {
            var uid = r["uid"].As<string?>();
            if (uid is null || !gwMap.TryGetValue(r["gnid"].As<long>(), out var gid)) return;
            if (!await db.Users.AnyAsync(u => u.UserId == uid)) return;
            var ts = Dto(r["ts"]) ?? DateTimeOffset.UtcNow;
            if (r["t"].As<string>() == "SIGNED_UP_FOR")
            {
                if (await db.Signups.FindAsync(uid, gid) is null) db.Signups.Add(new UserSignup { UserId = uid, GoodWorkId = gid, CreatedAt = ts });
            }
            else if (await db.Interested.FindAsync(uid, gid) is null)
                db.Interested.Add(new UserInterested { UserId = uid, GoodWorkId = gid, CreatedAt = ts });
        });
        await db.SaveChangesAsync();

        // 5) User -> Organization (MEMBER_OF / ADMIN_OF)
        await ForEach(session, "MATCH (u:User)-[r]->(o:Organization) WHERE type(r) IN ['MEMBER_OF','ADMIN_OF'] RETURN u.userId AS uid, id(o) AS onid, type(r) AS t, r.role AS role, r.joinedDate AS jd, r.since AS since", async r =>
        {
            var uid = r["uid"].As<string?>();
            if (uid is null || !orgMap.TryGetValue(r["onid"].As<long>(), out var oid)) return;
            if (!await db.Users.AnyAsync(u => u.UserId == uid)) return;
            if (r["t"].As<string>() == "ADMIN_OF")
            {
                if (await db.OrganizationAdmins.FindAsync(uid, oid) is null)
                    db.OrganizationAdmins.Add(new OrganizationAdmin { UserId = uid, OrganizationId = oid, Since = Dto(r["since"]) ?? DateTimeOffset.UtcNow });
            }
            else if (await db.OrganizationMembers.FindAsync(uid, oid) is null)
                db.OrganizationMembers.Add(new OrganizationMember { UserId = uid, OrganizationId = oid, Role = r["role"].As<string?>() ?? "Member", JoinedDate = Dto(r["jd"]) ?? DateTimeOffset.UtcNow });
        });
        await db.SaveChangesAsync();

        // 6) User -> Skill (HAS_SKILL)
        await ForEach(session, "MATCH (u:User)-[:HAS_SKILL]->(s:Skill) RETURN u.userId AS uid, s.name AS skill", async r =>
        {
            var uid = r["uid"].As<string?>();
            var name = r["skill"].As<string?>();
            if (uid is null || string.IsNullOrWhiteSpace(name) || !await db.Users.AnyAsync(u => u.UserId == uid)) return;
            var skill = GetOrCreate(skills, name, () => new Skill { Name = name });
            if (skill.Id == 0) { db.Skills.Add(skill); await db.SaveChangesAsync(); }
            if (await db.UserSkills.FindAsync(uid, skill.Id) is null) db.UserSkills.Add(new UserSkill { UserId = uid, SkillId = skill.Id });
        });
        await db.SaveChangesAsync();
    }

    private static T GetOrCreate<T>(Dictionary<string, T> cache, string key, Func<T> make) where T : class
    {
        if (!cache.TryGetValue(key, out var v)) { v = make(); cache[key] = v; }
        return v;
    }

    private static async Task ForEach(IAsyncSession session, string cypher, Func<IRecord, Task> handle)
    {
        var cursor = await session.RunAsync(cypher);
        while (await cursor.FetchAsync()) await handle(cursor.Current);
    }

    private static string? Str(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true && v is not null ? v.As<string?>() : null;
    private static double? Dbl(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true && v is not null ? v.As<double?>() : null;
    private static double Point(INode? n, string sub) => n?.Properties.TryGetValue("location", out var v) == true && v is Point p ? (sub == "latitude" ? p.Y : p.X) : 0;
    private static int? Int(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true && v is not null ? v.As<int?>() : null;
    private static bool Bool(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true && v is bool b && b;
    private static List<string> StrList(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true && v is not null ? v.As<List<string>>() : new();
    private static DateTimeOffset? Dto(INode? n, string k) => n?.Properties.TryGetValue(k, out var v) == true ? Dto(v) : null;
    private static DateTimeOffset? Dto(object? v) => v switch
    {
        ZonedDateTime z => z.ToDateTimeOffset(),
        LocalDateTime l => new DateTimeOffset(l.ToDateTime(), TimeSpan.Zero),
        _ => null,
    };
}

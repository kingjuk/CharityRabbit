using CharityRabbit.Models;
using Neo4j.Driver;
using System.Text.RegularExpressions;

namespace CharityRabbit.Data;

public class SkillService
{
    private readonly IDriver _driver;

    public SkillService(IDriver driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Normalizes skill name for deduplication (lowercase, trim, remove extra spaces)
    /// </summary>
    private string NormalizeSkillName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        
        // Convert to lowercase, trim, and replace multiple spaces with single space
        var normalized = name.ToLowerInvariant().Trim();
        normalized = Regex.Replace(normalized, @"\s+", " ");
        
        return normalized;
    }

    /// <summary>
    /// Get all skills from database with usage counts
    /// </summary>
    public async Task<List<SkillModel>> GetAllSkillsAsync()
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (s:Skill)
                OPTIONAL MATCH (s)<-[:REQUIRES_SKILL|HAS_SKILL]-(ref)
                WITH s, count(DISTINCT ref) as usageCount
                RETURN s.name as name, 
                       s.description as description, 
                       s.category as category,
                       usageCount
                ORDER BY usageCount DESC, s.name ASC";

            var cursor = await tx.RunAsync(query);
            var skills = new List<SkillModel>();

            await foreach (var record in cursor)
            {
                skills.Add(new SkillModel
                {
                    Name = record["name"].As<string>(),
                    Description = record["description"].As<string?>(),
                    Category = record["category"].As<string?>(),
                    UsageCount = record["usageCount"].As<int>()
                });
            }

            return skills;
        });
    }

    /// <summary>
    /// Get skills grouped by category
    /// </summary>
    public async Task<List<SkillCategoryModel>> GetSkillsByCategoryAsync()
    {
        var allSkills = await GetAllSkillsAsync();
        
        return allSkills
            .GroupBy(s => s.Category ?? "Other")
            .Select(g => new SkillCategoryModel
            {
                Category = g.Key,
                Skills = g.ToList()
            })
            .OrderBy(c => c.Category == "Other" ? "ZZZ" : c.Category) // Put "Other" at end
            .ToList();
    }

    /// <summary>
    /// Get or create a skill, handling deduplication
    /// </summary>
    public async Task<SkillModel> GetOrCreateSkillAsync(string skillName, string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            throw new ArgumentException("Skill name cannot be empty", nameof(skillName));

        var normalizedName = NormalizeSkillName(skillName);
        
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            // First, check if skill exists (case-insensitive)
            var checkQuery = @"
                MATCH (s:Skill)
                WHERE toLower(s.name) = $normalizedName
                OPTIONAL MATCH (s)<-[:REQUIRES_SKILL|HAS_SKILL]-(ref)
                WITH s, count(DISTINCT ref) as usageCount
                RETURN s.name as name, 
                       s.description as description, 
                       s.category as category,
                       usageCount
                LIMIT 1";

            var checkCursor = await tx.RunAsync(checkQuery, new { normalizedName });
            
            if (await checkCursor.FetchAsync())
            {
                // Skill exists, return it
                var record = checkCursor.Current;
                return new SkillModel
                {
                    Name = record["name"].As<string>(),
                    Description = record["description"].As<string?>(),
                    Category = record["category"].As<string?>(),
                    UsageCount = record["usageCount"].As<int>()
                };
            }

            // Skill doesn't exist, create it
            var createQuery = @"
                CREATE (s:Skill {
                    name: $name,
                    description: $description,
                    category: $category,
                    createdDate: datetime()
                })
                RETURN s.name as name, 
                       s.description as description, 
                       s.category as category,
                       0 as usageCount";

            var createCursor = await tx.RunAsync(createQuery, new 
            { 
                name = skillName.Trim(), // Store with original casing
                description, 
                category 
            });

            var newRecord = await createCursor.SingleAsync();
            return new SkillModel
            {
                Name = newRecord["name"].As<string>(),
                Description = newRecord["description"].As<string?>(),
                Category = newRecord["category"].As<string?>(),
                UsageCount = 0
            };
        });
    }

    /// <summary>
    /// Search for skills by partial name match
    /// </summary>
    public async Task<List<SkillModel>> SearchSkillsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllSkillsAsync();

        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (s:Skill)
                WHERE toLower(s.name) CONTAINS toLower($searchTerm)
                   OR toLower(s.description) CONTAINS toLower($searchTerm)
                OPTIONAL MATCH (s)<-[:REQUIRES_SKILL|HAS_SKILL]-(ref)
                WITH s, count(DISTINCT ref) as usageCount
                RETURN s.name as name, 
                       s.description as description, 
                       s.category as category,
                       usageCount
                ORDER BY usageCount DESC, s.name ASC
                LIMIT 50";

            var cursor = await tx.RunAsync(query, new { searchTerm });
            var skills = new List<SkillModel>();

            await foreach (var record in cursor)
            {
                skills.Add(new SkillModel
                {
                    Name = record["name"].As<string>(),
                    Description = record["description"].As<string?>(),
                    Category = record["category"].As<string?>(),
                    UsageCount = record["usageCount"].As<int>()
                });
            }

            return skills;
        });
    }

    /// <summary>
    /// Get predefined skill suggestions organized by category
    /// </summary>
    public Dictionary<string, List<string>> GetPredefinedSkills()
    {
        return new Dictionary<string, List<string>>
        {
            ["Physical"] = new()
            {
                "Manual Labor",
                "Lifting & Moving",
                "Construction",
                "Gardening",
                "Cleaning",
                "Painting",
                "Landscaping",
                "Driving",
                "Sports & Fitness"
            },
            ["Technical"] = new()
            {
                "Computer Skills",
                "Web Development",
                "Graphic Design",
                "Video Editing",
                "Photography",
                "Social Media Management",
                "Data Entry",
                "IT Support",
                "Software Development"
            },
            ["Social"] = new()
            {
                "Public Speaking",
                "Teaching & Tutoring",
                "Customer Service",
                "Event Planning",
                "Team Leadership",
                "Mentoring",
                "Counseling",
                "Networking",
                "Community Outreach"
            },
            ["Creative"] = new()
            {
                "Writing & Editing",
                "Arts & Crafts",
                "Music",
                "Cooking & Baking",
                "Event Decoration",
                "Content Creation",
                "Marketing",
                "Storytelling",
                "Design Thinking"
            },
            ["Administrative"] = new()
            {
                "Organization",
                "Scheduling",
                "Record Keeping",
                "Phone Skills",
                "Email Management",
                "Bookkeeping",
                "Project Management",
                "Filing & Documentation",
                "Office Management"
            },
            ["Healthcare"] = new()
            {
                "First Aid",
                "CPR Certified",
                "Medical Knowledge",
                "Elderly Care",
                "Child Care",
                "Mental Health Support",
                "Nutrition",
                "Physical Therapy",
                "Patient Care"
            },
            ["Language"] = new()
            {
                "Spanish",
                "French",
                "Mandarin",
                "Sign Language",
                "Translation",
                "Multilingual",
                "ESL Teaching",
                "Interpretation"
            },
            ["Specialized"] = new()
            {
                "Legal Knowledge",
                "Financial Planning",
                "Fundraising",
                "Grant Writing",
                "Research",
                "Environmental Science",
                "Animal Care",
                "Emergency Response",
                "Disaster Relief"
            }
        };
    }

    /// <summary>
    /// Initialize database with predefined skills if they don't exist
    /// </summary>
    public async Task InitializePredefinedSkillsAsync()
    {
        var predefinedSkills = GetPredefinedSkills();

        foreach (var category in predefinedSkills)
        {
            foreach (var skillName in category.Value)
            {
                try
                {
                    await GetOrCreateSkillAsync(skillName, category.Key);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing skill '{skillName}': {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Add skill to a user's profile
    /// </summary>
    public async Task<bool> AddUserSkillAsync(string userId, string skillName)
    {
        var skill = await GetOrCreateSkillAsync(skillName);
        
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})
                MATCH (s:Skill)
                WHERE toLower(s.name) = toLower($skillName)
                MERGE (u)-[:HAS_SKILL]->(s)
                RETURN count(*) as count";

            var cursor = await tx.RunAsync(query, new { userId, skillName = skill.Name });
            var record = await cursor.SingleAsync();
            return record["count"].As<int>() > 0;
        });
    }

    /// <summary>
    /// Remove skill from user's profile
    /// </summary>
    public async Task<bool> RemoveUserSkillAsync(string userId, string skillName)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteWriteAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})-[r:HAS_SKILL]->(s:Skill)
                WHERE toLower(s.name) = toLower($skillName)
                DELETE r
                RETURN count(r) as count";

            var cursor = await tx.RunAsync(query, new { userId, skillName });
            var record = await cursor.SingleAsync();
            return record["count"].As<int>() > 0;
        });
    }

    /// <summary>
    /// Get all skills for a user
    /// </summary>
    public async Task<List<string>> GetUserSkillsAsync(string userId)
    {
        await using var session = _driver.AsyncSession();

        return await session.ExecuteReadAsync(async tx =>
        {
            var query = @"
                MATCH (u:User {userId: $userId})-[:HAS_SKILL]->(s:Skill)
                RETURN s.name as name
                ORDER BY s.name ASC";

            var cursor = await tx.RunAsync(query, new { userId });
            var skills = new List<string>();

            await foreach (var record in cursor)
            {
                skills.Add(record["name"].As<string>());
            }

            return skills;
        });
    }
}

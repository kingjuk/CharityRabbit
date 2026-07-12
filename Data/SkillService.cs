using CharityRabbit.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CharityRabbit.Data;

// Skill catalog + user skills, backed by PostgreSQL (EF Core). Public API unchanged.
public class SkillService(CharityDbContext db)
{
    private static string NormalizeSkillName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return Regex.Replace(name.ToLowerInvariant().Trim(), @"\s+", " ");
    }

    // Usage count = distinct entities referencing the skill (good works via REQUIRES_SKILL +
    // users via HAS_SKILL). Materialize then sort in memory (the skill set is small).
    public async Task<List<SkillModel>> GetAllSkillsAsync()
    {
        var skills = await db.Skills.Select(s => new SkillModel
        {
            Name = s.Name,
            Description = s.Description,
            Category = s.Category,
            UsageCount = db.GoodWorks.Count(g => g.Skills.Any(sk => sk.Id == s.Id))
                         + db.UserSkills.Count(us => us.SkillId == s.Id),
        }).ToListAsync();

        return skills.OrderByDescending(s => s.UsageCount).ThenBy(s => s.Name).ToList();
    }

    public async Task<List<SkillCategoryModel>> GetSkillsByCategoryAsync()
    {
        var allSkills = await GetAllSkillsAsync();
        return allSkills
            .GroupBy(s => s.Category ?? "Other")
            .Select(g => new SkillCategoryModel { Category = g.Key, Skills = g.ToList() })
            .OrderBy(c => c.Category == "Other" ? "ZZZ" : c.Category)
            .ToList();
    }

    public async Task<SkillModel> GetOrCreateSkillAsync(string skillName, string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            throw new ArgumentException("Skill name cannot be empty", nameof(skillName));

        var normalized = NormalizeSkillName(skillName);
        var existing = await db.Skills.FirstOrDefaultAsync(s => s.Name.ToLower() == normalized);
        if (existing is not null)
            return new SkillModel
            {
                Name = existing.Name, Description = existing.Description, Category = existing.Category,
                UsageCount = db.GoodWorks.Count(g => g.Skills.Any(sk => sk.Id == existing.Id))
                             + db.UserSkills.Count(us => us.SkillId == existing.Id),
            };

        var skill = new Skill { Name = skillName.Trim(), Category = category, Description = description };
        db.Skills.Add(skill);
        await db.SaveChangesAsync();
        return new SkillModel { Name = skill.Name, Description = skill.Description, Category = skill.Category, UsageCount = 0 };
    }

    public async Task<List<SkillModel>> SearchSkillsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllSkillsAsync();
        var term = searchTerm.ToLower();
        var skills = await db.Skills
            .Where(s => s.Name.ToLower().Contains(term) || (s.Description != null && s.Description.ToLower().Contains(term)))
            .Select(s => new SkillModel
            {
                Name = s.Name, Description = s.Description, Category = s.Category,
                UsageCount = db.GoodWorks.Count(g => g.Skills.Any(sk => sk.Id == s.Id))
                             + db.UserSkills.Count(us => us.SkillId == s.Id),
            }).ToListAsync();
        return skills.OrderByDescending(s => s.UsageCount).ThenBy(s => s.Name).Take(50).ToList();
    }

    public async Task<bool> AddUserSkillAsync(string userId, string skillName)
    {
        var skill = await GetOrCreateSkillAsync(skillName);
        var entity = await db.Skills.FirstAsync(s => s.Name == skill.Name);
        // Ensure the user row exists (provisioned on login; stub-safe).
        if (!await db.Users.AnyAsync(u => u.UserId == userId)) db.Users.Add(new User { UserId = userId });
        if (!await db.UserSkills.AnyAsync(us => us.UserId == userId && us.SkillId == entity.Id))
            db.UserSkills.Add(new UserSkill { UserId = userId, SkillId = entity.Id });
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveUserSkillAsync(string userId, string skillName)
    {
        var name = skillName.ToLower();
        var affected = await db.UserSkills
            .Where(us => us.UserId == userId && db.Skills.Any(s => s.Id == us.SkillId && s.Name.ToLower() == name))
            .ExecuteDeleteAsync();
        return affected > 0;
    }

    public async Task<List<string>> GetUserSkillsAsync(string userId) =>
        await db.UserSkills.Where(us => us.UserId == userId)
            .Join(db.Skills, us => us.SkillId, s => s.Id, (us, s) => s.Name)
            .OrderBy(n => n).ToListAsync();

    public async Task InitializePredefinedSkillsAsync()
    {
        foreach (var category in GetPredefinedSkills())
            foreach (var skillName in category.Value)
                try { await GetOrCreateSkillAsync(skillName, category.Key); }
                catch (Exception ex) { Console.WriteLine($"Error initializing skill '{skillName}': {ex.Message}"); }
    }

    public Dictionary<string, List<string>> GetPredefinedSkills() => new()
    {
        ["Physical"] = new() { "Manual Labor", "Lifting & Moving", "Construction", "Gardening", "Cleaning", "Painting", "Landscaping", "Driving", "Sports & Fitness" },
        ["Technical"] = new() { "Computer Skills", "Web Development", "Graphic Design", "Video Editing", "Photography", "Social Media Management", "Data Entry", "IT Support", "Software Development" },
        ["Social"] = new() { "Public Speaking", "Teaching & Tutoring", "Customer Service", "Event Planning", "Team Leadership", "Mentoring", "Counseling", "Networking", "Community Outreach" },
        ["Creative"] = new() { "Writing & Editing", "Arts & Crafts", "Music", "Cooking & Baking", "Event Decoration", "Content Creation", "Marketing", "Storytelling", "Design Thinking" },
        ["Administrative"] = new() { "Organization", "Scheduling", "Record Keeping", "Phone Skills", "Email Management", "Bookkeeping", "Project Management", "Filing & Documentation", "Office Management" },
        ["Healthcare"] = new() { "First Aid", "CPR Certified", "Medical Knowledge", "Elderly Care", "Child Care", "Mental Health Support", "Nutrition", "Physical Therapy", "Patient Care" },
        ["Language"] = new() { "Spanish", "French", "Mandarin", "Sign Language", "Translation", "Multilingual", "ESL Teaching", "Interpretation" },
        ["Specialized"] = new() { "Legal Knowledge", "Financial Planning", "Fundraising", "Grant Writing", "Research", "Environmental Science", "Animal Care", "Emergency Response", "Disaster Relief" },
    };
}

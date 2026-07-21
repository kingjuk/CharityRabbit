namespace CharityRabbit.Models;

public class SkillModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // e.g., "Physical", "Technical", "Social", "Creative"
    public int UsageCount { get; set; } // How many times this skill is referenced
}

public class SkillCategoryModel
{
    public string Category { get; set; } = string.Empty;
    public List<SkillModel> Skills { get; set; } = new();
}

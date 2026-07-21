using CharityRabbit.Models;

namespace CharityRabbit.Data;

/// <summary>
/// Skill data access shared by the server (EF Core) and mobile (HTTP) implementations.
/// <c>userId</c> parameters are ignored by HTTP implementations (identity from bearer token).
/// </summary>
public interface ISkillService
{
    Task<List<SkillModel>> GetAllSkillsAsync();
    Task<List<SkillCategoryModel>> GetSkillsByCategoryAsync();
    Task<List<SkillModel>> SearchSkillsAsync(string searchTerm);
    Task<bool> AddUserSkillAsync(string userId, string skillName);
    Task<bool> RemoveUserSkillAsync(string userId, string skillName);
    Task<List<string>> GetUserSkillsAsync(string userId);
    Dictionary<string, List<string>> GetPredefinedSkills();
}

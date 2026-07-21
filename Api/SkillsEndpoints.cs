using CharityRabbit.Data;

namespace CharityRabbit.Api;

public static class SkillsEndpoints
{
    public static void MapSkillsApi(this IEndpointRouteBuilder api)
    {
        var skills = api.MapGroup("/skills").WithTags("Skills").AllowAnonymous();

        skills.MapGet("/", async (ISkillService skillService) => await skillService.GetAllSkillsAsync())
            .WithName("GetAllSkills");

        skills.MapGet("/categories", async (ISkillService skillService) => await skillService.GetSkillsByCategoryAsync())
            .WithName("GetSkillsByCategory");

        skills.MapGet("/search", async (string q, ISkillService skillService) => await skillService.SearchSkillsAsync(q))
            .WithName("SearchSkills");
    }
}

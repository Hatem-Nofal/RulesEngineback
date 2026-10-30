using Volo.Abp.Identity;

namespace RulesEngine;

public static class RulesEngineConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;
    public const string AdminEmailDefaultValue = IdentityDataSeedContributor.AdminEmailDefaultValue;
    public static string AdminPasswordDefaultValue =>
        System.Environment.GetEnvironmentVariable("RULESENGINE_ADMIN_PASSWORD")
        ?? throw new System.InvalidOperationException("RULESENGINE_ADMIN_PASSWORD must be configured before seeding users.");
}

using Volo.Abp.Identity;

namespace RulesEngine;

public static class RulesEngineConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;
    public const string AdminEmailDefaultValue = IdentityDataSeedContributor.AdminEmailDefaultValue;
    public const string AdminPasswordDefaultValue = "P@ssw0rd";
}

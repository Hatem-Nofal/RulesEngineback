using Microsoft.EntityFrameworkCore;
using RulesEngine.Entities.WorkFlows;
using RulesEngine.Entities.WorkFlows.Rules;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.Lookups;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace RulesEngine.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class RulesEngineDbContext :
    AbpDbContext<RulesEngineDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */


    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }


    public DbSet<WorkFlow> WorkFlows => Set<WorkFlow>();
    public DbSet<RuleField> RuleFields => Set<RuleField>();
    public DbSet<RuleFormConfig> RuleFormConfigs => Set<RuleFormConfig>();
    public DbSet<RuleFormConfigField> RuleFormConfigFields => Set<RuleFormConfigField>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RuleNode> RuleNodes => Set<RuleNode>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<ActivitySubType> ActivitySubTypes => Set<ActivitySubType>();
    public DbSet<ActivityValidDate> ActivityValidDates => Set<ActivityValidDate>();

    // These entities were already part of the model (reached through RuleDiagnosis / RuleFormConfigField),
    // but ABP only registers IRepository<T> for entities exposed as a DbSet. Without these two lines,
    // IRepository<Diagnosis> and IRepository<Specialities> cannot be resolved (LookupAppService, RulesValidationManager).
    // No migration is needed: the model itself does not change.
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<Specialities> Specialities => Set<Specialities>();

    #endregion

    public RulesEngineDbContext(DbContextOptions<RulesEngineDbContext> options)
         : base(options)
    {

    }



    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);




        builder.Entity<WorkFlow>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Name).IsUnique();

            e.HasOne(x => x.Ruleformconfig)
            .WithMany()
            .HasForeignKey(x => x.RuleformconfigId)
            .OnDelete(DeleteBehavior.Restrict);

        });

        builder.Entity<RuleField>(e =>
        {
            e.Property(x => x.Key).IsRequired().HasMaxLength(128);
            e.Property(x => x.Label).IsRequired().HasMaxLength(200);
            e.Property(x => x.DataType).IsRequired().HasMaxLength(32);
            e.HasIndex(x => x.Key).IsUnique();
        });

        builder.Entity<RuleFormConfig>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.StrategyKey).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();

            e.HasMany(x => x.RuleFormConfigFields).WithOne().HasForeignKey(x => x.RuleformConfigId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RuleFormConfigField>(e =>
        {
            e.HasIndex(x => new { x.RuleformConfigId, x.RulefieldId })
                .IsUnique();

            e.HasOne(x => x.RuleformConfig)
                .WithMany(x=>x.RuleFormConfigFields)
                .HasForeignKey(x => x.RuleformConfigId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Rulefield)
                .WithMany()
                .HasForeignKey(x => x.RulefieldId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Rule>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.HasMany(x => x.Nodes).WithOne().HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Workflow)
       .WithMany(x => x.Rules)
       .HasForeignKey(x => x.WorkflowId)
       .IsRequired()
       .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RuleNode>(e =>
        {
            e.Property(x => x.FieldKey).HasMaxLength(128);
            // NoAction: checked at end of statement, so cascade/bulk deletes of a whole tree succeed.
            e.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Rule)
            .WithMany(x=>x.Nodes)
            .HasForeignKey(x => x.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
        });
        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        //builder.Entity<YourEntity>(builder =>
        //{
        //    builder.ToTable(RulesEngineConsts.DbTablePrefix + "YourEntities", RulesEngineConsts.DbSchema);
        //    builder.ConfigureByConvention(); //auto configure for the builder class props
        //    //...
        //});
    }
}
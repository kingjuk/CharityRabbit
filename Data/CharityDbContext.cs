using Microsoft.EntityFrameworkCore;

namespace CharityRabbit.Data;

public class CharityDbContext(DbContextOptions<CharityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<GoodWork> GoodWorks => Set<GoodWork>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<UserInterested> Interested => Set<UserInterested>();
    public DbSet<UserSignup> Signups => Set<UserSignup>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrganizationAdmin> OrganizationAdmins => Set<OrganizationAdmin>();
    public DbSet<UserFriend> Friends => Set<UserFriend>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().ToTable("users").HasKey(x => x.UserId);
        b.Entity<Contact>(e => { e.ToTable("contacts"); e.HasIndex(x => x.Email).IsUnique(); });
        b.Entity<Location>(e => { e.ToTable("locations"); e.HasIndex(x => new { x.City, x.State, x.Country, x.Zip }).IsUnique(); });
        b.Entity<Skill>(e => { e.ToTable("skills"); e.HasIndex(x => x.Name); }); // case-insensitive dedup enforced in code + lower() index below
        b.Entity<Tag>(e => { e.ToTable("tags"); e.HasIndex(x => x.Name).IsUnique(); });

        b.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Status);
        });

        b.Entity<GoodWork>(e =>
        {
            e.ToTable("good_works");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.StartTime);
            e.HasIndex(x => x.CreatedBy);
            e.HasIndex(x => x.OrganizationId);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasMany(x => x.Tags).WithMany().UsingEntity(j => j.ToTable("good_work_tags"));
            e.HasMany(x => x.Skills).WithMany().UsingEntity(j => j.ToTable("good_work_skills"));
        });

        b.Entity<UserSkill>(e =>
        {
            e.ToTable("user_skills"); e.HasKey(x => new { x.UserId, x.SkillId });
        });
        b.Entity<UserInterested>(e =>
        {
            e.ToTable("user_interested"); e.HasKey(x => new { x.UserId, x.GoodWorkId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GoodWork).WithMany().HasForeignKey(x => x.GoodWorkId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<UserSignup>(e =>
        {
            e.ToTable("user_signups"); e.HasKey(x => new { x.UserId, x.GoodWorkId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.GoodWork).WithMany().HasForeignKey(x => x.GoodWorkId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<OrganizationMember>(e =>
        {
            e.ToTable("organization_members"); e.HasKey(x => new { x.UserId, x.OrganizationId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<OrganizationAdmin>(e =>
        {
            e.ToTable("organization_admins"); e.HasKey(x => new { x.UserId, x.OrganizationId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<UserFriend>().ToTable("user_friends").HasKey(x => new { x.UserId, x.FriendUserId });
    }
}

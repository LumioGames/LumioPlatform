using Microsoft.EntityFrameworkCore;

namespace Lumio.Platform.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountCredential> AccountCredentials => Set<AccountCredential>();
    public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<TrackedEvent> Events => Set<TrackedEvent>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasSequence<long>("account_uid_seq").StartsAt(100000);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.AccountId).HasColumnName("account_id").HasMaxLength(37).IsRequired();
            entity.Property(value => value.Uid).HasColumnName("uid").HasDefaultValueSql("nextval('account_uid_seq')").ValueGeneratedOnAdd();
            entity.Property(value => value.LoginName).HasColumnName("login_name").HasMaxLength(32).IsRequired();
            entity.Property(value => value.Email).HasColumnName("email").HasMaxLength(254);
            entity.Property(value => value.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.AvatarId).HasColumnName("avatar_id");
            entity.Property(value => value.Role).HasColumnName("role").IsRequired();
            entity.Property(value => value.Status).HasColumnName("status").IsRequired();
            entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(value => value.AccountId).IsUnique();
            entity.HasIndex(value => value.Uid).IsUnique();
            entity.HasIndex(value => value.LoginName).IsUnique();
            entity.HasIndex(value => value.Email).IsUnique();
        });

        modelBuilder.Entity<AccountCredential>(entity =>
        {
            entity.ToTable("account_credentials");
            entity.HasKey(value => value.AccountId);
            entity.Property(value => value.AccountId).HasColumnName("account_id").ValueGeneratedNever();
            entity.Property(value => value.Argon2idHash).HasColumnName("argon2id_hash").IsRequired();
            entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.HasOne<Account>().WithOne().HasForeignKey<AccountCredential>(value => value.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.ToTable("email_verifications");
            entity.HasKey(value => value.Email);
            entity.Property(value => value.Email).HasColumnName("email").HasMaxLength(254);
            entity.Property(value => value.CodeHash).HasColumnName("code_hash").HasMaxLength(64).IsRequired();
            entity.Property(value => value.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.Attempts).HasColumnName("attempts");
            entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.ToTable("login_attempts");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.AccountId).HasColumnName("account_id");
            entity.Property(value => value.Identifier).HasColumnName("identifier").IsRequired();
            entity.Property(value => value.Port).HasColumnName("port").IsRequired();
            entity.Property(value => value.Outcome).HasColumnName("outcome").IsRequired();
            entity.Property(value => value.ErrorCode).HasColumnName("error_code");
            entity.Property(value => value.Ip).HasColumnName("ip");
            entity.Property(value => value.UserAgent).HasColumnName("user_agent");
            entity.Property(value => value.At).HasColumnName("at").HasColumnType("timestamp with time zone");
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.ToTable("games");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Slug).HasColumnName("slug").IsRequired();
            entity.Property(value => value.Name).HasColumnName("name").IsRequired();
            entity.Property(value => value.Summary).HasColumnName("summary").IsRequired();
            entity.Property(value => value.CoverUrl).HasColumnName("cover_url").IsRequired();
            entity.Property(value => value.Status).HasColumnName("status").IsRequired();
            entity.Property(value => value.BundleDir).HasColumnName("bundle_dir").IsRequired();
            entity.Property(value => value.ServerWsUrl).HasColumnName("server_ws_url").IsRequired();
            entity.Property(value => value.Subprotocol).HasColumnName("subprotocol").IsRequired();
            entity.Property(value => value.ContractId).HasColumnName("contract_id").IsRequired();
            entity.Property(value => value.SortOrder).HasColumnName("sort_order");
            entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(value => value.Slug).IsUnique();
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedbacks");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Type).HasColumnName("type").IsRequired();
            entity.Property(value => value.Title).HasColumnName("title").HasMaxLength(80).IsRequired();
            entity.Property(value => value.Body).HasColumnName("body").HasMaxLength(4000).IsRequired();
            entity.Property(value => value.GameSlug).HasColumnName("game_slug");
            entity.Property(value => value.PageUrl).HasColumnName("page_url");
            entity.Property(value => value.Contact).HasColumnName("contact").HasMaxLength(120);
            entity.Property(value => value.AccountId).HasColumnName("account_id");
            entity.Property(value => value.Status).HasColumnName("status").IsRequired();
            entity.Property(value => value.AdminNote).HasColumnName("admin_note");
            entity.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TrackedEvent>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Name).HasColumnName("name").HasMaxLength(64).IsRequired();
            entity.Property(value => value.Props).HasColumnName("props").HasColumnType("jsonb");
            entity.Property(value => value.AccountId).HasColumnName("account_id");
            entity.Property(value => value.AnonId).HasColumnName("anon_id").IsRequired();
            entity.Property(value => value.ClientTs).HasColumnName("client_ts").HasColumnType("timestamp with time zone");
            entity.Property(value => value.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamp with time zone");
            entity.Property(value => value.PageUrl).HasColumnName("page_url");
            entity.Property(value => value.UserAgent).HasColumnName("user_agent");
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlatformSetting>(entity =>
        {
            entity.ToTable("platform_settings");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Key).HasColumnName("key").IsRequired();
            entity.Property(value => value.Value).HasColumnName("value").IsRequired();
            entity.Property(value => value.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
            entity.HasIndex(value => value.Key).IsUnique();
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("audit_log");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.ActorAccountId).HasColumnName("actor_account_id");
            entity.Property(value => value.Action).HasColumnName("action").IsRequired();
            entity.Property(value => value.Target).HasColumnName("target").IsRequired();
            entity.Property(value => value.Before).HasColumnName("before").HasColumnType("jsonb");
            entity.Property(value => value.After).HasColumnName("after").HasColumnType("jsonb");
            entity.Property(value => value.At).HasColumnName("at").HasColumnType("timestamp with time zone");
            entity.HasOne<Account>().WithMany().HasForeignKey(value => value.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

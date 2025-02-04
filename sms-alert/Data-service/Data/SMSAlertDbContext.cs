using Entities_Dtos.DBSets;
using Entities_Dtos.Types;
namespace Data_service.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class SMSAlertDbContext : IdentityDbContext
{

    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAccount> CustomerAccounts { get; set; }
    public DbSet<BatchChargeEntry> BatchCharges { get; set; }
    public DbSet<BatchChargeArchive> BatchChargeArchives { get; set; }
    public DbSet<SMSAlert> SMSAlerts { get; set; }
    public DbSet<DirectDebitQueue> DirectDebitQueues { get; set; }
    public DbSet<AccountingEntry> AccountingEntries { get; set; }
    public DbSet<QuickBalanceEnquiry> QuickBalanceEnquiries { get; set; }
    public DbSet<AccountTransaction> AccountTransactions { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }


    public SMSAlertDbContext(DbContextOptions<SMSAlertDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        // Customer Configurations
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PhoneNumber).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(5).HasDefaultValue("en");
            entity.Property(e => e.IsBlacklisted).HasDefaultValue(false);

            // Configure relationships
            entity.HasMany(c => c.Accounts)
                  .WithOne(a => a.Customer)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.SMSAlerts)
                  .WithOne(a => a.Customer)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(c => c.DebitQueues)
                  .WithOne(d => d.Customer)
                  .HasForeignKey(d => d.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Customize Identity table names
        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("Roles");  // Instead of AspNetRoles
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");  // Instead of AspNetUserRoles
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");  // Instead of AspNetUserClaims
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");  // Instead of AspNetUserLogins
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");  // Instead of AspNetUserTokens
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");  // Instead of AspNetRoleClaims
        });

        // CustomerAccount Configurations
        builder.Entity<CustomerAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).IsRequired();
            entity.Property(e => e.BranchSolId).IsRequired();
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.HasIndex(e => e.AccountNumber).IsUnique();

            // Configure relationships
            entity.HasMany(a => a.Transactions)
                  .WithOne(t => t.Account)
                  .HasForeignKey(t => t.CustomerAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.IsDomiciliaryAccount)
          .HasDefaultValue(false);

            entity.Property(e => e.LinkedNigerianAccountNumber)
                  .HasMaxLength(20)
                  .IsRequired(false);

            // Configure enums as strings
            entity.Property(e => e.AccountType)
                      .HasConversion(
                          v => v.ToString(),
                          v => (AccountType)Enum.Parse(typeof(AccountType), v));

            entity.Property(e => e.CurrencyType)
                  .HasConversion(
                      v => v.ToString(),
                      v => (CurrencyType)Enum.Parse(typeof(CurrencyType), v));
        });

        // SMSAlert Configurations
        builder.Entity<SMSAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageContent).IsRequired();
            entity.Property(e => e.ChargeAmount).HasPrecision(18, 2);
            entity.Property(e => e.VATAmount).HasPrecision(18, 2);

            entity.Property(e => e.AlertType)
                       .HasConversion(
                           v => v.ToString(),
                           v => (AlertType)Enum.Parse(typeof(AlertType), v));

            entity.Property(e => e.DeliveryStatus)
                  .HasConversion(
                      v => v.ToString(),
                      v => (DeliveryStatus)Enum.Parse(typeof(DeliveryStatus), v));


            // Configure optional relationship with CustomerAccount
            entity.HasOne(s => s.Account)
                  .WithMany()
                  .HasForeignKey(s => s.CustomerAccountId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DirectDebitQueue Configurations
        builder.Entity<DirectDebitQueue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalChargeAmount).HasPrecision(18, 2);
            entity.Property(e => e.RetryCount).HasDefaultValue(0);

            entity.Property(e => e.FailureReason).HasDefaultValue("");

            entity.Property(e => e.Status)
                     .HasConversion(
                         v => v.ToString(),
                         v => (QueueStatus)Enum.Parse(typeof(QueueStatus), v));

            // Configure relationships
            entity.HasOne(d => d.SMSAlert)
                  .WithOne(s => s.DebitQueue)
                  .HasForeignKey<DirectDebitQueue>(d => d.SMSAlertId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SourceAccount)
                  .WithMany()
                  .HasForeignKey(d => d.SourceAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AccountingEntry Configurations
        builder.Entity<AccountingEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DebitAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreditAmount).HasPrecision(18, 2);
            entity.Property(e => e.VATAmount).HasPrecision(18, 2);
            entity.Property(e => e.TransactionReference).IsRequired();

            entity.Property(e => e.EntryType)
                     .HasConversion(
                         v => v.ToString(),
                         v => (EntryType)Enum.Parse(typeof(EntryType), v));
        });

        // QuickBalanceEnquiry Configurations
        builder.Entity<QuickBalanceEnquiry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChargeAmount).HasPrecision(18, 2);
            entity.Property(e => e.SessionCharge).HasPrecision(18, 2);

            entity.Property(e => e.TelcoProvider)
                       .HasConversion(
                           v => v.ToString(),
                           v => (TelcoProvider)Enum.Parse(typeof(TelcoProvider), v));

            // Configure relationships
            entity.HasOne(q => q.Account)
                  .WithMany()
                  .HasForeignKey(q => q.CustomerAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AccountTransaction Configurations
        builder.Entity<AccountTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.TransactionReference).IsRequired();

            entity.Property(e => e.TransactionType)
                      .HasConversion(
                          v => v.ToString(),
                          v => (TransactionType)Enum.Parse(typeof(TransactionType), v));
        });

        //SystemCongiguration Configurations
        builder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigKey).IsRequired();
            entity.Property(e => e.ConfigValue).IsRequired();
            entity.Property(e => e.LastModifiedBy).IsRequired();
            entity.HasIndex(e => e.ConfigKey).IsUnique();
        });

        // Add any indexes needed for performance
        builder.Entity<SMSAlert>().HasIndex(e => e.DeliveryTimestamp);
        builder.Entity<DirectDebitQueue>().HasIndex(e => e.Status);
        builder.Entity<AccountTransaction>().HasIndex(e => e.TransactionReference);
        builder.Entity<CustomerAccount>().HasIndex(e => new { e.CustomerId, e.AccountType });
    }
}

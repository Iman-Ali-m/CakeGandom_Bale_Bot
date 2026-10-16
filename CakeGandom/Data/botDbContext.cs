using CakeGandom.Models;
using Microsoft.EntityFrameworkCore;

namespace CakeGandom.Data;

public class BotDbContext:DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options):base(options){}
    public DbSet<User> Users=>Set<User>();
    public DbSet<Profile> Profiles=>Set<Profile>();
    public DbSet<MenuButton> MenuButtons=>Set<MenuButton>();
    public DbSet<CakeItem> CakeItems=>Set<CakeItem>();
    public DbSet<CakeMedia> CakeMedias=>Set<CakeMedia>();
    public DbSet<UserSession> UserSessions=>Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e=>
        {
            e.HasKey(x=>x.UserId);
            e.Property(x=>x.UserId).HasMaxLength(64);
            e.Property(x=>x.BaleUsername).HasMaxLength(128);
            e.HasOne(x=>x.Profile).WithOne(x=>x.User!).HasForeignKey<Profile>(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<Profile>(e=>
        {
            e.HasKey(x=>x.Id);
            e.Property(x=>x.UserId).HasMaxLength(64).IsRequired();
            e.HasIndex(x=>x.UserId).IsUnique();
            e.Property(x=>x.FirstName).HasMaxLength(64);
            e.Property(x=>x.LastName).HasMaxLength(64);
            e.Property(x=>x.PhoneEncrypted).HasMaxLength(512);
            e.Property(x=>x.BaleUsernameContact).HasMaxLength(128);
            e.Property(x=>x.RegistrationStep).HasMaxLength(32);
        });
        mb.Entity<MenuButton>(e=>
        {
            e.HasKey(x=>x.Id);
            e.Property(x=>x.Title).HasMaxLength(128).IsRequired();
            e.Property(x=>x.CallbackData).HasMaxLength(128);
            e.HasOne(x=>x.Parent).WithMany(x=>x.Children).HasForeignKey(x=>x.ParentId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.Level,x.ParentId,x.Position});
        });
        mb.Entity<CakeItem>(e=>
        {
            e.HasKey(x=>x.Id);
            e.Property(x=>x.Title).HasMaxLength(128).IsRequired();
            e.HasOne(x=>x.MenuButton).WithMany(x=>x.CakeItems).HasForeignKey(x=>x.MenuButtonId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x=>new{x.MenuButtonId,x.Position});
        });
        mb.Entity<CakeMedia>(e=>
        {
            e.HasKey(x=>x.Id);
            e.Property(x=>x.PhotoPath).HasMaxLength(512).IsRequired();
            e.HasOne(x=>x.CakeItem).WithMany(x=>x.Medias).HasForeignKey(x=>x.CakeItemId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<UserSession>(e=>
        {
            e.HasKey(x=>x.UserId);
            e.Property(x=>x.UserId).HasMaxLength(64);
            e.Property(x=>x.State).HasMaxLength(64);
        });
        mb.Entity<BotSetting>(e=>
        {
            e.HasKey(x=>x.Key);
            e.Property(x=>x.Key).HasMaxLength(64);
        });
        mb.Entity<BotSetting>().HasData(
            new BotSetting{Key="help_text",Value="راهنمای ربات:\nاز دکمه ثبت نام برای تکمیل پروفایل استفاده کنید\nاز منوی کیک می‌توانید کیک‌های موجود را ببینید\nدر صورت نیاز از دکمه ارتباط با ما استفاده کنید"},
            new BotSetting{Key="contact_text",Value="برای ارتباط با ما به آیدی زیر پیام دهید:\n@Pyton_lerning"}
        );
        SeedMenu(mb);
    }

public DbSet<BotSetting> BotSettings=>Set<BotSetting>();

    private void SeedMenu(ModelBuilder mb)
    {
        mb.Entity<MenuButton>().HasData(
            new MenuButton{Id=1,Level=1,ParentId=null,Title="ثبت نام",CallbackData="menu_register",Position=1,IsSuperOnly=false,IsActive=true,CreatedAt=0},
            new MenuButton{Id=2,Level=1,ParentId=null,Title="منوی کیک",CallbackData="menu_cakes",Position=2,IsSuperOnly=false,IsActive=true,CreatedAt=0},
            new MenuButton{Id=3,Level=1,ParentId=null,Title="پروفایل",CallbackData="menu_profile",Position=3,IsSuperOnly=false,IsActive=true,CreatedAt=0},
            new MenuButton{Id=4,Level=1,ParentId=null,Title="راهنما",CallbackData="menu_help",Position=4,IsSuperOnly=false,IsActive=true,CreatedAt=0},
            new MenuButton{Id=5,Level=1,ParentId=null,Title="ارتباط با ما",CallbackData="menu_contact",Position=5,IsSuperOnly=false,IsActive=true,CreatedAt=0},
            new MenuButton{Id=6,Level=1,ParentId=null,Title="⬇️⬇️⬇️پنل کاربری⬇️⬇️⬇️",CallbackData="menu_user_panel",Position=6,IsSuperOnly=true,IsActive=true,CreatedAt=0}
        );
    }
}
using CakeGandom.Data;
using CakeGandom.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CakeGandom.Services;

public class UserService
{
    private readonly BotDbContext _db;
    private readonly IConfiguration _cfg;

    public UserService(BotDbContext db,IConfiguration cfg)
    {
        _db=db;
        _cfg=cfg;
    }

    public bool IsSuperUser(string userId)
    {
        var main=_cfg["SuperUsers:MainUserId"];
        if(string.IsNullOrEmpty(main)) return false;
        return main.Split(',',StringSplitOptions.RemoveEmptyEntries)
            .Select(x=>x.Trim())
            .Contains(userId);
    }
    public bool IsAdmin(string userId)
    {
        var arr=_cfg.GetSection("SuperUsers:AdminIds").Get<string[]>();
        return arr!=null&&arr.Contains(userId);
    }

    public bool IsPrivileged(string userId)=>IsSuperUser(userId)||IsAdmin(userId);

    public async Task<bool> RegisterOrUpdateAsync(string userId,string? baleUsername,string? firstName,string? lastName)
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
        bool isNew;
        if(user==null)
        {
            user=new User
            {
                UserId=userId,
                BaleUsername=baleUsername,
                FirstSeen=now,
                LastSeen=now,
                IsRegistered=false,
                IsBlocked=false,
                IsSuperUser=IsSuperUser(userId),
                IsAdmin=IsAdmin(userId),
                CreatedAt=now
            };
            _db.Users.Add(user);
            _db.Profiles.Add(new Profile
            {
                UserId=userId,
                FirstName=firstName,
                LastName=lastName,
                RegistrationStep="start",
                CreatedAt=now,
                UpdatedAt=now
            });
            isNew=true;
        }
        else
        {
            user.LastSeen=now;
            if(!string.IsNullOrEmpty(baleUsername)) user.BaleUsername=baleUsername;
            user.IsSuperUser=IsSuperUser(userId);
            user.IsAdmin=IsAdmin(userId);
            isNew=false;
        }
        await _db.SaveChangesAsync();
        return isNew;
    }

    public async Task UpdateLastSeenAsync(string userId)
    {
        var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(user==null) return;
        user.LastSeen=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserAsync(string userId)
        =>await _db.Users.Include(x=>x.Profile).FirstOrDefaultAsync(x=>x.UserId==userId);

    public async Task<List<User>> GetRegisteredUsersAsync()
        =>await _db.Users.Include(x=>x.Profile)
            .Where(x=>x.IsRegistered)
            .OrderBy(x=>x.FirstSeen)
            .ToListAsync();

    public async Task SetBlockedAsync(string userId,bool blocked)
    {
        var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(user==null) return;
        user.IsBlocked=blocked;
        await _db.SaveChangesAsync();
    }

    public async Task<string> GetSessionStateAsync(string userId)
    {
        var s=await _db.UserSessions.FirstOrDefaultAsync(x=>x.UserId==userId);
        return s?.State??"idle";
    }

    public async Task<string?> GetSessionDataAsync(string userId)
    {
        var s=await _db.UserSessions.FirstOrDefaultAsync(x=>x.UserId==userId);
        return s?.TempData;
    }

    public async Task SetSessionAsync(string userId,string state,string? tempData=null)
    {
        var s=await _db.UserSessions.FirstOrDefaultAsync(x=>x.UserId==userId);
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if(s==null)
        {
            s=new UserSession{UserId=userId,State=state,TempData=tempData,UpdatedAt=now};
            _db.UserSessions.Add(s);
        }
        else
        {
            s.State=state;
            s.TempData=tempData;
            s.UpdatedAt=now;
        }
        await _db.SaveChangesAsync();
    }

    public async Task ClearSessionAsync(string userId)
    {
        var s=await _db.UserSessions.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(s==null) return;
        _db.UserSessions.Remove(s);
        await _db.SaveChangesAsync();
    }
}
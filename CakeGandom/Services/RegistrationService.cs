using System.Text;
using CakeGandom.Data;
using CakeGandom.Models;
using Microsoft.EntityFrameworkCore;

namespace CakeGandom.Services;

public class RegistrationService
{
    private readonly BotDbContext _db;
    private readonly PhoneProtector _protector;

    public RegistrationService(BotDbContext db,PhoneProtector protector)
    {
        _db=db;
        _protector=protector;
    }

    public async Task<bool> IsCompleteAsync(string userId)
    {
        var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
        return user!=null&&user.IsRegistered;
    }

    public async Task<string> GetStepAsync(string userId)
    {
        var p=await _db.Profiles.FirstOrDefaultAsync(x=>x.UserId==userId);
        return p?.RegistrationStep??"start";
    }

    public async Task ResetAsync(string userId)
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var p=await _db.Profiles.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(p==null)
        {
            _db.Profiles.Add(new Profile
            {
                UserId=userId,
                RegistrationStep="first_name",
                CreatedAt=now,
                UpdatedAt=now
            });
        }
        else
        {
            p.RegistrationStep="first_name";
            p.UpdatedAt=now;
        }
        var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(user!=null) user.IsRegistered=false;
        await _db.SaveChangesAsync();
    }

    public async Task<(bool ok,string message)> ProcessAsync(string userId,string text)
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var p=await _db.Profiles.FirstOrDefaultAsync(x=>x.UserId==userId);
        if(p==null)
        {
            p=new Profile
            {
                UserId=userId,
                RegistrationStep="first_name",
                CreatedAt=now,
                UpdatedAt=now
            };
            _db.Profiles.Add(p);
            await _db.SaveChangesAsync();
        }

        var step=p.RegistrationStep;
        switch(step)
        {
            case "first_name":
                if(text.Trim().Length<2)
                    return (false,"نام باید حداقل ۲ حرف باشد. دوباره وارد کنید:");
                p.FirstName=text.Trim();
                p.RegistrationStep="last_name";
                p.UpdatedAt=now;
                await _db.SaveChangesAsync();
                return (true,"نام ثبت شد\nنام خانوادگی خود را وارد کنید:");

            case "last_name":
                if(text.Trim().Length<2)
                    return (false,"نام خانوادگی باید حداقل ۲ حرف باشد. دوباره وارد کنید:");
                p.LastName=text.Trim();
                p.RegistrationStep="phone";
                p.UpdatedAt=now;
                await _db.SaveChangesAsync();
                return (true,"نام خانوادگی ثبت شد\nشماره تماس خود را وارد کنید:");

            case "phone":
                var digits=new string(text.Where(char.IsDigit).ToArray());
                if(digits.Length<10)
                    return (false,"شماره تماس نامعتبر است. دوباره وارد کنید:");
                p.PhoneEncrypted=_protector.Encrypt(digits);
                p.RegistrationStep="bale_username";
                p.UpdatedAt=now;
                await _db.SaveChangesAsync();
                return (true,"شماره تماس ثبت شد\nآیدی یا شماره پیام‌رسان بله خود را وارد کنید:");

            case "bale_username":
                var uname=text.Trim();
                if(uname.Length<3)
                    return (false,"آیدی معتبر وارد کنید (حداقل ۳ کاراکتر):");
                p.BaleUsernameContact=uname;
                p.RegistrationStep="complete";
                p.UpdatedAt=now;
                var user=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==userId);
                if(user!=null) user.IsRegistered=true;
                await _db.SaveChangesAsync();
                return (true,"ثبت‌نام شما با موفقیت تکمیل شد\n");

            default:
                return (false,"وضعیت نامعتبر. لطفاً از منوی اصلی ثبت نام را دوباره شروع کنید");
        }
    }

    public async Task<string> BuildProfileSummaryAsync(string userId)
    {
        var u=await _db.Users.Include(x=>x.Profile).FirstOrDefaultAsync(x=>x.UserId==userId);
        if(u==null) return "کاربر یافت نشد";
        var p=u.Profile;
        var phone=p!=null?_protector.Decrypt(p.PhoneEncrypted??string.Empty):string.Empty;
        var sb=new StringBuilder();
        sb.AppendLine("پروفایل شما");
        sb.AppendLine($"نام: {(string.IsNullOrEmpty(p?.FirstName)?"وارد نشده":p!.FirstName)}");
        sb.AppendLine($"نام خانوادگی: {(string.IsNullOrEmpty(p?.LastName)?"وارد نشده":p!.LastName)}");
        sb.AppendLine($"شماره تماس: {(string.IsNullOrEmpty(phone)?"وارد نشده":phone)}");
        sb.AppendLine($"آیدی بله: {(string.IsNullOrEmpty(p?.BaleUsernameContact)?"وارد نشده":p!.BaleUsernameContact)}");
        if(u.IsSuperUser) sb.AppendLine("نقش: کاربر اصلی");
        else if(u.IsAdmin) sb.AppendLine("نقش: ادمین");
        return sb.ToString().TrimEnd();
    }

    public async Task<string> BuildUsersExportAsync()
    {
        var users=await _db.Users.Include(x=>x.Profile)
            .Where(x=>x.IsRegistered)
            .OrderBy(x=>x.FirstSeen)
            .ToListAsync();
        if(users.Count==0) return "هیچ کاربر ثبت‌نام‌شده‌ای وجود ندارد";
        var sb=new StringBuilder();
        sb.AppendLine($"تعداد کاربران ثبت‌نام‌شده: {users.Count}");
        sb.AppendLine("--------------------------------");
        int i=1;
        foreach(var u in users)
        {
            var p=u.Profile;
            var phone=p!=null?_protector.Decrypt(p.PhoneEncrypted??string.Empty):string.Empty;
            sb.AppendLine($"{i}. {p?.FirstName??"-"} {p?.LastName??"-"} | تماس: {phone} | آیدی: {p?.BaleUsernameContact??"-"} | شناسه: {u.UserId}");
            i++;
        }
        return sb.ToString().TrimEnd();
    }
}
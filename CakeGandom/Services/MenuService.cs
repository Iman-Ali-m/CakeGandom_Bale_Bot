using CakeGandom.Data;
using CakeGandom.Models;
using Microsoft.EntityFrameworkCore;

namespace CakeGandom.Services;

public class MenuService
{
    private readonly BotDbContext _db;
    private readonly UserService _users;

    public MenuService(BotDbContext db,UserService users)
    {
        _db=db;
        _users=users;
    }

    public async Task<bool> IsPrivilegedAsync(string userId)
        =>await Task.FromResult(_users.IsPrivileged(userId));

    public async Task<List<MenuButton>> GetButtonsByLevelAsync(int level,int? parentId,string userId)
    {
        bool priv=_users.IsPrivileged(userId);
        return await _db.MenuButtons
            .Where(x=>x.Level==level&&x.ParentId==parentId&&x.IsActive&&(!x.IsSuperOnly||priv))
            .OrderBy(x=>x.Position)
            .ToListAsync();
    }

    public async Task<List<MenuButton>> GetAllButtonsByLevelAsync(int level,int? parentId)
        =>await _db.MenuButtons
            .Where(x=>x.Level==level&&x.ParentId==parentId)
            .OrderBy(x=>x.Position)
            .ToListAsync();

    public async Task<MenuButton?> GetButtonAsync(int id)
        =>await _db.MenuButtons.FirstOrDefaultAsync(x=>x.Id==id);

    public async Task<MenuButton> AddButtonAsync(int level,int? parentId,string title,int position,bool isSuperOnly,string? callbackData=null)
    {
        var siblings=await _db.MenuButtons
            .Where(x=>x.Level==level&&x.ParentId==parentId)
            .ToListAsync();
        int maxPos=siblings.Count==0?0:siblings.Max(x=>x.Position);
        if(position<1) position=1;
        if(position>maxPos+1) position=maxPos+1;
        foreach(var s in siblings.Where(x=>x.Position>=position)) s.Position+=1;
        var btn=new MenuButton
        {
            Level=level,
            ParentId=parentId,
            Title=title,
            CallbackData=callbackData,
            Position=position,
            IsSuperOnly=isSuperOnly,
            IsActive=true,
            CreatedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.MenuButtons.Add(btn);
        await _db.SaveChangesAsync();
        if(string.IsNullOrEmpty(btn.CallbackData))
        {
            btn.CallbackData=$"mnu:{btn.Id}";
            await _db.SaveChangesAsync();
        }
        return btn;
    }

    public async Task<bool> EditButtonAsync(int id,string? title=null,string? callbackData=null,bool? isSuperOnly=null)
    {
        var btn=await _db.MenuButtons.FirstOrDefaultAsync(x=>x.Id==id);
        if(btn==null) return false;
        if(!string.IsNullOrEmpty(title)) btn.Title=title;
        if(!string.IsNullOrEmpty(callbackData)) btn.CallbackData=callbackData;
        if(isSuperOnly.HasValue) btn.IsSuperOnly=isSuperOnly.Value;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteButtonAsync(int id)
    {
        var btn=await _db.MenuButtons.FirstOrDefaultAsync(x=>x.Id==id);
        if(btn==null) return false;
        await DeleteChildrenRecursiveAsync(id);
        int level=btn.Level;
        int? parentId=btn.ParentId;
        int pos=btn.Position;
        _db.MenuButtons.Remove(btn);
        var siblings=await _db.MenuButtons
            .Where(x=>x.Level==level&&x.ParentId==parentId&&x.Id!=id)
            .ToListAsync();
        foreach(var s in siblings.Where(x=>x.Position>pos)) s.Position-=1;
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task DeleteChildrenRecursiveAsync(int parentId)
    {
        var children=await _db.MenuButtons.Where(x=>x.ParentId==parentId).ToListAsync();
        foreach(var c in children)
            await DeleteChildrenRecursiveAsync(c.Id);
        var cakes=await _db.CakeItems.Where(x=>x.MenuButtonId==parentId).ToListAsync();
        foreach(var cake in cakes)
        {
            var medias=await _db.CakeMedias.Where(x=>x.CakeItemId==cake.Id).ToListAsync();
            _db.CakeMedias.RemoveRange(medias);
        }
        _db.CakeItems.RemoveRange(cakes);
        _db.MenuButtons.RemoveRange(children);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> MoveButtonAsync(int id,int newPosition)
    {
        var btn=await _db.MenuButtons.FirstOrDefaultAsync(x=>x.Id==id);
        if(btn==null) return false;
        var siblings=await _db.MenuButtons
            .Where(x=>x.Level==btn.Level&&x.ParentId==btn.ParentId)
            .ToListAsync();
        int maxPos=siblings.Count==0?1:siblings.Max(x=>x.Position);
        if(newPosition<1) newPosition=1;
        if(newPosition>maxPos) newPosition=maxPos;
        if(newPosition==btn.Position) return true;
        int oldPos=btn.Position;
        foreach(var s in siblings.Where(x=>x.Id!=id))
        {
            if(oldPos<newPosition&&s.Position>oldPos&&s.Position<=newPosition) s.Position-=1;
            else if(oldPos>newPosition&&s.Position<oldPos&&s.Position>=newPosition) s.Position+=1;
        }
        btn.Position=newPosition;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> CountButtonsAsync(int level,int? parentId)
        =>await _db.MenuButtons.CountAsync(x=>x.Level==level&&x.ParentId==parentId);

    public async Task<List<CakeItem>> GetCakeItemsAsync(int menuButtonId)
        =>await _db.CakeItems
            .Where(x=>x.MenuButtonId==menuButtonId)
            .OrderBy(x=>x.Position)
            .ToListAsync();

    public async Task<CakeItem?> GetCakeItemAsync(int id)
        =>await _db.CakeItems
            .Include(x=>x.Medias.OrderBy(m=>m.SortOrder))
            .FirstOrDefaultAsync(x=>x.Id==id);

    public async Task<CakeItem> AddCakeItemAsync(int menuButtonId,string title,int position,List<string> photoIds,string? caption)
    {
        var siblings=await _db.CakeItems.Where(x=>x.MenuButtonId==menuButtonId).ToListAsync();
        int maxPos=siblings.Count==0?0:siblings.Max(x=>x.Position);
        if(position<1) position=1;
        if(position>maxPos+1) position=maxPos+1;
        foreach(var s in siblings.Where(x=>x.Position>=position)) s.Position+=1;
        var item=new CakeItem
        {
            MenuButtonId=menuButtonId,
            Title=title,
            Position=position,
            CreatedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        _db.CakeItems.Add(item);
        await _db.SaveChangesAsync();
        for(int i=0;i<photoIds.Count;i++)
        {
            _db.CakeMedias.Add(new CakeMedia
            {
                CakeItemId=item.Id,
                PhotoPath=photoIds[i],
                Caption=i==0?caption:null,
                SortOrder=i
            });
        }
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<bool> EditCakeItemAsync(int id,string? title=null,string? caption=null,List<string>? photoIds=null)
    {
        var item=await _db.CakeItems.Include(x=>x.Medias).FirstOrDefaultAsync(x=>x.Id==id);
        if(item==null) return false;
        if(!string.IsNullOrEmpty(title)) item.Title=title;
        if(photoIds!=null&&photoIds.Count>0)
        {
            _db.CakeMedias.RemoveRange(item.Medias);
            await _db.SaveChangesAsync();
            for(int i=0;i<photoIds.Count;i++)
            {
                _db.CakeMedias.Add(new CakeMedia
                {
                    CakeItemId=item.Id,
                    PhotoPath=photoIds[i],
                    Caption=i==0?caption:null,
                    SortOrder=i
                });
            }
        }
        else if(caption!=null&&item.Medias.Count>0)
        {
            var first=item.Medias.OrderBy(x=>x.SortOrder).First();
            first.Caption=caption;
        }
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCakeItemAsync(int id)
    {
        var item=await _db.CakeItems.Include(x=>x.Medias).FirstOrDefaultAsync(x=>x.Id==id);
        if(item==null) return false;
        int parentId=item.MenuButtonId;
        int pos=item.Position;
        _db.CakeMedias.RemoveRange(item.Medias);
        _db.CakeItems.Remove(item);
        var siblings=await _db.CakeItems
            .Where(x=>x.MenuButtonId==parentId&&x.Id!=id)
            .ToListAsync();
        foreach(var s in siblings.Where(x=>x.Position>pos)) s.Position-=1;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveCakeItemAsync(int id,int newPosition)
    {
        var item=await _db.CakeItems.FirstOrDefaultAsync(x=>x.Id==id);
        if(item==null) return false;
        var siblings=await _db.CakeItems.Where(x=>x.MenuButtonId==item.MenuButtonId).ToListAsync();
        int maxPos=siblings.Count==0?1:siblings.Max(x=>x.Position);
        if(newPosition<1) newPosition=1;
        if(newPosition>maxPos) newPosition=maxPos;
        if(newPosition==item.Position) return true;
        int oldPos=item.Position;
        foreach(var s in siblings.Where(x=>x.Id!=id))
        {
            if(oldPos<newPosition&&s.Position>oldPos&&s.Position<=newPosition) s.Position-=1;
            else if(oldPos>newPosition&&s.Position<oldPos&&s.Position>=newPosition) s.Position+=1;
        }
        item.Position=newPosition;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> BuildMenuTreeTextAsync()
    {
        var l1=await GetAllButtonsByLevelAsync(1,null);
        var sb=new System.Text.StringBuilder();
        sb.AppendLine("🌳 ساختار منوی کیک گندم");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━");
        foreach(var b1 in l1)
        {
            if(b1.CallbackData=="menu_manage") continue;
            string extra1=b1.IsSuperOnly?" (ویژه)":"";
            sb.AppendLine($"📁 {b1.Title}{extra1}");
            var l2=await GetAllButtonsByLevelAsync(2,b1.Id);
            foreach(var b2 in l2)
            {
                string extra2=b2.IsSuperOnly?" (ویژه)":"";
                sb.AppendLine($"   └─📂 {b2.Title}{extra2}");
                var cakes=await GetCakeItemsAsync(b2.Id);
                foreach(var c in cakes)
                    sb.AppendLine($"      └─🍰 {c.Title}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    public async Task<int> CountCakeItemsAsync(int menuButtonId)
            =>await _db.CakeItems.CountAsync(x=>x.MenuButtonId==menuButtonId);

        public async Task<string?> GetSettingAsync(string key)
        {
            var s=await _db.BotSettings.FirstOrDefaultAsync(x=>x.Key==key);
            return s?.Value;
        }

        public async Task SetSettingAsync(string key,string value)
        {
            var s=await _db.BotSettings.FirstOrDefaultAsync(x=>x.Key==key);
            if(s==null) _db.BotSettings.Add(new BotSetting{Key=key,Value=value});
            else s.Value=value;
            await _db.SaveChangesAsync();
        }
}
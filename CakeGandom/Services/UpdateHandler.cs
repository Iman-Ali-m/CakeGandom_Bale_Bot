using System.Text;
using System.Text.Json;
using CakeGandom.Data;
using CakeGandom.Dtos;
using CakeGandom.Models;

namespace CakeGandom.Services;

public class UpdateHandler
{
    private readonly BaleApiClient _api;
    private readonly UserService _users;
    private readonly RegistrationService _reg;
    private readonly MenuService _menu;
    private readonly BotDbContext _db;
    private readonly ILogger<UpdateHandler> _logger;

    private class AdmData
    {
        public string? Title{get;set;}
        public int Position{get;set;}
        public List<string> Photos{get;set;}=new();
        public string? Caption{get;set;}
        public bool IsSuperOnly{get;set;}
    }

    private class ScrData
    {
        public string Screen{get;set;}="main";
        public int? ButtonId{get;set;}
        public int? CakeId{get;set;}
    }

    public UpdateHandler(BaleApiClient api,UserService users,RegistrationService reg,MenuService menu,BotDbContext db,ILogger<UpdateHandler> logger)
    {
        _api=api;
        _users=users;
        _reg=reg;
        _menu=menu;
        _db=db;
        _logger=logger;
    }

    public async Task HandleAsync(Update update,CancellationToken ct=default)
    {
        if(update.CallbackQuery!=null)
        {
            await HandleCallbackAsync(update.CallbackQuery,ct);
            return;
        }
        if(update.Message==null) return;
        if(update.Message.Photo!=null&&update.Message.Photo.Count>0)
        {
            await HandlePhotoAsync(update.Message,ct);
            return;
        }
        if(!string.IsNullOrEmpty(update.Message.Text))
        {
            await HandleTextAsync(update.Message,ct);
        }
    }

    private async Task HandleTextAsync(Message msg,CancellationToken ct)
    {
        var userId=msg.From?.Id.ToString()??string.Empty;
        var chatId=msg.Chat?.Id.ToString()??string.Empty;
        if(string.IsNullOrEmpty(userId)||string.IsNullOrEmpty(chatId)) return;
        await _users.RegisterOrUpdateAsync(userId,msg.From?.Username,msg.From?.FirstName,msg.From?.LastName);
        var user=await _users.GetUserAsync(userId);
        if(user!=null&&user.IsBlocked) return;
        var text=(msg.Text??string.Empty).Trim();

        if(text=="/start")
        {
            await _users.ClearSessionAsync(userId);
            var u=await _users.GetUserAsync(userId);
            if(u!=null&&u.IsRegistered&&u.Profile!=null&&!string.IsNullOrWhiteSpace(u.Profile.LastName))
            {
                var welcome=$"👋 سلام {u.Profile.LastName} عزیز\nبه کیک گندم خوش آمدید 🍰";
                await _api.SendMessage(chatId,welcome,null,ct);
            }
            else
            {
                var welcome="🌟 به خانواده کیک گندم خوش آمدید 🍰\n\n"+
                            "برای اینکه بتونیم سفارش شما را ثبت کنیم، لطفاً مراحل ثبت‌نام را کامل کنید\n"+
                            "بعد از ثبت‌نام از منوی زیر، سفارش خود را انتخاب کنید";
                await _api.SendMessage(chatId,welcome,null,ct);
            }
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }

        if(text==Nav.RestartText||text==Nav.MainText||text==Nav.PanelSeparatorText||text=="/menu")
        {
            await _users.ClearSessionAsync(userId);
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }

        if(text==Nav.BackText)
        {
            await GoBackAsync(userId,chatId,null,ct);
            return;
        }

        var state=await _users.GetSessionStateAsync(userId);
        if(state.StartsWith("adm|"))
        {
            bool isMenuButton=text.StartsWith("📝")||text.StartsWith("📚")||text.StartsWith("👤")||
                              text.StartsWith("📖")||text.StartsWith("ℹ️")||text.StartsWith("📄")||
                              text.StartsWith("📋")||text.StartsWith("🗂")||text.StartsWith("➕")||
                              text.StartsWith("✏️")||text.StartsWith("🗑")||text.StartsWith("🔀")||
                              text.StartsWith("♻")||text.StartsWith("⬇️");
            if(!isMenuButton)
            {
                await HandleAdminInputAsync(userId,chatId,text,state,ct);
                return;
            }
            await _users.ClearSessionAsync(userId);
        }

        var isComplete=await _reg.IsCompleteAsync(userId);
        if(!isComplete)
        {
            var step=await _reg.GetStepAsync(userId);
            if(step!="start"&&step!="complete")
            {
                var result=await _reg.ProcessAsync(userId,text);
                await _api.SendMessage(chatId,result.message,null,ct);
                return;
            }
        }

        if(_users.IsPrivileged(userId))
        {
            var t=(msg.Text??string.Empty).Trim();
            if(text=="📄 خروجی کاربران")
            {
                var export=await _reg.BuildUsersExportAsync();
                var bytes=System.Text.Encoding.UTF8.GetBytes(export);
                var fileName=$"users_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                await _api.SendDocument(chatId,bytes,fileName,null,ct);
                return;
            }
            if(text=="📋 خروجی کاربر")
            {
                var export=await _reg.BuildUsersExportAsync();
                await SendLongAsync(chatId,export,null,ct);
                return;
            }
            if(text=="🗂 ساختار منو")
            {
                var tree=await _menu.BuildMenuTreeTextAsync();
                await SendLongAsync(chatId,tree,null,ct);
                return;
            }
            if(text=="➕ افزودن")
            {
                await StartAddAsync(userId,chatId,0,"adm_add:1:0",ct);
                return;
            }
            if(text=="✏️ ویرایش")
            {
                await StartEditSelectionAsync(userId,chatId,0,"adm_edit:1:0",ct);
                return;
            }
            if(text=="🗑 حذف")
            {
                await StartDeleteSelectionAsync(userId,chatId,0,"adm_del:1:0",ct);
                return;
            }
            if(text=="🔀 جابجایی")
            {
                await StartMoveSelectionAsync(userId,chatId,0,"adm_move:1:0",ct);
                return;
            }
        }

        var mainButtons=await _menu.GetButtonsByLevelAsync(1,null,userId);
        var matched=mainButtons.FirstOrDefault(b=>b.Title==text);
        if(matched!=null)
        {
            if(matched.CallbackData=="menu_manage") return;
            if(matched.CallbackData=="menu_register")
            {
                await _reg.ResetAsync(userId);
                await _users.SetSessionAsync(userId,"idle",null);
                await _api.SendMessage(chatId,"📝 ثبت‌نام شروع شد\nنام خود را وارد کنید:",null,ct);
                return;
            }
            if(matched.CallbackData=="menu_profile")
            {
                var summary=await _reg.BuildProfileSummaryAsync(userId);
                await _api.SendMessage(chatId,summary,Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            if(matched.CallbackData=="menu_help")
            {
                var help=await _menu.GetSettingAsync("help_text")??"راهنمایی موجود نیست";
                await _api.SendMessage(chatId,help,Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            if(matched.CallbackData=="menu_contact")
            {
                var contact=await _menu.GetSettingAsync("contact_text")??"اطلاعات تماس موجود نیست";
                await _api.SendMessage(chatId,contact,Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            await SendButtonScreenAsync(userId,chatId,null,matched.Id,ct);
            return;
        }

        await _api.SendMessage(chatId,"لطفاً از دکمه‌های منو استفاده کنید",null,ct);
    }

    private async Task HandlePhotoAsync(Message msg,CancellationToken ct)
    {
        var userId=msg.From?.Id.ToString()??string.Empty;
        var chatId=msg.Chat?.Id.ToString()??string.Empty;
        if(string.IsNullOrEmpty(userId)||string.IsNullOrEmpty(chatId)) return;
        await _users.RegisterOrUpdateAsync(userId,msg.From?.Username,msg.From?.FirstName,msg.From?.LastName);
        var state=await _users.GetSessionStateAsync(userId);
        bool isAddCake=state.StartsWith("adm|add_cake|")&&state.EndsWith("|photos");
        bool isEditCake=state.StartsWith("adm|edit_cake_photos|");
        if(!isAddCake&&!isEditCake) return;
        if(msg.Photo==null||msg.Photo.Count==0) return;
        var fileId=msg.Photo[^1].FileId;
        if(string.IsNullOrEmpty(fileId)) return;
        var data=await GetAdmDataAsync(userId);
        if(data.Photos.Count>=10)
        {
            await _api.SendMessage(chatId,"حداکثر ۱۰ عکس مجاز است. /done را بفرستید",null,ct);
            return;
        }
        data.Photos.Add(fileId);
        await SetAdmDataAsync(userId,data);
        await _api.SendMessage(chatId,$"عکس ثبت شد ({data.Photos.Count})\nعکس‌های دیگر را بفرستید یا /done برای پایان",null,ct);
    }

    private async Task HandleCallbackAsync(CallbackQuery query,CancellationToken ct)
    {
        var userId=query.From?.Id.ToString()??string.Empty;
        var chatId=query.Message?.Chat?.Id.ToString()??string.Empty;
        var messageId=query.Message?.MessageId??0;
        var data=query.Data??string.Empty;
        if(string.IsNullOrEmpty(userId)||string.IsNullOrEmpty(chatId)) return;
        await _api.AnswerCallback(query.Id,null,ct);
        await _users.RegisterOrUpdateAsync(userId,query.From?.Username,query.From?.FirstName,query.From?.LastName);
        var user=await _users.GetUserAsync(userId);
        if(user!=null&&user.IsBlocked) return;

        if(data==Nav.BackToMain)
        {
            await SendMainMenuAsync(userId,chatId,messageId,ct);
            return;
        }
        if(data==Nav.Restart)
        {
            await _users.ClearSessionAsync(userId);
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }
        if(data==Nav.Back)
        {
            await GoBackAsync(userId,chatId,messageId,ct);
            return;
        }
        if(data==Nav.Noop) return;
        if(data.StartsWith(Nav.MenuPrefix))
        {
            var idStr=data.Substring(Nav.MenuPrefix.Length);
            if(int.TryParse(idStr,out var id))
            {
                await SendButtonScreenAsync(userId,chatId,messageId,id,ct);
                return;
            }
        }
        if(data.StartsWith(Nav.CakePrefix))
        {
            var idStr=data.Substring(Nav.CakePrefix.Length);
            if(int.TryParse(idStr,out var id))
            {
                await SendCakeScreenAsync(userId,chatId,messageId,id,ct);
                return;
            }
        }
        if(data=="menu_register")
        {
            await _reg.ResetAsync(userId);
            await _users.SetSessionAsync(userId,"idle",null);
            await _api.EditMessage(chatId,messageId,"📝 ثبت‌نام شروع شد\nنام خود را وارد کنید:",null,ct);
            return;
        }
        if(data=="menu_cakes")
        {
            var list=await _menu.GetButtonsByLevelAsync(1,null,userId);
            var btn=list.FirstOrDefault(x=>x.CallbackData=="menu_cakes");
            if(btn==null) return;
            await SendButtonScreenAsync(userId,chatId,messageId,btn.Id,ct);
            return;
        }
        if(data=="menu_profile")
        {
            var summary=await _reg.BuildProfileSummaryAsync(userId);
            await _api.EditMessage(chatId,messageId,summary,Keyboards.KeyboardsWithBack(),ct);
            return;
        }
        if(data=="menu_help")
        {
            var help=await _menu.GetSettingAsync("help_text")??"راهنمایی موجود نیست";
            InlineKeyboardMarkup kb;
            if(_users.IsPrivileged(userId))
            {
                var rows=new List<List<InlineKeyboardButton>>();
                rows.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text="✏️ ویرایش متن",CallbackData="adm_help_edit"}
                });
                kb=Keyboards.KeyboardsWithExtra(rows,true);
            }
            else
            {
                kb=Keyboards.KeyboardsWithBack();
            }
            await _api.EditMessage(chatId,messageId,help,kb,ct);
            return;
        }
        if(data=="menu_contact")
        {
            var contact=await _menu.GetSettingAsync("contact_text")??"اطلاعات تماس موجود نیست";
            InlineKeyboardMarkup kb;
            if(_users.IsPrivileged(userId))
            {
                var rows=new List<List<InlineKeyboardButton>>();
                rows.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text="✏️ ویرایش متن",CallbackData="adm_contact_edit"}
                });
                kb=Keyboards.KeyboardsWithExtra(rows,true);
            }
            else
            {
                kb=Keyboards.KeyboardsWithBack();
            }
            await _api.EditMessage(chatId,messageId,contact,kb,ct);
            return;
        }
        if(data==Nav.UserExport)
        {
            if(!_users.IsPrivileged(userId)) return;
            var export=await _reg.BuildUsersExportAsync();
            var bytes=System.Text.Encoding.UTF8.GetBytes(export);
            var fileName=$"users_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            await _api.SendDocument(chatId,bytes,fileName,null,ct);
            return;
        }
        if(data==Nav.UserExportText)
        {
            if(!_users.IsPrivileged(userId)) return;
            var text=await _reg.BuildUsersExportAsync();
            await SendLongAsync(chatId,text,null,ct);
            return;
        }
        if(data==Nav.ManageMenu)
        {
            if(!_users.IsPrivileged(userId)) return;
            var tree=await _menu.BuildMenuTreeTextAsync();
            await SendLongAsync(chatId,tree,null,ct);
            return;
        }
        if(data=="adm_help_edit")
        {
            if(!_users.IsPrivileged(userId)) return;
            await _users.SetSessionAsync(userId,"adm|help",null);
            await _api.EditMessage(chatId,messageId,"متن جدید راهنما را ارسال کنید:",Keyboards.KeyboardsWithCancel(),ct);
            return;
        }
        if(data=="adm_contact_edit")
        {
            if(!_users.IsPrivileged(userId)) return;
            await _users.SetSessionAsync(userId,"adm|contact",null);
            await _api.EditMessage(chatId,messageId,"متن جدید ارتباط با ما را ارسال کنید:",Keyboards.KeyboardsWithCancel(),ct);
            return;
        }
        if(data.StartsWith("adm_add:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            await StartAddAsync(userId,chatId,messageId,data,ct);
            return;
        }
        if(data.StartsWith("adm_edit:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            await StartEditSelectionAsync(userId,chatId,messageId,data,ct);
            return;
        }
        if(data.StartsWith("adm_del:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            await StartDeleteSelectionAsync(userId,chatId,messageId,data,ct);
            return;
        }
        if(data.StartsWith("adm_move:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            await StartMoveSelectionAsync(userId,chatId,messageId,data,ct);
            return;
        }
        if(data.StartsWith("adm_edit_btn:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_edit_btn:".Length));
            await _users.SetSessionAsync(userId,$"adm|edit_btn|{id}",null);
            await _api.EditMessage(chatId,messageId,"عنوان جدید دکمه را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            return;
        }
        if(data.StartsWith("adm_move_btn:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_move_btn:".Length));
            await _users.SetSessionAsync(userId,$"adm|move_btn|{id}",null);
            await _api.EditMessage(chatId,messageId,"شماره موقعیت جدید را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            return;
        }
        if(data.StartsWith("adm_del_btn:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_del_btn:".Length));
            var btn=await _menu.GetButtonAsync(id);
            if(btn==null) return;
            int level=btn.Level;
            int parentId=btn.ParentId??0;
            await _menu.DeleteButtonAsync(id);
            await _api.EditMessage(chatId,messageId,"دکمه حذف شد",null,ct);
            if(level==1) await SendMainMenuAsync(userId,chatId,null,ct);
            else await SendButtonScreenAsync(userId,chatId,null,parentId,ct);
            return;
        }
        if(data.StartsWith("adm_edit_cake_field:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var rest=data.Substring("adm_edit_cake_field:".Length);
            var parts=rest.Split(':');
            if(parts.Length!=2) return;
            var id=int.Parse(parts[0]);
            var field=parts[1];
            if(field=="title")
            {
                await _users.SetSessionAsync(userId,$"adm|edit_cake_title|{id}",null);
                await _api.EditMessage(chatId,messageId,"عنوان جدید کیک را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(field=="caption")
            {
                await _users.SetSessionAsync(userId,$"adm|edit_cake_caption|{id}",null);
                await _api.EditMessage(chatId,messageId,"متن جدید زیر عکس‌ها را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(field=="photos")
            {
                var ad=new AdmData();
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|edit_cake_photos|{id}",json);
                await _api.EditMessage(chatId,messageId,"عکس‌های جدید را ارسال کنید. /done برای پایان:",Keyboards.KeyboardsWithCancel(),ct);
            }
            return;
        }
        if(data.StartsWith("adm_edit_cake:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_edit_cake:".Length));
            var rows=new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>{new InlineKeyboardButton{Text="عنوان",CallbackData=$"adm_edit_cake_field:{id}:title"}},
                new List<InlineKeyboardButton>{new InlineKeyboardButton{Text="متن زیر عکس",CallbackData=$"adm_edit_cake_field:{id}:caption"}},
                new List<InlineKeyboardButton>{new InlineKeyboardButton{Text="عکس‌ها",CallbackData=$"adm_edit_cake_field:{id}:photos"}}
            };
            var kb=Keyboards.KeyboardsWithExtra(rows,true);
            await _api.EditMessage(chatId,messageId,"کدام قسمت را ویرایش می‌کنید؟",kb,ct);
            return;
        }
        if(data.StartsWith("adm_move_cake:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_move_cake:".Length));
            await _users.SetSessionAsync(userId,$"adm|move_cake|{id}",null);
            await _api.EditMessage(chatId,messageId,"شماره موقعیت جدید را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            return;
        }
        if(data.StartsWith("adm_del_cake:"))
        {
            if(!_users.IsPrivileged(userId)) return;
            var id=int.Parse(data.Substring("adm_del_cake:".Length));
            var cake=await _menu.GetCakeItemAsync(id);
            if(cake==null) return;
            int menuId=cake.MenuButtonId;
            await _menu.DeleteCakeItemAsync(id);
            await _api.EditMessage(chatId,messageId,"کیک حذف شد",null,ct);
            await SendButtonScreenAsync(userId,chatId,null,menuId,ct);
            return;
        }
    }

    private async Task StartAddAsync(string userId,string chatId,long messageId,string data,CancellationToken ct)
    {
        var rest=data.Substring("adm_add:".Length);
        var parts=rest.Split(':');
        if(parts.Length!=2) return;
        var level=int.Parse(parts[0]);
        var parentId=int.Parse(parts[1]);
        long? editId=messageId>0?messageId:(long?)null;
        if(level==3)
        {
            await _users.SetSessionAsync(userId,$"adm|add_cake|{parentId}|title",null);
            await SendOrEditAsync(chatId,editId,"نام کیک را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
        }
        else
        {
            await _users.SetSessionAsync(userId,$"adm|add_btn|{level}|{parentId}|title",null);
            await SendOrEditAsync(chatId,editId,"نام دکمه را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
        }
    }

    private async Task StartEditSelectionAsync(string userId,string chatId,long messageId,string data,CancellationToken ct)
    {
        var rest=data.Substring("adm_edit:".Length);
        var parts=rest.Split(':');
        if(parts.Length!=2) return;
        var level=int.Parse(parts[0]);
        var parentId=int.Parse(parts[1]);
        long? editId=messageId>0?messageId:(long?)null;
        var rows=new List<List<InlineKeyboardButton>>();
        if(level==3)
        {
            var cakes=await _menu.GetCakeItemsAsync(parentId);
            if(cakes.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"کیکی وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var c in cakes)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{c.Position}. {c.Title}",CallbackData=$"adm_edit_cake:{c.Id}"}});
        }
        else
        {
            var btns=await _menu.GetAllButtonsByLevelAsync(level,parentId==0?null:parentId);
            if(btns.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"دکمه‌ای وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var b in btns)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{b.Position}. {b.Title}",CallbackData=$"adm_edit_btn:{b.Id}"}});
        }
        await SendOrEditAsync(chatId,editId,"کدام مورد را ویرایش می‌کنید؟",Keyboards.KeyboardsWithExtra(rows,true),ct);
    }

    private async Task StartDeleteSelectionAsync(string userId,string chatId,long messageId,string data,CancellationToken ct)
    {
        var rest=data.Substring("adm_del:".Length);
        var parts=rest.Split(':');
        if(parts.Length!=2) return;
        var level=int.Parse(parts[0]);
        var parentId=int.Parse(parts[1]);
        long? editId=messageId>0?messageId:(long?)null;
        var rows=new List<List<InlineKeyboardButton>>();
        if(level==3)
        {
            var cakes=await _menu.GetCakeItemsAsync(parentId);
            if(cakes.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"کیکی وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var c in cakes)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{c.Position}. {c.Title}",CallbackData=$"adm_del_cake:{c.Id}"}});
        }
        else
        {
            var btns=await _menu.GetAllButtonsByLevelAsync(level,parentId==0?null:parentId);
            if(btns.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"دکمه‌ای وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var b in btns)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{b.Position}. {b.Title}",CallbackData=$"adm_del_btn:{b.Id}"}});
        }
        await SendOrEditAsync(chatId,editId,"کدام مورد را حذف می‌کنید؟",Keyboards.KeyboardsWithExtra(rows,true),ct);
    }

    private async Task StartMoveSelectionAsync(string userId,string chatId,long messageId,string data,CancellationToken ct)
    {
        var rest=data.Substring("adm_move:".Length);
        var parts=rest.Split(':');
        if(parts.Length!=2) return;
        var level=int.Parse(parts[0]);
        var parentId=int.Parse(parts[1]);
        long? editId=messageId>0?messageId:(long?)null;
        var rows=new List<List<InlineKeyboardButton>>();
        if(level==3)
        {
            var cakes=await _menu.GetCakeItemsAsync(parentId);
            if(cakes.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"کیکی وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var c in cakes)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{c.Position}. {c.Title}",CallbackData=$"adm_move_cake:{c.Id}"}});
        }
        else
        {
            var btns=await _menu.GetAllButtonsByLevelAsync(level,parentId==0?null:parentId);
            if(btns.Count==0)
            {
                await SendOrEditAsync(chatId,editId,"دکمه‌ای وجود ندارد",Keyboards.KeyboardsWithBack(),ct);
                return;
            }
            foreach(var b in btns)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=$"{b.Position}. {b.Title}",CallbackData=$"adm_move_btn:{b.Id}"}});
        }
        await SendOrEditAsync(chatId,editId,"کدام مورد را جابجا می‌کنید؟",Keyboards.KeyboardsWithExtra(rows,true),ct);
    }

    private async Task HandleAdminInputAsync(string userId,string chatId,string text,string state,CancellationToken ct)
    {
        var parts=state.Split('|');
        if(parts.Length<2)
        {
            await _users.ClearSessionAsync(userId);
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }
        if(parts[1]=="help")
        {
            await _menu.SetSettingAsync("help_text",text);
            await _users.ClearSessionAsync(userId);
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }
        if(parts[1]=="contact")
        {
            await _menu.SetSettingAsync("contact_text",text);
            await _users.ClearSessionAsync(userId);
            await SendMainMenuAsync(userId,chatId,null,ct);
            return;
        }
        if(parts[1]=="edit_btn"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            var btn=await _menu.GetButtonAsync(id);
            if(btn==null)
            {
                await _users.ClearSessionAsync(userId);
                return;
            }
            int level=btn.Level;
            int parentId=btn.ParentId??0;
            await _menu.EditButtonAsync(id,title:text);
            await _users.ClearSessionAsync(userId);
            await _api.SendMessage(chatId,"دکمه ویرایش شد",null,ct);
            if(level==1) await SendMainMenuAsync(userId,chatId,null,ct);
            else await SendButtonScreenAsync(userId,chatId,null,parentId,ct);
            return;
        }
        if(parts[1]=="edit_cake_title"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            var cake=await _menu.GetCakeItemAsync(id);
            if(cake==null)
            {
                await _users.ClearSessionAsync(userId);
                return;
            }
            int menuId=cake.MenuButtonId;
            await _menu.EditCakeItemAsync(id,title:text);
            await _users.ClearSessionAsync(userId);
            await _api.SendMessage(chatId,"عنوان کیک ویرایش شد",null,ct);
            await SendButtonScreenAsync(userId,chatId,null,menuId,ct);
            return;
        }
        if(parts[1]=="edit_cake_caption"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            var cake=await _menu.GetCakeItemAsync(id);
            if(cake==null)
            {
                await _users.ClearSessionAsync(userId);
                return;
            }
            int menuId=cake.MenuButtonId;
            await _menu.EditCakeItemAsync(id,caption:text);
            await _users.ClearSessionAsync(userId);
            await _api.SendMessage(chatId,"متن کیک ویرایش شد",null,ct);
            await SendButtonScreenAsync(userId,chatId,null,menuId,ct);
            return;
        }
        if(parts[1]=="edit_cake_photos"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            if(text=="/done")
            {
                var ad=await GetAdmDataAsync(userId);
                if(ad.Photos.Count==0)
                {
                    await _api.SendMessage(chatId,"حداقل یک عکس ارسال کنید",null,ct);
                    return;
                }
                var cake=await _menu.GetCakeItemAsync(id);
                if(cake==null)
                {
                    await _users.ClearSessionAsync(userId);
                    return;
                }
                int menuId=cake.MenuButtonId;
                await _menu.EditCakeItemAsync(id,photoIds:ad.Photos);
                await _users.ClearSessionAsync(userId);
                await _api.SendMessage(chatId,"عکس‌های کیک ویرایش شد",null,ct);
                await SendButtonScreenAsync(userId,chatId,null,menuId,ct);
            }
            return;
        }
        if(parts[1]=="move_btn"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            var norm=NormalizeDigits(text);
            if(!int.TryParse(norm,out var pos))
            {
                await _api.SendMessage(chatId,"عدد معتبر وارد کنید",null,ct);
                return;
            }
            var btn=await _menu.GetButtonAsync(id);
            if(btn==null)
            {
                await _users.ClearSessionAsync(userId);
                return;
            }
            int level=btn.Level;
            int parentId=btn.ParentId??0;
            await _menu.MoveButtonAsync(id,pos);
            await _users.ClearSessionAsync(userId);
            await _api.SendMessage(chatId,"موقعیت تغییر کرد",null,ct);
            if(level==1) await SendMainMenuAsync(userId,chatId,null,ct);
            else await SendButtonScreenAsync(userId,chatId,null,parentId,ct);
            return;
        }
        if(parts[1]=="move_cake"&&parts.Length==3)
        {
            var id=int.Parse(parts[2]);
            var norm=NormalizeDigits(text);
            if(!int.TryParse(norm,out var pos))
            {
                await _api.SendMessage(chatId,"عدد معتبر وارد کنید",null,ct);
                return;
            }
            var cake=await _menu.GetCakeItemAsync(id);
            if(cake==null)
            {
                await _users.ClearSessionAsync(userId);
                return;
            }
            int menuId=cake.MenuButtonId;
            await _menu.MoveCakeItemAsync(id,pos);
            await _users.ClearSessionAsync(userId);
            await _api.SendMessage(chatId,"موقعیت تغییر کرد",null,ct);
            await SendButtonScreenAsync(userId,chatId,null,menuId,ct);
            return;
        }
        if(parts[1]=="add_btn"&&parts.Length==5)
        {
            var level=int.Parse(parts[2]);
            var parentId=int.Parse(parts[3]);
            var step=parts[4];
            int? parent=parentId==0?null:parentId;
            if(step=="title")
            {
                var ad=new AdmData{Title=text};
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|add_btn|{level}|{parentId}|pos",json);
                int count=await _menu.CountButtonsAsync(level,parent);
                await _api.SendMessage(chatId,$"موقعیت را وارد کنید (۱ تا {count+1}):",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(step=="pos")
            {
                var norm=NormalizeDigits(text);
                if(!int.TryParse(norm,out var pos)||pos<1)
                {
                    await _api.SendMessage(chatId,"عدد معتبر وارد کنید",null,ct);
                    return;
                }
                var ad=await GetAdmDataAsync(userId);
                ad.Position=pos;
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|add_btn|{level}|{parentId}|super",json);
                await _api.SendMessage(chatId,"آیا این دکمه فقط برای کاربر اصلی نمایش داده شود؟ (بله/خیر)",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(step=="super")
            {
                var ad=await GetAdmDataAsync(userId);
                var t=text.Trim();
                ad.IsSuperOnly=t=="بله"||t=="yes"||t=="آری"||t=="1";
                await _menu.AddButtonAsync(level,parent,ad.Title??"بدون عنوان",ad.Position,ad.IsSuperOnly);
                await _users.ClearSessionAsync(userId);
                await _api.SendMessage(chatId,"دکمه اضافه شد",null,ct);
                if(level==1) await SendMainMenuAsync(userId,chatId,null,ct);
                else await SendButtonScreenAsync(userId,chatId,null,parentId,ct);
            }
            return;
        }
        if(parts[1]=="add_cake"&&parts.Length==4)
        {
            var menuButtonId=int.Parse(parts[2]);
            var step=parts[3];
            if(step=="title")
            {
                var ad=new AdmData{Title=text};
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|add_cake|{menuButtonId}|pos",json);
                int count=await _menu.CountCakeItemsAsync(menuButtonId);
                await _api.SendMessage(chatId,$"موقعیت را وارد کنید (۱ تا {count+1}):",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(step=="pos")
            {
                var norm=NormalizeDigits(text);
                if(!int.TryParse(norm,out var pos)||pos<1)
                {
                    await _api.SendMessage(chatId,"عدد معتبر وارد کنید",null,ct);
                    return;
                }
                var ad=await GetAdmDataAsync(userId);
                ad.Position=pos;
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|add_cake|{menuButtonId}|photos",json);
                await _api.SendMessage(chatId,"عکس‌ها را ارسال کنید. /done برای پایان",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(step=="photos")
            {
                if(text!="/done")
                {
                    await _api.SendMessage(chatId,"عکس بفرستید یا /done بزنید",null,ct);
                    return;
                }
                var ad=await GetAdmDataAsync(userId);
                if(ad.Photos.Count==0)
                {
                    await _api.SendMessage(chatId,"حداقل یک عکس ارسال کنید",null,ct);
                    return;
                }
                var json=JsonSerializer.Serialize(ad);
                await _users.SetSessionAsync(userId,$"adm|add_cake|{menuButtonId}|caption",json);
                await _api.SendMessage(chatId,"متن زیر عکس‌ها را وارد کنید:",Keyboards.KeyboardsWithCancel(),ct);
            }
            else if(step=="caption")
            {
                var ad=await GetAdmDataAsync(userId);
                ad.Caption=text;
                await _menu.AddCakeItemAsync(menuButtonId,ad.Title??"بدون عنوان",ad.Position,ad.Photos,ad.Caption);
                await _users.ClearSessionAsync(userId);
                await _api.SendMessage(chatId,"کیک اضافه شد",null,ct);
                await SendButtonScreenAsync(userId,chatId,null,menuButtonId,ct);
            }
            return;
        }
        await _users.ClearSessionAsync(userId);
        await SendMainMenuAsync(userId,chatId,null,ct);
    }

    private async Task SendMainMenuAsync(string userId,string chatId,long? editId,CancellationToken ct)
    {
        var buttons=await _menu.GetButtonsByLevelAsync(1,null,userId);
        var publicTitles=new List<string>();
        string panelTitle=Nav.PanelSeparatorText;
        foreach(var b in buttons)
        {
            if(b.CallbackData=="menu_manage") continue;
            if(b.CallbackData=="menu_users_export") continue;
            if(b.CallbackData=="menu_users_export_text") continue;
            if(b.IsSuperOnly&&!_users.IsPrivileged(userId)) continue;
            if(b.Title==Nav.PanelSeparatorText||b.CallbackData=="menu_user_panel")
            {
                panelTitle=b.Title;
                continue;
            }
            publicTitles.Add(b.Title);
        }
        var rows=new List<List<string>>();
        var cur=new List<string>();
        foreach(var t in publicTitles)
        {
            cur.Add(t);
            if(cur.Count==2)
            {
                rows.Add(cur);
                cur=new List<string>();
            }
        }
        if(cur.Count>0) rows.Add(cur);
        if(_users.IsPrivileged(userId))
        {
            rows.Add(new List<string>{panelTitle});
            rows.Add(new List<string>{"📄 خروجی کاربران","📋 خروجی کاربر"});
            rows.Add(new List<string>{"🗂 ساختار منو"});
            rows.Add(new List<string>{"➕ افزودن","✏️ ویرایش","🗑 حذف","🔀 جابجایی"});
            rows.Add(new List<string>{Nav.RestartText});
        }
        else
        {
            rows.Add(new List<string>{Nav.RestartText});
        }
        var replyKb=Keyboards.DynamicMainReply(rows);
        await SetScrDataAsync(userId,new ScrData{Screen="main"});
        if(editId.HasValue)
        {
            await _api.EditMessage(chatId,editId.Value,"🏠 منوی اصلی کیک گندم\nاز دکمه‌های زیر استفاده کنید",null,ct);
        }
        await _api.SendMessage(chatId,"🏠 منوی اصلی کیک گندم\nاز دکمه‌های زیر استفاده کنید",replyKb,ct);
    }

    private async Task SendButtonScreenAsync(string userId,string chatId,long? editId,int buttonId,CancellationToken ct)
    {
        var btn=await _menu.GetButtonAsync(buttonId);
        if(btn==null)
        {
            await SendOrEditAsync(chatId,editId,"دکمه یافت نشد",null,ct);
            return;
        }
        if(btn.IsSuperOnly&&!_users.IsPrivileged(userId))
        {
            await SendOrEditAsync(chatId,editId,"دسترسی ندارید",null,ct);
            return;
        }
        var rows=new List<List<InlineKeyboardButton>>();
        var children=await _menu.GetButtonsByLevelAsync(btn.Level+1,btn.Id,userId);
        if(children.Count>0)
        {
            foreach(var c in children)
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=c.Title,CallbackData=c.CallbackData??$"mnu:{c.Id}"}});
        }
        else
        {
            var cakes=await _menu.GetCakeItemsAsync(btn.Id);
            if(cakes.Count==0)
            {
                rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text="(خالی)",CallbackData=Nav.Noop}});
            }
            else
            {
                foreach(var c in cakes)
                    rows.Add(new List<InlineKeyboardButton>{new InlineKeyboardButton{Text=c.Title,CallbackData=$"cake:{c.Id}"}});
            }
        }
        if(_users.IsPrivileged(userId))
        {
            if(btn.Level==1)
            {
                int childLevel=btn.Level+1;
                rows.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text="➕ افزودن دکمه",CallbackData=$"adm_add:{childLevel}:{btn.Id}"},
                    new InlineKeyboardButton{Text="✏️ ویرایش",CallbackData=$"adm_edit:{childLevel}:{btn.Id}"},
                    new InlineKeyboardButton{Text="🗑 حذف",CallbackData=$"adm_del:{childLevel}:{btn.Id}"},
                    new InlineKeyboardButton{Text="🔀 جابجایی",CallbackData=$"adm_move:{childLevel}:{btn.Id}"}
                });
            }
            else
            {
                rows.Add(new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text="➕ افزودن کیک",CallbackData=$"adm_add:3:{btn.Id}"},
                    new InlineKeyboardButton{Text="✏️ ویرایش کیک",CallbackData=$"adm_edit:3:{btn.Id}"},
                    new InlineKeyboardButton{Text="🗑 حذف کیک",CallbackData=$"adm_del:3:{btn.Id}"},
                    new InlineKeyboardButton{Text="🔀 جابجایی کیک",CallbackData=$"adm_move:3:{btn.Id}"}
                });
            }
        }
        rows.Add(new List<InlineKeyboardButton>
        {
            new InlineKeyboardButton{Text=Nav.BackText,CallbackData=Nav.Back},
            new InlineKeyboardButton{Text=Nav.MainText,CallbackData=Nav.BackToMain},
            new InlineKeyboardButton{Text=Nav.RestartText,CallbackData=Nav.Restart}
        });
        var kb=new InlineKeyboardMarkup{InlineKeyboard=rows};
        await SetScrDataAsync(userId,new ScrData{Screen="btn",ButtonId=btn.Id});
        await SendOrEditAsync(chatId,editId,btn.Title,kb,ct);
    }

    private async Task SendCakeScreenAsync(string userId,string chatId,long? editId,int cakeId,CancellationToken ct)
    {
        var cake=await _menu.GetCakeItemAsync(cakeId);
        if(cake==null) return;
        await SetScrDataAsync(userId,new ScrData{Screen="cake",CakeId=cakeId});
        var photos=cake.Medias.OrderBy(m=>m.SortOrder).Select(m=>m.PhotoPath).ToList();
        var caption=cake.Medias.OrderBy(m=>m.SortOrder).FirstOrDefault()?.Caption??string.Empty;
        if(editId.HasValue)
        {
            await _api.EditMessage(chatId,editId.Value,"نمایش جزئیات کیک",null,ct);
        }
        if(photos.Count>0)
        {
            await _api.SendMediaGroup(chatId,photos,caption,ct);
        }
        else
        {
            await _api.SendMessage(chatId,caption,null,ct);
        }
        var navRow=new List<List<InlineKeyboardButton>>
        {
            new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton{Text=Nav.BackText,CallbackData=Nav.Back},
                new InlineKeyboardButton{Text=Nav.MainText,CallbackData=Nav.BackToMain},
                new InlineKeyboardButton{Text=Nav.RestartText,CallbackData=Nav.Restart}
            }
        };
        var kb=new InlineKeyboardMarkup{InlineKeyboard=navRow};
        await _api.SendMessage(chatId,"برای ادامه یکی از گزینه‌های زیر را انتخاب کنید",kb,ct);
    }

    private async Task GoBackAsync(string userId,string chatId,long? editId,CancellationToken ct)
    {
        var scr=await GetScrDataAsync(userId);
        if(scr.Screen=="cake"&&scr.CakeId.HasValue)
        {
            var cake=await _menu.GetCakeItemAsync(scr.CakeId.Value);
            if(cake!=null)
            {
                await SendButtonScreenAsync(userId,chatId,editId,cake.MenuButtonId,ct);
                return;
            }
        }
        if(scr.Screen=="btn"&&scr.ButtonId.HasValue)
        {
            var btn=await _menu.GetButtonAsync(scr.ButtonId.Value);
            if(btn!=null&&btn.ParentId.HasValue)
            {
                await SendButtonScreenAsync(userId,chatId,editId,btn.ParentId.Value,ct);
                return;
            }
        }
        await SendMainMenuAsync(userId,chatId,editId,ct);
    }

    private async Task SendOrEditAsync(string chatId,long? editId,string text,object? kb,CancellationToken ct)
    {
        if(editId.HasValue)
        {
            var ok=await _api.EditMessage(chatId,editId.Value,text,kb,ct);
            if(ok) return;
        }
        await _api.SendMessage(chatId,text,kb,ct);
    }

    private async Task SendLongAsync(string chatId,string text,long? editId,CancellationToken ct)
    {
        const int max=3500;
        if(text.Length<=max)
        {
            await SendOrEditAsync(chatId,editId,text,Keyboards.KeyboardsWithBack(),ct);
            return;
        }
        if(editId.HasValue)
        {
            await _api.EditMessage(chatId,editId.Value,"خروجی در چند پیام ارسال می‌شود",null,ct);
        }
        int pos=0;
        while(pos<text.Length)
        {
            int len=Math.Min(max,text.Length-pos);
            if(pos+len<text.Length)
            {
                int nl=text.LastIndexOf('\n',pos+len-1,Math.Min(len,200));
                if(nl>pos) len=nl-pos+1;
            }
            await _api.SendMessage(chatId,text.Substring(pos,len),null,ct);
            pos+=len;
        }
    }

    private static string NormalizeDigits(string s)
    {
        var sb=new StringBuilder(s.Length);
        foreach(var c in s)
        {
            if(c>='۰'&&c<='۹') sb.Append((char)('0'+(c-'۰')));
            else if(c>='٠'&&c<='٩') sb.Append((char)('0'+(c-'٠')));
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private async Task<AdmData> GetAdmDataAsync(string userId)
    {
        var raw=await _users.GetSessionDataAsync(userId);
        if(string.IsNullOrEmpty(raw)) return new AdmData();
        try
        {
            return JsonSerializer.Deserialize<AdmData>(raw)??new AdmData();
        }
        catch
        {
            return new AdmData();
        }
    }

    private async Task SetAdmDataAsync(string userId,AdmData data)
    {
        var state=await _users.GetSessionStateAsync(userId);
        var json=JsonSerializer.Serialize(data);
        await _users.SetSessionAsync(userId,state,json);
    }

    private async Task<ScrData> GetScrDataAsync(string userId)
    {
        var raw=await _users.GetSessionDataAsync(userId);
        if(string.IsNullOrEmpty(raw)) return new ScrData();
        try
        {
            return JsonSerializer.Deserialize<ScrData>(raw)??new ScrData();
        }
        catch
        {
            return new ScrData();
        }
    }

    private async Task SetScrDataAsync(string userId,ScrData data)
    {
        var json=JsonSerializer.Serialize(data);
        await _users.SetSessionAsync(userId,"screen",json);
    }
}
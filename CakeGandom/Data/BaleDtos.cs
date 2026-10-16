using System.Text.Json.Serialization;

namespace CakeGandom.Dtos;

public class BaleResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok{get;set;}
    [JsonPropertyName("result")]
    public T? Result{get;set;}
    [JsonPropertyName("description")]
    public string? Description{get;set;}
    [JsonPropertyName("error_code")]
    public int? ErrorCode{get;set;}
}

public class Update
{
    [JsonPropertyName("update_id")]
    public long UpdateId{get;set;}
    [JsonPropertyName("message")]
    public Message? Message{get;set;}
    [JsonPropertyName("callback_query")]
    public CallbackQuery? CallbackQuery{get;set;}
}

public class Message
{
    [JsonPropertyName("message_id")]
    public long MessageId{get;set;}
    [JsonPropertyName("from")]
    public UserFrom? From{get;set;}
    [JsonPropertyName("chat")]
    public Chat? Chat{get;set;}
    [JsonPropertyName("date")]
    public long Date{get;set;}
    [JsonPropertyName("text")]
    public string? Text{get;set;}
    [JsonPropertyName("caption")]
    public string? Caption{get;set;}
    [JsonPropertyName("photo")]
    public List<PhotoSize>? Photo{get;set;}
}

public class UserFrom
{
    [JsonPropertyName("id")]
    public long Id{get;set;}
    [JsonPropertyName("first_name")]
    public string? FirstName{get;set;}
    [JsonPropertyName("last_name")]
    public string? LastName{get;set;}
    [JsonPropertyName("username")]
    public string? Username{get;set;}
}

public class Chat
{
    [JsonPropertyName("id")]
    public long Id{get;set;}
    [JsonPropertyName("type")]
    public string? Type{get;set;}
}

public class PhotoSize
{
    [JsonPropertyName("file_id")]
    public string? FileId{get;set;}
    [JsonPropertyName("width")]
    public int Width{get;set;}
    [JsonPropertyName("height")]
    public int Height{get;set;}
    [JsonPropertyName("file_size")]
    public long? FileSize{get;set;}
}

public class CallbackQuery
{
    [JsonPropertyName("id")]
    public string Id{get;set;}=string.Empty;
    [JsonPropertyName("from")]
    public UserFrom? From{get;set;}
    [JsonPropertyName("message")]
    public Message? Message{get;set;}
    [JsonPropertyName("data")]
    public string? Data{get;set;}
}

public class InlineKeyboardMarkup
{
    [JsonPropertyName("inline_keyboard")]
    public List<List<InlineKeyboardButton>> InlineKeyboard{get;set;}=new();
}

public class InlineKeyboardButton
{
    [JsonPropertyName("text")]
    public string Text{get;set;}=string.Empty;
    [JsonPropertyName("callback_data")]
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public string? CallbackData{get;set;}
    [JsonPropertyName("url")]
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public string? Url{get;set;}
}

public class ReplyKeyboardMarkup
{
    [JsonPropertyName("keyboard")]
    public List<List<ReplyKeyboardButton>> Keyboard{get;set;}=new();
    [JsonPropertyName("resize_keyboard")]
    public bool ResizeKeyboard{get;set;}=true;
    [JsonPropertyName("one_time_keyboard")]
    public bool OneTimeKeyboard{get;set;}
}

public class ReplyKeyboardButton
{
    [JsonPropertyName("text")]
    public string Text{get;set;}=string.Empty;
}

public class SendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId{get;set;}=string.Empty;
    [JsonPropertyName("text")]
    public string Text{get;set;}=string.Empty;
    [JsonPropertyName("reply_markup")]
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public object? ReplyMarkup{get;set;}
    [JsonPropertyName("parse_mode")]
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public string? ParseMode{get;set;}
}

public class EditMessageRequest
{
    [JsonPropertyName("chat_id")]
    public string ChatId{get;set;}=string.Empty;
    [JsonPropertyName("message_id")]
    public long MessageId{get;set;}
    [JsonPropertyName("text")]
    public string Text{get;set;}=string.Empty;
    [JsonPropertyName("reply_markup")]
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public object? ReplyMarkup{get;set;}
}

public class SendMessageResult
{
    [JsonPropertyName("message_id")]
    public long MessageId{get;set;}
}

public static class Nav
{
    public const string Back="nav_back";
    public const string BackToMain="nav_main";
    public const string Restart="nav_restart";
    public const string BackText="🔙 بازگشت";
    public const string MainText="🏠 منوی اصلی";
    public const string RestartText="♻ راه‌اندازی مجدد";
    public const string PanelSeparatorText="⬇️⬇️⬇️پنل کاربری⬇️⬇️⬇️";
    public const string AddPrefix="adm_add:";
    public const string EditPrefix="adm_edit:";
    public const string DeletePrefix="adm_del:";
    public const string PosPrefix="adm_pos:";
    public const string ConfirmPrefix="adm_ok:";
    public const string CancelPrefix="adm_cancel:";
    public const string MenuPrefix="mnu:";
    public const string CakePrefix="cake:";
    public const string UserExport="usr_export";
    public const string ManageMenu="mng_root";
    public const string Noop="noop";
    public const string UserExportText="usr_export_text";
    public const string MenuAddText="➕ افزودن";
    public const string MenuEditText="✏️ ویرایش";
    public const string MenuDeleteText="🗑 حذف";
    public const string MenuMoveText="🔀 جابجایی";

}

public static class Keyboards
{
    public static InlineKeyboardMarkup KeyboardsWithBack()
    {
        return new InlineKeyboardMarkup
        {
            InlineKeyboard=new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text=Nav.BackText,CallbackData=Nav.Back},
                    new InlineKeyboardButton{Text=Nav.MainText,CallbackData=Nav.BackToMain},
                    new InlineKeyboardButton{Text=Nav.RestartText,CallbackData=Nav.Restart}
                }
            }
        };
    }

    public static InlineKeyboardMarkup KeyboardsWithCancel()
    {
        return new InlineKeyboardMarkup
        {
            InlineKeyboard=new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    new InlineKeyboardButton{Text=Nav.MainText,CallbackData=Nav.BackToMain},
                    new InlineKeyboardButton{Text=Nav.RestartText,CallbackData=Nav.Restart}
                }
            }
        };
    }

    public static InlineKeyboardMarkup KeyboardsWithExtra(List<List<InlineKeyboardButton>> rows,bool withNav)
    {
        var all=new List<List<InlineKeyboardButton>>(rows);
        if(withNav)
        {
            all.Add(new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton{Text=Nav.BackText,CallbackData=Nav.Back},
                new InlineKeyboardButton{Text=Nav.MainText,CallbackData=Nav.BackToMain},
                new InlineKeyboardButton{Text=Nav.RestartText,CallbackData=Nav.Restart}
            });
        }
        return new InlineKeyboardMarkup{InlineKeyboard=all};
    }

    public static InlineKeyboardMarkup FromTitles(List<(string Title,string Callback)> items)
    {
        var rows=new List<List<InlineKeyboardButton>>();
        foreach(var item in items)
        {
            rows.Add(new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton{Text=item.Title,CallbackData=item.Callback}
            });
        }
        return new InlineKeyboardMarkup{InlineKeyboard=rows};
    }

    public static ReplyKeyboardMarkup DynamicMainReply(List<List<string>> rows)
    {
        var kb=new List<List<ReplyKeyboardButton>>();
        foreach(var row in rows)
        {
            var r=new List<ReplyKeyboardButton>();
            foreach(var t in row)
            {
                if(string.IsNullOrWhiteSpace(t)) continue;
                r.Add(new ReplyKeyboardButton{Text=t});
            }
            if(r.Count>0) kb.Add(r);
        }
        return new ReplyKeyboardMarkup{Keyboard=kb,ResizeKeyboard=true};
    }
}
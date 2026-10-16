using System.Net.Http.Json;
using System.Text.Json;
using CakeGandom.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CakeGandom.Services;

public class BaleApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<BaleApiClient> _logger;
    private readonly string _baseUrl;
    private static readonly JsonSerializerOptions _json=new(JsonSerializerDefaults.Web);

    public BaleApiClient(HttpClient http,IConfiguration cfg,ILogger<BaleApiClient> logger)
    {
        _http=http;
        _logger=logger;
        var root=cfg["Bot:BaseUrl"]??"https://tapi.bale.ai/bot";
        var token=cfg["Bot:Token"]??string.Empty;
        _baseUrl=$"{root}{token}";
        _http.Timeout=TimeSpan.FromSeconds(60);
    }

    private async Task<BaleResponse<T>?> PostJson<T>(string method,object payload,CancellationToken ct=default)
    {
        try
        {
            var resp=await _http.PostAsJsonAsync($"{_baseUrl}/{method}",payload,_json,ct);
            if(!resp.IsSuccessStatusCode)
            {
                var body=await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Bale api {Method} status {Status} body {Body}",method,(int)resp.StatusCode,body);
                return null;
            }
            return await resp.Content.ReadFromJsonAsync<BaleResponse<T>>(_json,ct);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"Bale api {Method} failed",method);
            return null;
        }
    }

    public async Task<long?> SendMessage(string chatId,string text,object? replyMarkup=null,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(chatId)||string.IsNullOrWhiteSpace(text)) return null;
        var req=new SendMessageRequest{ChatId=chatId,Text=text,ReplyMarkup=replyMarkup};
        var res=await PostJson<SendMessageResult>("sendMessage",req,ct);
        return res!=null&&res.Ok?res.Result?.MessageId:null;
    }

    public async Task<bool> EditMessage(string chatId,long messageId,string text,object? replyMarkup=null,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(chatId)) return false;
        var req=new EditMessageRequest{ChatId=chatId,MessageId=messageId,Text=text,ReplyMarkup=replyMarkup};
        var res=await PostJson<SendMessageResult>("editMessageText",req,ct);
        return res!=null&&res.Ok;
    }

    public async Task<bool> AnswerCallback(string callbackId,string? text=null,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(callbackId)) return false;
        var payload=new Dictionary<string,object>{{"callback_query_id",callbackId}};
        if(!string.IsNullOrWhiteSpace(text)) payload["text"]=text;
        var res=await PostJson<object>("answerCallbackQuery",payload,ct);
        return res!=null&&res.Ok;
    }

    public async Task<bool> DeleteMessage(string chatId,long messageId,CancellationToken ct=default)
    {
        var payload=new Dictionary<string,object>{{"chat_id",chatId},{"message_id",messageId}};
        var res=await PostJson<object>("deleteMessage",payload,ct);
        return res!=null&&res.Ok;
    }

    public async Task<List<Update>> GetUpdates(long? offset=null,int timeoutSeconds=30,CancellationToken ct=default)
    {
        var url=$"{_baseUrl}/getUpdates?timeout={timeoutSeconds}";
        if(offset.HasValue) url+=$"&offset={offset.Value}";
        try
        {
            var resp=await _http.GetAsync(url,ct);
            if(!resp.IsSuccessStatusCode) return new List<Update>();
            var data=await resp.Content.ReadFromJsonAsync<BaleResponse<List<Update>>>(_json,ct);
            return data!=null&&data.Ok?(data.Result??new List<Update>()):new List<Update>();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"getUpdates failed");
            return new List<Update>();
        }
    }

    public async Task<bool> SetWebhook(string url,CancellationToken ct=default)
    {
        var res=await PostJson<object>("setWebhook",new Dictionary<string,object>{{"url",url}},ct);
        return res!=null&&res.Ok;
    }

    public async Task<bool> DeleteWebhook(CancellationToken ct=default)
    {
        var res=await PostJson<object>("deleteWebhook",new Dictionary<string,object>(),ct);
        return res!=null&&res.Ok;
    }

    public async Task<long?> SendDocument(string chatId,byte[] content,string fileName,string? caption=null,CancellationToken ct=default)
    {
        using var form=new MultipartFormDataContent();
        form.Add(new StringContent(chatId),"chat_id");
        if(!string.IsNullOrWhiteSpace(caption)) form.Add(new StringContent(caption),"caption");
        var file=new ByteArrayContent(content);
        file.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(file,"document",fileName);
        try
        {
            var resp=await _http.PostAsync($"{_baseUrl}/sendDocument",form,ct);
            var data=await resp.Content.ReadFromJsonAsync<BaleResponse<SendMessageResult>>(_json,ct);
            return data!=null&&data.Ok?data.Result?.MessageId:null;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"sendDocument failed");
            return null;
        }
    }

    private static bool IsLocalFile(string source)
    {
        if(string.IsNullOrWhiteSpace(source)) return false;
        if(source.StartsWith("http://")||source.StartsWith("https://")) return false;
        if(source.Length>200) return false;
        try
        {
            return File.Exists(source);
        }
        catch
        {
            return false;
        }
    }

    public async Task<long?> SendPhoto(string chatId,string photoSource,string? caption=null,object? replyMarkup=null,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(photoSource)) return null;
        if(!IsLocalFile(photoSource))
        {
            var payload=new Dictionary<string,object>
            {
                {"chat_id",chatId},
                {"photo",photoSource}
            };
            if(!string.IsNullOrWhiteSpace(caption)) payload["caption"]=caption;
            if(replyMarkup!=null) payload["reply_markup"]=replyMarkup;
            var res=await PostJson<SendMessageResult>("sendPhoto",payload,ct);
            return res!=null&&res.Ok?res.Result?.MessageId:null;
        }
        using var form=new MultipartFormDataContent();
        form.Add(new StringContent(chatId),"chat_id");
        if(!string.IsNullOrWhiteSpace(caption)) form.Add(new StringContent(caption),"caption");
        if(replyMarkup!=null) form.Add(new StringContent(JsonSerializer.Serialize(replyMarkup,_json)),"reply_markup");
        await using var stream=File.OpenRead(photoSource);
        using var file=new StreamContent(stream);
        form.Add(file,"photo",Path.GetFileName(photoSource));
        try
        {
            var resp=await _http.PostAsync($"{_baseUrl}/sendPhoto",form,ct);
            var data=await resp.Content.ReadFromJsonAsync<BaleResponse<SendMessageResult>>(_json,ct);
            return data!=null&&data.Ok?data.Result?.MessageId:null;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"sendPhoto upload failed");
            return null;
        }
    }

    public async Task<List<long>> SendMediaGroup(string chatId,List<string> photoSources,string? caption=null,CancellationToken ct=default)
    {
        var valid=photoSources.Where(s=>!string.IsNullOrWhiteSpace(s)).ToList();
        if(valid.Count==0) return new List<long>();
        if(valid.Count==1)
        {
            var single=await SendPhoto(chatId,valid[0],caption,null,ct);
            return single.HasValue?new List<long>{single.Value}:new List<long>();
        }
        var allLocal=valid.All(IsLocalFile);
        if(allLocal)
        {
            return await SendMediaGroupUploadAsync(chatId,valid,caption,ct);
        }
        var fileIds=valid.Where(s=>!IsLocalFile(s)).ToList();
        if(fileIds.Count==1)
        {
            var results=new List<long>();
            var single=await SendPhoto(chatId,fileIds[0],caption,null,ct);
            if(single.HasValue) results.Add(single.Value);
            return results;
        }
        var media=new List<Dictionary<string,object>>();
        for(int i=0;i<fileIds.Count;i++)
        {
            media.Add(new Dictionary<string,object>
            {
                {"type","photo"},
                {"media",fileIds[i]},
                {"caption",i==0?(caption??string.Empty):string.Empty}
            });
        }
        var payload=new Dictionary<string,object>
        {
            {"chat_id",chatId},
            {"media",media}
        };
        var res=await PostJson<List<SendMessageResult>>("sendMediaGroup",payload,ct);
        if(res!=null&&res.Ok&&res.Result!=null)
            return res.Result.Select(x=>x.MessageId).ToList();
        return new List<long>();
    }

    private async Task<List<long>> SendMediaGroupUploadAsync(string chatId,List<string> localPaths,string? caption,CancellationToken ct)
    {
        using var form=new MultipartFormDataContent();
        form.Add(new StringContent(chatId),"chat_id");
        var media=new List<Dictionary<string,object>>();
        var streams=new List<FileStream>();
        for(int i=0;i<localPaths.Count;i++)
        {
            media.Add(new Dictionary<string,object>
            {
                {"type","photo"},
                {"media",$"attach://photo{i}"},
                {"caption",i==0?(caption??string.Empty):string.Empty}
            });
            var stream=File.OpenRead(localPaths[i]);
            streams.Add(stream);
            form.Add(new StreamContent(stream),$"photo{i}",Path.GetFileName(localPaths[i]));
        }
        form.Add(new StringContent(JsonSerializer.Serialize(media,_json)),"media");
        try
        {
            var resp=await _http.PostAsync($"{_baseUrl}/sendMediaGroup",form,ct);
            var data=await resp.Content.ReadFromJsonAsync<BaleResponse<List<SendMessageResult>>>(_json,ct);
            if(data!=null&&data.Ok&&data.Result!=null) return data.Result.Select(x=>x.MessageId).ToList();
            return new List<long>();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex,"sendMediaGroup upload failed");
            return new List<long>();
        }
        finally
        {
            foreach(var s in streams) s.Dispose();
        }
    }
}
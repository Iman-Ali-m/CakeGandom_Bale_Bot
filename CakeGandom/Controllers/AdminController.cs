using CakeGandom.Dtos;
using CakeGandom.Services;
using Microsoft.AspNetCore.Mvc;

namespace CakeGandom.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController:ControllerBase
{
    private readonly MenuService _menu;
    private readonly UserService _users;
    private readonly RegistrationService _reg;
    private readonly IConfiguration _cfg;

    public AdminController(MenuService menu,UserService users,RegistrationService reg,IConfiguration cfg)
    {
        _menu=menu;
        _users=users;
        _reg=reg;
        _cfg=cfg;
    }

    private bool KeyOk()
    {
        var expected=_cfg["Admin:ApiKey"];
        if(string.IsNullOrEmpty(expected)) return false;
        var provided=Request.Headers["X-Api-Key"].ToString();
        return expected==provided;
    }

    [HttpGet("menu/tree")]
    public async Task<IActionResult> MenuTree()
    {
        if(!KeyOk()) return Unauthorized();
        var text=await _menu.BuildMenuTreeTextAsync();
        return Ok(new{tree=text});
    }

    [HttpGet("menu/level/{level}")]
    public async Task<IActionResult> MenuByLevel(int level,int? parentId)
    {
        if(!KeyOk()) return Unauthorized();
        var list=await _menu.GetAllButtonsByLevelAsync(level,parentId);
        return Ok(list.Select(x=>new{x.Id,x.Title,x.Position,x.IsSuperOnly,x.CallbackData,x.ParentId,x.Level}));
    }

    [HttpPost("menu/button")]
    public async Task<IActionResult> AddButton([FromBody] AddButtonDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var btn=await _menu.AddButtonAsync(dto.Level,dto.ParentId,dto.Title,dto.Position,dto.IsSuperOnly,dto.CallbackData);
        return Ok(new{ok=true,id=btn.Id});
    }

    [HttpPut("menu/button/{id}")]
    public async Task<IActionResult> EditButton(int id,[FromBody] EditButtonDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.EditButtonAsync(id,dto.Title,dto.CallbackData,dto.IsSuperOnly);
        return Ok(new{ok});
    }

    [HttpDelete("menu/button/{id}")]
    public async Task<IActionResult> DeleteButton(int id)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.DeleteButtonAsync(id);
        return Ok(new{ok});
    }

    [HttpPut("menu/button/{id}/move")]
    public async Task<IActionResult> MoveButton(int id,[FromBody] MoveDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.MoveButtonAsync(id,dto.NewPosition);
        return Ok(new{ok});
    }

    [HttpGet("cake/{menuButtonId}")]
    public async Task<IActionResult> GetCakes(int menuButtonId)
    {
        if(!KeyOk()) return Unauthorized();
        var list=await _menu.GetCakeItemsAsync(menuButtonId);
        return Ok(list.Select(x=>new{x.Id,x.Title,x.Position}));
    }

    [HttpGet("cake/item/{id}")]
    public async Task<IActionResult> GetCake(int id)
    {
        if(!KeyOk()) return Unauthorized();
        var cake=await _menu.GetCakeItemAsync(id);
        if(cake==null) return NotFound();
        return Ok(new
        {
            cake.Id,
            cake.Title,
            cake.Position,
            cake.MenuButtonId,
            Medias=cake.Medias.Select(m=>new{m.Id,m.PhotoPath,m.Caption,m.SortOrder})
        });
    }

    [HttpPost("cake")]
    public async Task<IActionResult> AddCake([FromBody] AddCakeDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var item=await _menu.AddCakeItemAsync(dto.MenuButtonId,dto.Title,dto.Position,dto.Photos,dto.Caption);
        return Ok(new{ok=true,id=item.Id});
    }

    [HttpPut("cake/{id}")]
    public async Task<IActionResult> EditCake(int id,[FromBody] EditCakeDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.EditCakeItemAsync(id,dto.Title,dto.Caption,dto.Photos);
        return Ok(new{ok});
    }

    [HttpDelete("cake/{id}")]
    public async Task<IActionResult> DeleteCake(int id)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.DeleteCakeItemAsync(id);
        return Ok(new{ok});
    }

    [HttpPut("cake/{id}/move")]
    public async Task<IActionResult> MoveCake(int id,[FromBody] MoveDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        var ok=await _menu.MoveCakeItemAsync(id,dto.NewPosition);
        return Ok(new{ok});
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        if(!KeyOk()) return Unauthorized();
        var users=await _users.GetRegisteredUsersAsync();
        var protector=HttpContext.RequestServices.GetRequiredService<PhoneProtector>();
        var result=new List<UserExportItem>();
        foreach(var u in users)
        {
            var p=u.Profile;
            var phone=string.Empty;
            if(p!=null&&!string.IsNullOrEmpty(p.PhoneEncrypted))
                phone=protector.Decrypt(p.PhoneEncrypted);
            result.Add(new UserExportItem
            {
                UserId=u.UserId,
                FirstName=p?.FirstName,
                LastName=p?.LastName,
                Phone=phone,
                BaleUsername=p?.BaleUsernameContact,
                FirstSeen=u.FirstSeen,
                LastSeen=u.LastSeen,
                IsSuperUser=u.IsSuperUser,
                IsAdmin=u.IsAdmin
            });
        }
        return Ok(result);
    }

    [HttpGet("users/export")]
    public async Task<IActionResult> UsersExport()
    {
        if(!KeyOk()) return Unauthorized();
        var text=await _reg.BuildUsersExportAsync();
        return Ok(new{text});
    }

    [HttpGet("settings/{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        if(!KeyOk()) return Unauthorized();
        var value=await _menu.GetSettingAsync(key);
        return Ok(new{key,value});
    }

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> SetSetting(string key,[FromBody] SettingDto dto)
    {
        if(!KeyOk()) return Unauthorized();
        await _menu.SetSettingAsync(key,dto.Value);
        return Ok(new{ok=true});
    }
}
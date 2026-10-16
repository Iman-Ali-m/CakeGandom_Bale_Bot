namespace CakeGandom.Models;

public class User
{
    public string UserId{get;set;}=string.Empty;
    public string? BaleUsername{get;set;}
    public long FirstSeen{get;set;}
    public long LastSeen{get;set;}
    public bool IsRegistered{get;set;}
    public bool IsBlocked{get;set;}
    public bool IsSuperUser{get;set;}
    public bool IsAdmin{get;set;}
    public long CreatedAt{get;set;}
    public Profile? Profile{get;set;}
}

public class Profile
{
    public int Id{get;set;}
    public string UserId{get;set;}=string.Empty;
    public string? FirstName{get;set;}
    public string? LastName{get;set;}
    public string? PhoneEncrypted{get;set;}
    public string? BaleUsernameContact{get;set;}
    public string RegistrationStep{get;set;}="start";
    public long CreatedAt{get;set;}
    public long UpdatedAt{get;set;}
    public User? User{get;set;}
}

public class MenuButton
{
    public int Id{get;set;}
    public int Level{get;set;}
    public int? ParentId{get;set;}
    public string Title{get;set;}=string.Empty;
    public string? CallbackData{get;set;}
    public int Position{get;set;}
    public bool IsSuperOnly{get;set;}
    public bool IsActive{get;set;}=true;
    public long CreatedAt{get;set;}
    public MenuButton? Parent{get;set;}
    public List<MenuButton> Children{get;set;}=new();
    public List<CakeItem> CakeItems{get;set;}=new();
}

public class CakeItem
{
    public int Id{get;set;}
    public int MenuButtonId{get;set;}
    public string Title{get;set;}=string.Empty;
    public int Position{get;set;}
    public long CreatedAt{get;set;}
    public MenuButton? MenuButton{get;set;}
    public List<CakeMedia> Medias{get;set;}=new();
}

public class CakeMedia
{
    public int Id{get;set;}
    public int CakeItemId{get;set;}
    public string PhotoPath{get;set;}=string.Empty;
    public string? Caption{get;set;}
    public int SortOrder{get;set;}
    public CakeItem? CakeItem{get;set;}
}

public class UserSession
{
    public string UserId{get;set;}=string.Empty;
    public string State{get;set;}="idle";
    public string? TempData{get;set;}
    public long UpdatedAt{get;set;}
}

public class BotSetting
{
    public string Key{get;set;}=string.Empty;
    public string? Value{get;set;}
}
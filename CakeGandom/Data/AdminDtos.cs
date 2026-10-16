namespace CakeGandom.Dtos;

public class AddButtonDto
{
    public int Level{get;set;}
    public int? ParentId{get;set;}
    public string Title{get;set;}=string.Empty;
    public int Position{get;set;}
    public bool IsSuperOnly{get;set;}
    public string? CallbackData{get;set;}
}

public class EditButtonDto
{
    public string? Title{get;set;}
    public string? CallbackData{get;set;}
    public bool? IsSuperOnly{get;set;}
}

public class MoveDto
{
    public int NewPosition{get;set;}
}

public class AddCakeDto
{
    public int MenuButtonId{get;set;}
    public string Title{get;set;}=string.Empty;
    public int Position{get;set;}
    public List<string> Photos{get;set;}=new();
    public string? Caption{get;set;}
}

public class EditCakeDto
{
    public string? Title{get;set;}
    public string? Caption{get;set;}
    public List<string>? Photos{get;set;}
}

public class SettingDto
{
    public string Value{get;set;}=string.Empty;
}

public class UserExportItem
{
    public string UserId{get;set;}=string.Empty;
    public string? FirstName{get;set;}
    public string? LastName{get;set;}
    public string? Phone{get;set;}
    public string? BaleUsername{get;set;}
    public long FirstSeen{get;set;}
    public long LastSeen{get;set;}
    public bool IsSuperUser{get;set;}
    public bool IsAdmin{get;set;}
}
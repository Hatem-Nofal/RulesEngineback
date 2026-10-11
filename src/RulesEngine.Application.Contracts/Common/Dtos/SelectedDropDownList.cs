namespace RulesEngine.Common.Dtos;

public class SelectedDropDownList
{
    public long Id { get; private set; }
    public string Name { get; private set; }
    public bool Disabled { get; private set; }
    public string? Code { get; private set; }

    public SelectedDropDownList(long id, string name, bool disabled)
    {
        Id = id;
        Name = name;
        Disabled = disabled;
    }
    public SelectedDropDownList(long id, string name,string? code, bool disabled)
    {
        Id = id;
        Name = name;
        Code = code;
        Disabled = disabled;
    }
    public SelectedDropDownList(long id, string name)
    {
        Id = id;
        Name = name;
    }
}
namespace CathermanCore;

public class TaskResult
{
    public bool Success { get; set; } = true;
    public string Key { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Data { get; set; } = null;

    public bool SameReference { get; set; }
}
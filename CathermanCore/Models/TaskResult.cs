namespace CathermanCore;

public class TaskResult
{
    public bool Success { get; set; } = true;
    public bool HasWarning { get; set; } = false;
    public string Key { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Data { get; set; } = null;

    public string Reason { get; set; } = "";


    public static TaskResult Fail(string key, string message, object? data = null, string reason = "") =>
        new() { Success = false, Key = key, Message = message, Data = data, Reason = reason };

    public void SetFailed(string key, string message, object? data = null, string reason = "")
    { 
        Success = false; 
        Key = key;
        Message = message;
        Data = data;
        Reason = reason;
    }
    public static TaskResult Warning(string key, string message, object? data = null, string reason = "") =>
        new() { Success = true, HasWarning = true, Key = key, Message = message, Data = data, Reason = reason };


    public void SetWarning(string key, string message, object? data = null, string failReason = "")
    { 
        Success = false; 
        HasWarning = true;
        Key = key;
        Message = message;
        Data = data;
        Reason = failReason;
    }

    public static TaskResult Succeed(object? data = null) =>
        new() { Success = true , Data = data };
}
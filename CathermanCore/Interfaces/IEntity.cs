namespace CathermanCore;

public interface IEntity : IHasId<int>, ISoftDelete
{
    DateTime Added { get; set; }
    DateTime Updated { get; set; }
    string LastUpdateBy { get; set; }
    bool IsValid();
}

public interface ISoftDelete
{
    bool IsLive { get; set; }
}

public interface IConnectingEntity : ISoftDelete { }
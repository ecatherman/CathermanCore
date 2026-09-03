using System.ComponentModel.DataAnnotations;
using CathermanCore;

namespace CathermanCore;
public abstract class Entity : IEntity
{
    [Key]
    public int Id { get; set; }
    public bool IsLive { get; set; } = true;
    public DateTime Added { get; set; }
    public DateTime Updated { get; set; }
    public string LastUpdateBy { get; set; } = "";
    public string SourceData { get; set; } = "N/A";
    public override bool Equals(object? obj) => 
        obj != null && obj is Entity e && Id != 0 && e.Id != 0 && Id == e.Id;
    public override int GetHashCode() =>
        Id.GetHashCode();
    public virtual bool IsValid() => true;
}

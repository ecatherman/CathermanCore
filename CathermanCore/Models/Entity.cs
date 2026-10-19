using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CathermanCore;
public abstract class Entity : IEntity
{
    [Key]
    public int Id { get; set; }
    public bool IsLive { get; set; } = true;
    public DateTime Added { get; set; }
    public DateTime Updated { get; set; }
    [DefaultValue("")]
    public string LastUpdateBy { get; set; } = "";
    [DefaultValue("N/A")]
    public string SourceData { get; set; } = "N/A";
    public override bool Equals(object? obj) =>
       ReferenceEquals(this, obj) || (obj is Entity e && GetType() == e.GetType() && Id != 0 && Id == e.Id);
    public override int GetHashCode() =>
        Id != 0 ? HashCode.Combine(GetType(), Id) : RuntimeHelpers.GetHashCode(this);
    public virtual bool IsValid() => true;
}

using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;

namespace CathermanCore;

public class SerilogCustomModelDestructuringPolicy : IDestructuringPolicy
{
    /// <summary>
    /// Optional. Types in this namespace (for example "MyApp.Models") are logged as full objects.
    /// Set it once at startup. Leave it null to only special-case <see cref="IEntity"/>.
    /// </summary>
    public static string? ModelNamespace { get; set; }

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        // 1. Entities are logged using their own JSON shape
        if (value is IEntity entity)
        {
            var customShape = JObject.Parse(entity.ToJson());
            result = propertyValueFactory.CreatePropertyValue(customShape, destructureObjects: true);
            return true;
        }

        // 2. Optionally log every model in the configured namespace as a full object
        if (ModelNamespace != null && value != null && value.GetType().Namespace == ModelNamespace)
        {
            // You can use Reflection or Anonymous types to sanitize or shape objects globally
            result = propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
            return true;
        }

        result = null!;
        return false;
    }
}
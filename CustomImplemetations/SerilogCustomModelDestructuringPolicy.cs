using Newtonsoft.Json.Linq;
using Serilog.Core;
using Serilog.Events;

namespace CathermanCore;

public class SerilogCustomModelDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        // 1. Specific Customization for 'Work' Model
        if (value is IEntity entity)
        {
            var customShape = JObject.Parse(entity.ToJson());
            result = propertyValueFactory.CreatePropertyValue(customShape, destructureObjects: true);
            return true;
        }

        // 2. Generic Customization for All Other Models in 'BibleWork.Models'
        if (value != null && value.GetType().Namespace == "BibleWork.Models")
        {
            // You can use Reflection or Anonymous types to sanitize or shape objects globally
            result = propertyValueFactory.CreatePropertyValue(value, destructureObjects: true);
            return true;
        }

        result = null!;
        return false;
    }
}
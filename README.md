# CathermanCore

Shared .NET libraries used across my projects.

| Project | What it is |
| --- | --- |
| `CathermanCore` | Entity base types, settings, logging setup (Serilog), text and collection helpers |
| `CathermanCore.EntityFramework` | `BaseContext`: soft delete, Added/Updated/LastUpdateBy stamping, and query safety warnings for EF Core |
| `CathermanCore.Web` | ASP.NET Core helpers and tag helpers |
| `*.Tests` | xUnit tests |

## Logging

The application name written to logs is a setting (`Log:Application`), and
`SerilogCustomModelDestructuringPolicy.ModelNamespace` can be set at startup to log every model in a namespace as a full object.

## License

[MIT](LICENSE)

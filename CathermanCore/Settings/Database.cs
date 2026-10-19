namespace CathermanCore.Settings;

public class Database
{
    public string ServerName { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string UserName {  get; set; } = "";
    public string Password { get; set; } = "";
    public bool TrustedConnection { get; set; }
    public bool TrustServerCertificate { get; set; }

    public string GetConnectionString()
    {
        var bulder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = ServerName,
            InitialCatalog = DatabaseName,
            UserID = UserName,
            Password = Password,
            TrustServerCertificate = TrustServerCertificate
        };
        return bulder.ConnectionString;
    }
}
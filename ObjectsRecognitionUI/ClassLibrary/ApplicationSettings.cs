namespace ClassLibrary;

public class AppSettings
{
    public ConnectionStringsConfig ConnectionStrings { get; set; }
}

public class ConnectionStringsConfig
{
    public string dbStringConnection { get; set; }
    public string srsStringConnection { get; set; }
}

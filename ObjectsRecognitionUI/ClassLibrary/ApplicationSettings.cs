namespace ClassLibrary;

public class AppSettings
{
    public ConnectionStringsConfig ConnectionStrings { get; set; }

    public int NeuralWatcherTimeout { get; set; }

    public FrameRate FrameRate { get; set; }
}

public class ConnectionStringsConfig
{
    public string dbStringConnection { get; set; }
    public string srsStringConnection { get; set; }
}

public class FrameRate
{
    public int Value { get; set; }
}

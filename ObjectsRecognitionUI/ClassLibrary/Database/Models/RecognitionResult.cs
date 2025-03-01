namespace ClassLibrary.Database.Models;

public class RecognitionResult
{
    public int Id { get; set; }
    public required string ClassName { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Width { get; set; }
    public required float Height { get; set; }
}

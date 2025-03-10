﻿namespace ClassLibrary.Database.Models;

public class RecognitionResult
{
    public int Id { get; set; }
    public required string ClassName { get; set; }
    public required int X { get; set; }
    public required int Y { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
}

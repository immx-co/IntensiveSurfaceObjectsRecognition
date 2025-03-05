using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Configuration;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI.Services;

public class VideoService
{
    private IConfiguration _configuration;

    public VideoService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<List<Bitmap>> GetFramesAsync(IStorageFile file)
    {
        var bitmapImages = new List<Bitmap>();
        var capture = new VideoCapture(file.Path.LocalPath);
        var image = new Mat();

        int frameRate = Convert.ToInt32(_configuration.GetSection("FrameRate:Value").Value);
        int i = 0;
        await Task.Run(() =>
        {
            while (capture.IsOpened())
            {
                i++;
                capture.Read(image);
                if (image.Empty()) break;
                System.Drawing.Bitmap frame = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
                if (i % frameRate == 0) bitmapImages.Add(ConvertBitmapToAvalonia(frame));
            }
        });
        return bitmapImages;
    }

    private Bitmap ConvertBitmapToAvalonia(System.Drawing.Bitmap bitmap)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;

            return new Bitmap(memory);
        }
    }
}

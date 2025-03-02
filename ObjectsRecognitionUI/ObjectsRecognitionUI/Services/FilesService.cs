using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI.Services;

public class FilesService
{
    #region Private Fields
    public Window? Target => App.Current?.CurrentWindow;
    #endregion


    public async Task<IStorageFile?> OpenImageFileAsync()
    {
        var files = await Target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Image File",
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }
}

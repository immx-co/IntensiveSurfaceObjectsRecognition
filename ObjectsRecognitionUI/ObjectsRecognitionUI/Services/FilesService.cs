using Avalonia.Controls;
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
    private readonly Window _target;
    #endregion

    #region Constructors
    public FilesService(Window target)
    {
        _target = target;
    }
    #endregion

    public async Task<IStorageFile?> OpenImageFileAsync()
    {
        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Image File",
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }
}

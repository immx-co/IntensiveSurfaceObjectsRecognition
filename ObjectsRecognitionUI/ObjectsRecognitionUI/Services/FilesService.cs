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

    public async Task<List<IStorageFile>?> OpenImageFolderAsync()
    {
        var folder = await OpenFolederAsync();
        if (folder != null)
        {
            var files = folder?.GetItemsAsync().ToBlockingEnumerable();
            List<IStorageFile> imageFiles = new();
            foreach (var file in files)
            {
                if (file.Path.IsFile)
                {
                    var storageFile = await Target.StorageProvider.TryGetFileFromPathAsync(file.Path);
                    imageFiles.Add(storageFile);
                }
            }
            return imageFiles;
        }
        else return null;
    }

    private async Task<IStorageFolder?> OpenFolederAsync()
    {
        var folders = await Target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Open Image Folder",
            AllowMultiple = false,
        });

        return folders.Count >= 1 ? folders[0] : null;
    }
}

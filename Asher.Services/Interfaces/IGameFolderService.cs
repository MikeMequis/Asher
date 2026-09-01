using Asher.Core.Models;

namespace Asher.Services.Interfaces
{
    public interface IGameFolderService
    {
        GameFolderInfo DetectGameFolder();
        GameFolderInfo GetInfo(string folderPath);
    }
}

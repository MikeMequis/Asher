using Asher.Models;

namespace Asher.Services.Interfaces
{
    public interface IGameFolderService
    {
        GameFolderInfo DetectGameFolder();
        GameFolderInfo GetInfo(string folderPath);
        void CreatePatchesFolder(string folderPath);
    }
}
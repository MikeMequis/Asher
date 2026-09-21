using System.Diagnostics;

namespace Asher.Services.Interfaces
{
    /// <summary>
    /// Performs the actual process start. Injectable so launch behavior can be verified
    /// without launching a real game.
    /// </summary>
    public interface IProcessStarter
    {
        void Start(ProcessStartInfo startInfo);
    }
}

namespace CrusaderWars.Core.Process;

public interface IProcessController
{
    void SuspendProcess(string processName);
    void ResumeProcess(string processName);
    bool IsSupported { get; }
    bool IsProcessRunning(string processName);
    IReadOnlyList<int> FindPids(string processName);
    void KillProcess(string processName);
}

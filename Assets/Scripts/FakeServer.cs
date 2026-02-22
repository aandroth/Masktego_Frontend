//using System.Diagnostics;
//using System.IO;
//using UnityEngine;



//public class FakeServer : MonoBehaviour
//{
//    public string m_serverPath = "C:\\Users\\aandr\\Desktop\\Masktego_Server\\";
//    public string m_serverStartPs1File = "ServerStart.ps1";
//    public string m_serverKillCommand = "";
//    public Backend m_backend = null;
//    [SerializeField] int m_pId = 0;
//    [SerializeField] Process m_process = new Process();

//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        m_backend = FindAnyObjectByType<Backend>();
//    }

//    public async void StartServer()
//    {
//        UnityEngine.Debug.Log($"Starting fake server with command: {m_serverStartPs1File} in path: {m_serverPath}");
//        await RunPS();
//        //await ConnectToServer();
//    }

//    public System.Threading.Tasks.Task RunPS()
//    {
//        return System.Threading.Tasks.Task.Run(() =>
//        {
//            m_process = new Process();
//            UnityEngine.Debug.Log($"Creating Process");
//            var ps1File = Path.Combine(m_serverPath, m_serverStartPs1File);
//            m_process.StartInfo.FileName = "powershell.exe";
//            m_process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{ps1File}\"";
//            m_process.StartInfo.WorkingDirectory = m_serverPath;
//            m_process.StartInfo.CreateNoWindow = false;
//            m_process.StartInfo.UseShellExecute = false;
//            m_process.StartInfo.RedirectStandardOutput = true;
//            m_process.StartInfo.RedirectStandardError = true;
//            m_process.OutputDataReceived += (sender, args) => UnityEngine.Debug.LogWarning(args.Data);
//            m_process.ErrorDataReceived += (sender, args) => UnityEngine.Debug.LogError(args.Data);
//            m_process.Start();
//            m_pId = m_process.Id;
//            m_process.WaitForExit(60000); // Wait for 1 second to let the server start
//            m_process.BeginOutputReadLine();
//            m_process.BeginErrorReadLine();
//        });
//    }

//    public async System.Threading.Tasks.Task ConnectToServer()
//    {
//        await System.Threading.Tasks.Task.Delay(8000); // Wait for 2 seconds to let the server start
//        UnityEngine.Debug.Log($"Connecting to fake server");

//        await m_backend.StartWebSocketConnection();
//    }

//    public void OnDestroy()
//    {
//        if (m_process != null)
//        {
//            UnityEngine.Debug.Log("Killing the Server process");
//            m_process.Kill();
//        }
//    }
//}

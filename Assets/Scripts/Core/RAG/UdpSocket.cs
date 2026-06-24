using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Spawns the companion Python server, opens a UDP socket pair with it and
/// pushes the parsed payload into <see cref="RAGInput"/>.
///
/// Protocol: each datagram is a comma-separated string "action,step" (e.g.
/// "talk,2"). Anything malformed is logged and dropped.
/// </summary>
public class UdpSocket : MonoBehaviour
{
    public bool debug = true;
    public string pathWindows;
    public string pathLinux;

    [HideInInspector] public bool isTxStarted = false;

    [SerializeField] string IP = "127.0.0.1";
    [SerializeField] int rxPort = 8000; // Unity receives from Python on this port.
    [SerializeField] int txPort = 8001; // Unity sends to Python on this port.

    private UdpClient client;
    private IPEndPoint remoteEndPoint;
    private Thread receiveThread;
    private Process pythonProcess;

    void Awake()
    {
        StartPythonServer();

        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);
        client = new UdpClient(rxPort);

        receiveThread = new Thread(new ThreadStart(ReceiveData)) { IsBackground = true };
        receiveThread.Start();

        print("UDP Comms Initialised");
    }

    void OnDisable()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            try
            {
                pythonProcess.Kill();
                pythonProcess.Dispose();
                UnityEngine.Debug.Log("Python server stopped");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error stopping Python: " + e.Message);
            }
        }
    }

    public void SendData(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    // The `debug` flag toggles between the Linux dev path and the bundled Windows
    // path so the same scene works in both environments.
    void StartPythonServer()
    {
        try
        {
            string serverPath = debug ? pathLinux : pathWindows;
            if (string.IsNullOrEmpty(serverPath))
            {
                UnityEngine.Debug.LogError("Server path not set!");
                return;
            }

            string pythonExecutable = debug
                ? Path.Combine(serverPath, ".venv", "bin", "python")
                : Path.Combine(serverPath, ".venv", "Scripts", "python.exe");

            if (!File.Exists(pythonExecutable))
            {
                UnityEngine.Debug.LogError("Python executable not found: " + pythonExecutable);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = "index.py",
                WorkingDirectory = serverPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            pythonProcess = new Process { StartInfo = startInfo };

            pythonProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.Log("[PY] " + args.Data);
            };

            pythonProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.LogError("[PY ERROR] " + args.Data);
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log("Python server started");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Error starting Python: " + e.Message);
        }
    }

    // Runs on a background thread; blocks on client.Receive until a datagram arrives.
    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                print(">> " + text);
                ProcessInput(text);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    private void ProcessInput(string input)
    {
        if (!isTxStarted) isTxStarted = true;

        string[] values = input.Split(',');
        if (values.Length < 2) return;

        try
        {
            RAGInput.action = values[0];
            RAGInput.step = int.Parse(values[1]);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Parse error: " + e.Message);
        }
    }
}

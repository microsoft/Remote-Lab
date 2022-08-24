using System;
using UnityEngine;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System.Diagnostics;
using System.IO;
using System.Collections;

public class ObsManager : MonoBehaviour
{
    public string obsPath = "";
    public string url = "";
    public string password = "";

    public float waitForOBSDelay = 3;
    public int retryLimit = 5;
    private int currRetries;

    private OBSWebsocket obs;
    private Process obsProcess;

    public delegate void OBSEventHandler();
    public event OBSEventHandler ConnectedToOBS;
    public event OBSEventHandler DisconnectedFromOBS;
    public event OBSEventHandler StartedOBS;
    public event OBSEventHandler StoppedOBS;
    public event OBSEventHandler FailedOBSStart;

    private void Start()
    {
        obs = new OBSWebsocket();
        obs.Connected += OnConnect;
        obs.Disconnected += OnDisconnect;
        obs.RecordingStateChanged += OnRecordingStateChanged;
    }

    public void StartOBS()
    {
        if (Application.isEditor)
        {
            Process[] obsProcs = Process.GetProcessesByName("obs64");

            foreach (Process p in obsProcs)
            {
                if (obsProcess == null || obsProcess.HasExited || p.Id != obsProcess.Id)
                {
                    p.Kill();
                }
            }

            if (obsProcess == null || obsProcess.HasExited)
            {
                obsProcess = new Process();
                obsProcess.StartInfo.FileName = Path.Combine(obsPath, "obs64.exe");
                obsProcess.StartInfo.WorkingDirectory = obsPath;
                obsProcess.StartInfo.UseShellExecute = false;

                currRetries = retryLimit;

                try
                {
                    obsProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Error launching OBS recording");
                    UnityEngine.Debug.LogError(e.Message);
                    FailedOBSStart?.Invoke();
                }
            }

            // Try connection to OBS
            StartCoroutine(ConnectToOBS());
        }
    }

    private IEnumerator ConnectToOBS()
    {
        yield return new WaitForSeconds(waitForOBSDelay);

        string connectURL = "ws://" + url;
        print("Connecting to OBS...");

        try
        {
            obs.Connect(connectURL, password);
        }
        catch (Exception e)
        {
            if (currRetries > 0)
            {
                string formatMsg = String.Format("Failed to connect to OBS. Retrying {0} more times",
                    currRetries);
                UnityEngine.Debug.LogError(formatMsg);
                UnityEngine.Debug.LogError(e.Message);
                currRetries -= 1;

                StartCoroutine(ConnectToOBS());
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to connect to OBS. Aborting OBS start-up");
                UnityEngine.Debug.LogError("Aborting recording start-up");
                FailedOBSStart?.Invoke();
            }
        }
    }

    public void StartOBSRecording()
    {
        if (obs.IsConnected)
        {
            obs.StartRecording();
        }
    }

    public void StopOBSRecording()
    {
        if (obs.IsConnected)
        {
            obs.StopRecording();
        }
    }

    private void OnConnect(object sender, EventArgs e)
    {
        print("Connected to OBS...");
        ConnectedToOBS?.Invoke();
    }

    private void OnDisconnect(object sender, EventArgs e)
    {
        print("Disconnected from OBS");
        DisconnectedFromOBS?.Invoke();
    }

    private void OnRecordingStateChanged(OBSWebsocket sender, OutputState type)
    {
        if (type.Equals(OutputState.Started))
        {
            print("OBS Started Recording, Invoking StartedOBS");
            StartedOBS?.Invoke();
        }
        else if (type.Equals(OutputState.Stopped))
        {
            print("OBS Stopped Recording, Invoking StoppedOBS");
            StoppedOBS?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (obs.IsConnected)
        {
            obs.StopRecording();
            obs.Disconnect();
        }
    }

    private void OnDisable()
    {
        if (obs.IsConnected)
        {
            obs.StopRecording();
            obs.Disconnect();
        }
    }
}

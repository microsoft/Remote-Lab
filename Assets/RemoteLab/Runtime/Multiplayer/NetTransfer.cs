using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Photon.Pun;
using Google.Protobuf;

namespace RemoteLab
{
    public class NetTransfer : MonoBehaviour
    {
        [SerializeField] private NetworkManager launcher;
        private PhotonView photonView;
        private Dictionary<string, StreamWriter> writerDict;
        private Dictionary<string, StreamWriter> uiWriterDict;

        public bool triggerTransfer;
        public int batchSize = 100;

        private bool transformTransferring;
        private bool uiTransferring;

        private void Start()
        {
            photonView = GetComponent<PhotonView>();
            writerDict = new Dictionary<string, StreamWriter>();
            uiWriterDict = new Dictionary<string, StreamWriter>();
        }

        private void Update()
        {
            if (triggerTransfer && !(uiTransferring || transformTransferring))
            {
                if (photonView != null)
                {
                    photonView.RPC(nameof(RequestData), RpcTarget.Others);
                }

                triggerTransfer = false;
                uiTransferring = true;
                transformTransferring = true;
            }
            else if (triggerTransfer)
            {
                triggerTransfer = false;
                print("Currently transferring data...");
            }
        }

        public void SendStartStopToClients()
        {
            photonView.RPC(nameof(BroadcastStartStop), RpcTarget.Others);
        }

        [PunRPC]
        public void BroadcastStartStop()
        {
            if (ReplayManager.Instance == null)
                return;

            ReplayManager.Instance.HandleRecording();
        }

        [PunRPC]
        public void RequestData()
        {
            if (launcher != null)
            {
                ReplayManager.Instance.CloseAllWriters();
                SendTransformData();
                SendUiData();
                Debug.LogError("Sending data to researcher...");
            }
        }

        public void SendUiData()
        {
            string uiFileName = ReplayManager.Instance.GetUIDataPath();
            string origin = launcher.GetPlayerName();

            using (StreamReader reader = new StreamReader(uiFileName))
            {
                // Read in first header line
                string line = reader.ReadLine();
                UiRecord r = new UiRecord();
                byte[] streamBytes;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    if (values.Length <= 0)
                        break;

                    if (values[1].Equals("slider"))
                    {
                        values[1] = "Slider";
                    }
                    else if (values[1].Equals("toggle"))
                    {
                        values[1] = "Toggle";
                    }
                    else
                    {
                        values[1] = "Button";
                    }

                    UiStep s = new UiStep
                    {
                        FrameCount = int.Parse(values[0]),
                        Type = (UiStep.Types.TypeEnum)System.Enum.Parse(typeof(UiStep.Types.TypeEnum), values[1]),
                        NewValue = values[2],
                        Hierarchy = values[3],
                        ID = values[4],
                    };

                    r.UiRecord_.Add(s);

                    if (r.UiRecord_.Count >= batchSize)
                    {
                        // Send record batch over network
                        streamBytes = Compress(r.ToByteArray());
                        photonView.RPC(nameof(ReceiveTransformDataRPC), RpcTarget.Others, origin, streamBytes);
                        Debug.LogError("Sent UI batch...");
                        r = new UiRecord();
                    }
                }

                streamBytes = Compress(r.ToByteArray());
                photonView.RPC(nameof(ReceiveUiDataRPC), RpcTarget.Others, origin, streamBytes);
                photonView.RPC(nameof(CloseUiWriter), RpcTarget.Others, origin);
                Debug.LogError("Finishing last UI batch...");
            }
        }

        [PunRPC]
        public void ReceiveUiDataRPC(string origin, byte[] streamBytes)
        {
            if (launcher.IsParticipant())
                return;

            string writerFileName = ReplayManager.Instance.GetSaveFolderPath() + "/" +
                origin + "_" + ReplayManager.Instance.UIEventDataFilePrefix + "_data.csv";

            if (!uiWriterDict.ContainsKey(origin))
            {
                uiWriterDict.Add(origin, new StreamWriter(writerFileName));
                string[] uiDataHeaders = { "FrameCount", "UI Type", "New Value", "Hierarchy",
                                              "ID" };
                uiWriterDict[origin].WriteLine(string.Join(",", uiDataHeaders));
            }

            UiRecord r = UiRecord.Parser.ParseFrom(Decompress(streamBytes));

            foreach (UiStep s in r.UiRecord_)
            {
                string type;

                if (s.Type.Equals(UiStep.Types.TypeEnum.Button))
                {
                    type = "button";
                }
                else if (s.Type.Equals(UiStep.Types.TypeEnum.Slider))
                {
                    type = "slider";
                }
                else
                {
                    type = "toggle";
                }

                string[] values = { s.FrameCount.ToString(), type, s.NewValue, s.Hierarchy, s.ID };

                string entry = string.Join(",", values);
                uiWriterDict[origin].WriteLine(entry);
            }

            Debug.LogError("Received UI batch...");
        }

        [PunRPC]
        public void CloseUiWriter(string origin)
        {
            if (!uiWriterDict.ContainsKey(origin))
                return;

            uiWriterDict[origin].Close();
            uiWriterDict.Remove(origin);
            Debug.LogError("UI transfer finished, closing UI writer...");
            uiTransferring = false;
        }

        public void SendTransformData()
        {
            string transformFileName = ReplayManager.Instance.GetTransformDataPath();
            string origin = launcher.GetPlayerName();

            using (StreamReader reader = new StreamReader(transformFileName))
            {
                // Read in first header line
                string line = reader.ReadLine();
                Record r = new Record();
                byte[] streamBytes;

                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');

                    if (values.Length <= 0)
                        break;

                    if (values[2].Equals("IFrame_Active"))
                        values[2] = "IframeActive";
                    else if (values[2].Equals("IFrame_Inactive"))
                        values[2] = "IframeInactive";

                    Step s = new Step
                    {
                        FrameCount = int.Parse(values[0]),
                        GameObject = values[1],
                        Status = (Step.Types.StatusEnum)System.Enum.Parse(typeof(Step.Types.StatusEnum), values[2]),
                        PositionX = float.Parse(values[3]),
                        PositionY = float.Parse(values[4]),
                        PositionZ = float.Parse(values[5]),
                        RotationX = float.Parse(values[6]),
                        RotationY = float.Parse(values[7]),
                        RotationZ = float.Parse(values[8]),
                        ScaleX = float.Parse(values[9]),
                        ScaleY = float.Parse(values[10]),
                        ScaleZ = float.Parse(values[11]),
                        Resource = values[12],
                        ID = values[13],
                        Hierarchy = values[14]
                    };

                    r.Record_.Add(s);

                    if (r.Record_.Count >= batchSize)
                    {
                        // Send record batch over network
                        streamBytes = Compress(r.ToByteArray());
                        photonView.RPC(nameof(ReceiveTransformDataRPC), RpcTarget.Others, origin, streamBytes);
                        Debug.LogError("Sent batch...");
                        r = new Record();
                    }
                }

                streamBytes = Compress(r.ToByteArray());
                photonView.RPC(nameof(ReceiveTransformDataRPC), RpcTarget.Others, origin, streamBytes);
                photonView.RPC(nameof(CloseWriter), RpcTarget.Others, origin);
                Debug.LogError("Finishing last batch...");
            }
        }

        [PunRPC]
        public void ReceiveTransformDataRPC(string origin, byte[] streamBytes)
        {
            if (launcher.IsParticipant())
                return;

            string writerFileName = ReplayManager.Instance.GetSaveFolderPath() + "/" +
                origin + "_" + ReplayManager.Instance.transformDataFilePrefix + "_data.csv";

            if (!writerDict.ContainsKey(origin))
            {
                writerDict.Add(origin, new StreamWriter(writerFileName));
                string[] transformDataHeaders = { "FrameCount", "GameObject", "Status", "Pos X", "Pos Y", "Pos Z", "Rot X", "Rot Y",
                                          "Rot Z", "Scal X", "Scal Y", "Scal Z", "Resource Path", "ID", "Hierarchy" };
                writerDict[origin].WriteLine(string.Join(",", transformDataHeaders));
            }

            Record r = Record.Parser.ParseFrom(Decompress(streamBytes));

            foreach (Step s in r.Record_)
            {
                string[] values = { s.FrameCount.ToString(), s.GameObject, s.Status.ToString(),
                            s.PositionX.ToString(), s.PositionY.ToString(), s.PositionZ.ToString(),
                            s.RotationX.ToString(), s.RotationY.ToString(), s.RotationZ.ToString(),
                            s.ScaleX.ToString(), s.ScaleY.ToString(), s.ScaleZ.ToString(),
                            s.Resource, s.ID, s.Hierarchy };

                string entry = string.Join(",", values);
                writerDict[origin].WriteLine(entry);
            }

            Debug.LogError("Received batch...");
        }

        [PunRPC]
        public void CloseWriter(string origin)
        {
            if (!writerDict.ContainsKey(origin))
                return;

            writerDict[origin].Close();
            writerDict.Remove(origin);
            Debug.LogError("Transform transfer finished, closing writer...");
            transformTransferring = false;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, StreamWriter> kvp in writerDict)
            {
                kvp.Value.Close();
            }
        }

        public byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dStream = new DeflateStream(output, CompressionMode.Compress))
            {
                dStream.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();

            using (DeflateStream dStream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dStream.CopyTo(output);
            }

            return output.ToArray();
        }
    }
}
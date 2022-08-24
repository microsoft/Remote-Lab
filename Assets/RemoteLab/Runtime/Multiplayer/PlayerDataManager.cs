using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerDataManager : MonoBehaviour
{
    public PlayerNameTag playerSystem;
    [SerializeField] private GameObject camPrefab;
    private PhotonView photonView;
    private Camera playerCam;
    
    // Called when head is instantiated
    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (!NetworkManager.Instance.IsParticipant() && !photonView.AmOwner)
        {
            GameObject camObj = Instantiate(camPrefab, gameObject.transform);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            playerCam = camObj.GetComponent<Camera>();
            playerCam.targetDisplay = NetworkManager.Instance.AllocateDisplayId();
        }

        if (!photonView.IsMine)
        {
            RequestRefs();
        }
    }

    void OnDestroy()
    {
        if (playerCam != null)
            NetworkManager.Instance.DeallocateDisplayId(playerCam.targetDisplay);
    }

    public void RequestRefs()
    {
        photonView.RPC(nameof(OnReceiveRefRequest), RpcTarget.Others);
    }

    [PunRPC]
    void OnReceiveRefRequest()
    {
        if (photonView.IsMine)
        {
            photonView.RPC(nameof(OnReceiveViewRef), RpcTarget.Others, 
                playerSystem.GetComponent<PhotonView>().ViewID);
        }
    }

    [PunRPC]
    void OnReceiveViewRef(int viewRef)
    {
        playerSystem = PhotonView.Find(viewRef).GetComponent<PlayerNameTag>();
        playerSystem.transform.SetParent(transform);
    }
}

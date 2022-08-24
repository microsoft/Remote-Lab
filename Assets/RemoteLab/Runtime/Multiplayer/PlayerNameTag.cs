using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerNameTag : MonoBehaviour
{
    public string nickname;
    public bool updateName;

    private PhotonView photonView;

    public delegate void OnNicknameCallback(string s);

    public OnNicknameCallback onNicknameChanged;

    private void Start()
    {
        nickname = NetworkManager.Instance.GetPlayerName();
        photonView = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (updateName)
        {
            UpdateName();
            updateName = false;
        }
    }

    private void UpdateName()
    {
        if (photonView != null)
        {
            if (!photonView.IsMine)
            {
                photonView.RequestOwnership();
            }

            photonView.RPC(nameof(UpdateNickname), RpcTarget.Others, nickname);
        }
    }

    [PunRPC]
    private void UpdateNickname(string name)
    {
        Debug.Log("Updating nickname RPC...");
        nickname = name;
        onNicknameChanged?.Invoke(nickname);
    }
}
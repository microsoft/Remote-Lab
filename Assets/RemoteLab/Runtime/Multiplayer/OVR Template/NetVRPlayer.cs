using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetVRPlayer : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform leftHandRoot;
    [SerializeField] private Transform rightHandRoot;
    [SerializeField] private Transform headRoot;

    [SerializeField] private Vector3 leftHandOffset;
    [SerializeField] private Vector3 rightHandOffset;
    [SerializeField] private Vector3 headOffset;

    [SerializeField] private string leftHandAvatarName;
    [SerializeField] private string rightHandAvatarName;
    [SerializeField] private string headAvatarName;
    [SerializeField] private string nameTagName;

    [SerializeField] NetHand leftNetHand;
    [SerializeField] NetHand rightNetHand;

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        GameObject lHand = PhotonNetwork.Instantiate(leftHandAvatarName, Vector3.zero, Quaternion.identity);
        lHand.transform.SetParent(leftHandRoot);
        lHand.transform.localPosition = leftHandOffset;
        lHand.transform.localRotation = Quaternion.identity;

        GameObject rHand = PhotonNetwork.Instantiate(rightHandAvatarName, Vector3.zero, Quaternion.identity);
        rHand.transform.SetParent(rightHandRoot);
        rHand.transform.localPosition = rightHandOffset;
        rHand.transform.localRotation = Quaternion.identity;

        GameObject head = PhotonNetwork.Instantiate(headAvatarName, Vector3.zero, Quaternion.identity);
        head.transform.SetParent(headRoot);
        head.transform.localPosition = headOffset;
        head.transform.localRotation = Quaternion.identity;

        GameObject nameTag = PhotonNetwork.Instantiate(nameTagName, Vector3.zero, Quaternion.identity);
        nameTag.transform.SetParent(head.transform);

        leftNetHand.m_animator = lHand.GetComponent<Animator>();
        rightNetHand.m_animator = rHand.GetComponent<Animator>();

        PlayerNameTag system = nameTag.GetComponent<PlayerNameTag>();
        system.onNicknameChanged += OnNicknameChanged;

        PlayerDataManager info = head.GetComponent<PlayerDataManager>();
        info.playerSystem = system;
    }

    private void OnNicknameChanged(string s)
    {
        PhotonNetwork.LocalPlayer.NickName = s;
    }

    public string GetNameTag()
    {
        return nameTagName;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[System.Serializable]
public class QuestionnaireInformation
{
    public string name;
    public Vector3 location;
    public Vector3 rotation;
}

public class SpawnQuestionnaires : MonoBehaviour
{
    public List<QuestionnaireInformation> questionnairesToSpawn;

    /*
    void Update()
    {
        if (PhotonNetwork.InRoom && Input.GetKey(KeyCode.Alpha1) && !GameObject.Find(survey1Name + "(Clone)"))
        {
            PhotonNetwork.Instantiate(survey1Name, survey1Location, Quaternion.Euler(survey1Rotation));
        }
        if (PhotonNetwork.InRoom && Input.GetKey(KeyCode.Alpha2) && !GameObject.Find(survey2Name + "(Clone)"))
        {
            PhotonNetwork.Instantiate(survey2Name, survey2Location, Quaternion.Euler(survey2Rotation));
        }
        if (PhotonNetwork.InRoom && Input.GetKey(KeyCode.Alpha3) && !GameObject.Find(survey3Name + "(Clone)"))
        {
            PhotonNetwork.Instantiate(survey3Name, survey3Location, Quaternion.Euler(survey3Rotation));
        }
    }
    */
}

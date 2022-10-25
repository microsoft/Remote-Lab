using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct QuestionnaireInformation
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;

    public QuestionnaireInformation (string name, Vector3 position, Vector3 rotation)
    {
        this.name = name;
        this.position = position;
        this.rotation = rotation;
    }
}

public class SpawnQuestionnaires : MonoBehaviour
{
    public static SpawnQuestionnaires Instance;
    public List<QuestionnaireInformation> questionnairesToSpawn;

    private void Awake()
    {
        Instance = this;
    }
}

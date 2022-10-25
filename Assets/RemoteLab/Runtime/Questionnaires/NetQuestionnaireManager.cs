using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace RemoteLab
{
    public class NetQuestionnaireManager : MonoBehaviourPunCallbacks
    {
        [Header("Questionnaire Content")]
        public QuestionnaireContent questionnaireContent;

        [Header("Default Buttons")]
        [SerializeField] GameObject prevButton;
        [SerializeField] GameObject nextButton;
        [SerializeField] GameObject skipButton;

        [Header("Single Answer Children")]
        [SerializeField] GameObject singleAnswerTemplate;
        [SerializeField] GameObject singleAnswerChoicePrefab;

        [Header("Multiple Answers Children")]
        [SerializeField] GameObject multipleAnswersTemplate;
        [SerializeField] GameObject multipleAnswersChoicePrefab;

        [Header("Slider Children")]
        [SerializeField] GameObject sliderTemplate;

        [HideInInspector] public List<GameObject> questionnaireQuestionObjects;
        private int currQuestionIdx;

        // Data Logging
        private string questionnaireTitle;
        private string questionnaireDataSavePath;
        private StreamWriter questionnaireDataWriter;
        private List<bool> participantDidSkip;

        // Networkings
        private int[] questionViewIds;
        private List<QuestionnaireQuestion> questionnaireQuestions;
        private Dictionary<int, ChoiceRPCObject> questionChoicesMap;

        private bool initialized;

        [System.Serializable]
        class ChoiceRPCObject
        {
            public List<int> choiceViewIds;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

            InitializeQuestionnaire();
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            InitializeQuestionnaire();
        }

        private void InitializeQuestionnaire()
        {
            questionnaireQuestionObjects = new List<GameObject>();
            questionnaireQuestions = new List<QuestionnaireQuestion>();
            questionChoicesMap = new Dictionary<int, ChoiceRPCObject>();
            currQuestionIdx = 0;

            if (photonView.IsMine)
            {
                InitializeRecording();
                SetUpQuestionnaire();
                SetUpButtons();
            }
            else
            {
                // RPC to get questionnaire data
                Debug.LogError("RPC to get questionnaire data from owner");
                photonView.RPC(nameof(RequestQuestionnaireInfo), RpcTarget.Others);
            }
        }

        [PunRPC]
        private void RequestQuestionnaireInfo()
        {
            if (photonView.IsMine)
            {
                Debug.LogError("RPC to acknowledge request receipt");
                for (int i = 0; i < questionnaireQuestionObjects.Count; i++)
                {
                    string jsonTxt = JsonUtility.ToJson(questionnaireQuestions[i]);
                    string choiceJsonTxt = JsonUtility.ToJson(questionChoicesMap[questionViewIds[i]]);
                    photonView.RPC(nameof(UpdateQuestionInfo), RpcTarget.Others, jsonTxt, choiceJsonTxt, questionViewIds[i], i, i == 0);
                }

                // RPC to set up event handlers
                photonView.RPC(nameof(InitEventHandlers), RpcTarget.Others);
            }
        }

        [PunRPC]
        private void UpdateQuestionInfo(string questionJson, string choiceRPCJson, int viewId, int idx, bool activeStatus)
        {
            if (photonView.IsMine)
                return;

            Debug.LogError("RPC to send question info");
            print(questionJson);
            QuestionnaireQuestion questionnaireQuestion = JsonUtility.FromJson<QuestionnaireQuestion>(questionJson);
            ChoiceRPCObject cRPC = JsonUtility.FromJson<ChoiceRPCObject>(choiceRPCJson);
            GameObject questionnaireQuestionObj = PhotonView.Find(viewId).gameObject;

            switch (questionnaireQuestion.choicesType)
            {
                case ChoicesType.Single:
                    InitSingleChoiceQuestion(questionnaireQuestionObj, questionnaireQuestion, cRPC, idx);
                    break;
                case ChoicesType.Multiple:
                    InitMultiChoiceQuestion(questionnaireQuestionObj, questionnaireQuestion, cRPC, idx);
                    break;
                case ChoicesType.Slider:
                    InitSliderQuestion(questionnaireQuestionObj, questionnaireQuestion, cRPC, idx);
                    break;
                default:
                    break;
            }

            questionnaireQuestionObjects.Add(questionnaireQuestionObj);
            questionnaireQuestionObj.SetActive(idx == 0);
            questionnaireQuestions.Add(questionnaireQuestion);
        }

        [PunRPC]
        private void InitEventHandlers()
        {
            Debug.LogError("RPC to set event handlers");
            InitializeRecording();
            SetUpButtons();
        }

        private void InitializeRecording()
        {
            string platformPath = (Application.isEditor) ? Application.dataPath : Application.persistentDataPath;
            questionnaireTitle = ((gameObject.name).Replace(" ", "")).ToLower();
            questionnaireDataSavePath = platformPath + "/" + questionnaireTitle + "_questionnaire_data.csv";
            questionnaireDataWriter = new StreamWriter(questionnaireDataSavePath);
            participantDidSkip = new List<bool>();

            string[] transformDataHeaders = { "FrameCount", "Location", "Question", "User Value" };
            questionnaireDataWriter.WriteLine(string.Join(",", transformDataHeaders));
        }

        private void SetUpQuestionnaire()
        {
            currQuestionIdx = 0;
            questionViewIds = new int[questionnaireContent.questionnaireQuestions.Count];

            for (int questionIdx = 0; questionIdx < questionnaireContent.questionnaireQuestions.Count; questionIdx++)
            {
                QuestionnaireQuestion questionnaireQuestion = questionnaireContent.questionnaireQuestions[questionIdx];
                GameObject questionnaireQuestionObj = null;

                if (questionnaireQuestion.choicesType == ChoicesType.Single)
                {
                    // Instantiate Single Answer Template object
                    questionnaireQuestionObj = PhotonNetwork.Instantiate(singleAnswerTemplate.name, new Vector3(0, 0, 0), Quaternion.identity);
                    InitSingleChoiceQuestion(questionnaireQuestionObj, questionnaireQuestion, questionIdx);
                }
                else if (questionnaireQuestion.choicesType == ChoicesType.Multiple)
                {
                    // Instantiate Multiple Answers Template object
                    questionnaireQuestionObj = PhotonNetwork.Instantiate(multipleAnswersTemplate.name, new Vector3(0, 0, 0), Quaternion.identity);
                    InitMultiChoiceQuestion(questionnaireQuestionObj, questionnaireQuestion, questionIdx);
                }
                else if (questionnaireQuestion.choicesType == ChoicesType.Slider)
                {
                    // Instantiate Slider Template object
                    questionnaireQuestionObj = PhotonNetwork.Instantiate(sliderTemplate.name, new Vector3(0, 0, 0), Quaternion.identity);
                    InitSliderQuestion(questionnaireQuestionObj, questionnaireQuestion, questionIdx);
                }

                // Check for incomplete questionnaire template
                if (questionnaireQuestionObj == null)
                {
                    Debug.LogError("Incomplete questionnaire question " + questionIdx);
                    return;
                }

                // Deactivate all questions besides the first question
                if (questionIdx == 0)
                {
                    questionnaireQuestionObj.SetActive(true);
                }
                else
                {
                    questionnaireQuestionObj.SetActive(false);
                }

                if (questionnaireQuestionObj != null)
                {
                    questionnaireQuestionObjects.Add(questionnaireQuestionObj);
                    questionViewIds[questionIdx] = questionnaireQuestionObj.GetComponent<PhotonView>().ViewID;
                    questionnaireQuestions.Add(questionnaireQuestion);
                }
            }
        }

        private void InitSliderQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Assign slider parameters.
            Transform sliderChoiceObj = questionnaireQuestionObj.transform.GetChild(1);
            sliderChoiceObj.GetComponent<Slider>().minValue = questionnaireQuestion.minScore;
            sliderChoiceObj.GetComponent<Slider>().maxValue = questionnaireQuestion.maxScore;
            sliderChoiceObj.GetComponent<Slider>().value = questionnaireQuestion.minScore;
            sliderChoiceObj.GetComponent<Slider>().wholeNumbers = questionnaireQuestion.wholeNumber;

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;

            int viewID = questionnaireQuestionObj.GetComponent<PhotonView>().ViewID;
            questionChoicesMap.Add(viewID, null);
        }

        private void InitSliderQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, ChoiceRPCObject cRPC, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Assign slider parameters.
            Transform sliderChoiceObj = questionnaireQuestionObj.transform.GetChild(1);
            sliderChoiceObj.GetComponent<Slider>().minValue = questionnaireQuestion.minScore;
            sliderChoiceObj.GetComponent<Slider>().maxValue = questionnaireQuestion.maxScore;
            sliderChoiceObj.GetComponent<Slider>().value = questionnaireQuestion.minScore;
            sliderChoiceObj.GetComponent<Slider>().wholeNumbers = questionnaireQuestion.wholeNumber;

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;
        }

        private void InitMultiChoiceQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Add choices to map for RPC
            int viewID = questionnaireQuestionObj.GetComponent<PhotonView>().ViewID;
            List<int> choiceViewIds = new List<int>();

            // Instantiate choice prefabs & Set their parent (1) & Assign an appropriate label to each of them
            Transform choicesParent = questionnaireQuestionObj.transform.GetChild(1);
            for (int choiceLabelIdx = 0; choiceLabelIdx < questionnaireQuestion.choiceLabels.Count; choiceLabelIdx++)
            {
                RectTransform choicesParentRectTransform = choicesParent.GetComponent<RectTransform>();
                GameObject instantiatedChoicePrefab = PhotonNetwork.Instantiate(multipleAnswersChoicePrefab.name,
                                                                  new Vector3(choicesParentRectTransform.offsetMin.x + (((choicesParentRectTransform.offsetMax.x - choicesParentRectTransform.offsetMin.x) / (questionnaireQuestion.choiceLabels.Count - 1)) * choiceLabelIdx), 0, 0),
                                                                  Quaternion.identity);
                instantiatedChoicePrefab.transform.SetParent(choicesParentRectTransform, false);
                instantiatedChoicePrefab.name = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponentInChildren<TextMeshProUGUI>().text = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                choiceViewIds.Add(instantiatedChoicePrefab.GetComponent<PhotonView>().ViewID);
            }

            // Add choices to choices id dict
            ChoiceRPCObject cRPC = new ChoiceRPCObject();
            cRPC.choiceViewIds = choiceViewIds;
            questionChoicesMap.Add(viewID, cRPC);

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;
        }

        private void InitMultiChoiceQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, ChoiceRPCObject cRPC, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Instantiate choice prefabs & Set their parent (1) & Assign an appropriate label to each of them
            Transform choicesParent = questionnaireQuestionObj.transform.GetChild(1);
            for (int choiceLabelIdx = 0; choiceLabelIdx < questionnaireQuestion.choiceLabels.Count; choiceLabelIdx++)
            {
                RectTransform choicesParentRectTransform = choicesParent.GetComponent<RectTransform>();
                GameObject instantiatedChoicePrefab = PhotonView.Find(cRPC.choiceViewIds[choiceLabelIdx]).gameObject;
                instantiatedChoicePrefab.transform.SetParent(choicesParentRectTransform, false);
                instantiatedChoicePrefab.name = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponentInChildren<TextMeshProUGUI>().text = questionnaireQuestion.choiceLabels[choiceLabelIdx];
            }

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;
        }

        private void InitSingleChoiceQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Add choices to map for RPC
            int viewID = questionnaireQuestionObj.GetComponent<PhotonView>().ViewID;
            List<int> choiceViewIds = new List<int>();

            // Instantiate choice prefabs & Set their parent (1) & Assign an appropriate label to each of them
            Transform choicesParent = questionnaireQuestionObj.transform.GetChild(1);
            ToggleGroup choicesParentToggleGroup = choicesParent.GetComponent<ToggleGroup>();
            for (int choiceLabelIdx = 0; choiceLabelIdx < questionnaireQuestion.choiceLabels.Count; choiceLabelIdx++)
            {
                RectTransform choicesParentRectTransform = choicesParent.GetComponent<RectTransform>();
                GameObject instantiatedChoicePrefab = PhotonNetwork.Instantiate(singleAnswerChoicePrefab.name,
                                                                  new Vector3(choicesParentRectTransform.offsetMin.x + (((choicesParentRectTransform.offsetMax.x - choicesParentRectTransform.offsetMin.x) / (questionnaireQuestion.choiceLabels.Count - 1)) * choiceLabelIdx), 0, 0),
                                                                  Quaternion.identity);
                instantiatedChoicePrefab.transform.SetParent(choicesParentRectTransform, false);
                instantiatedChoicePrefab.name = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponentInChildren<TextMeshProUGUI>().text = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponent<Toggle>().group = choicesParentToggleGroup;
                choiceViewIds.Add(instantiatedChoicePrefab.GetComponent<PhotonView>().ViewID);
            }

            // Add choices to choices id dict
            ChoiceRPCObject cRPC = new ChoiceRPCObject();
            cRPC.choiceViewIds = choiceViewIds;
            questionChoicesMap.Add(viewID, cRPC);

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;

            // Turn off all toggles at start
            choicesParentToggleGroup.allowSwitchOff = true;
            choicesParentToggleGroup.SetAllTogglesOff();
        }

        private void InitSingleChoiceQuestion(GameObject questionnaireQuestionObj,
            QuestionnaireQuestion questionnaireQuestion, ChoiceRPCObject cRPC, int questionIdx)
        {
            questionnaireQuestionObj.transform.SetParent(transform, false);
            questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

            // Assign question string to question text (0)
            questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

            // Instantiate choice prefabs & Set their parent (1) & Assign an appropriate label to each of them
            Transform choicesParent = questionnaireQuestionObj.transform.GetChild(1);
            ToggleGroup choicesParentToggleGroup = choicesParent.GetComponent<ToggleGroup>();
            for (int choiceLabelIdx = 0; choiceLabelIdx < questionnaireQuestion.choiceLabels.Count; choiceLabelIdx++)
            {
                RectTransform choicesParentRectTransform = choicesParent.GetComponent<RectTransform>();
                GameObject instantiatedChoicePrefab = PhotonView.Find(cRPC.choiceViewIds[choiceLabelIdx]).gameObject;

                instantiatedChoicePrefab.transform.SetParent(choicesParentRectTransform, false);
                instantiatedChoicePrefab.name = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponentInChildren<TextMeshProUGUI>().text = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                instantiatedChoicePrefab.GetComponent<Toggle>().group = choicesParentToggleGroup;
            }

            // Assign Min Score Label (2) & Max Score Label (3)
            questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
            questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;

            // Turn off all toggles at start
            choicesParentToggleGroup.allowSwitchOff = true;
            choicesParentToggleGroup.SetAllTogglesOff();
        }

        private void SetUpButtons()
        {
            // Set up an OnClick event for the prev button based on 
            TextMeshProUGUI prevButtonText = prevButton.GetComponentInChildren<TextMeshProUGUI>();
            Button prevButtonComponent = prevButton.GetComponent<Button>();
            NetworkedUI prevButtonNetUI = prevButton.GetComponent<NetworkedUI>();

            if (currQuestionIdx == 0)
            {
                prevButtonText.text = "";
                prevButtonComponent.onClick.RemoveAllListeners();
            }
            else
            {
                prevButtonText.text = "Previous Question";
                prevButtonComponent.onClick.RemoveAllListeners();
                prevButtonComponent.onClick.AddListener(LoadPrevQuestion);
                prevButtonComponent.onClick.AddListener(prevButtonNetUI.BroadcastClick);
            }

            // Set up an OnClick event for the next button based on number of questions left
            TextMeshProUGUI nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            Button nextButtonComponent = nextButton.GetComponent<Button>();
            NetworkedUI nextButtonNetUI = nextButton.GetComponent<NetworkedUI>();

            if (currQuestionIdx >= questionnaireQuestionObjects.Count - 1)
            {
                nextButtonText.text = "Submit";
                nextButtonComponent.onClick.RemoveAllListeners();
                nextButtonComponent.onClick.AddListener(SubmitQuestionnaire);
                nextButtonComponent.onClick.AddListener(nextButtonNetUI.BroadcastClick);
            }
            else
            {
                nextButtonText.text = "Next Question";
                nextButtonComponent.onClick.RemoveAllListeners();
                nextButtonComponent.onClick.AddListener(LoadNextQuestion);
                nextButtonComponent.onClick.AddListener(nextButtonNetUI.BroadcastClick);
            }

            // Set up an OnClick event for the skip button
            Button skipButtonComponent = skipButton.GetComponent<Button>();
            NetworkedUI skipButtonNetUI = skipButton.GetComponent<NetworkedUI>();

            if (questionnaireContent.questionnaireQuestions[currQuestionIdx].canSkipQuestion)
            {
                if (currQuestionIdx >= questionnaireQuestionObjects.Count - 1)
                {
                    skipButtonComponent.onClick.RemoveAllListeners();
                    skipButtonComponent.onClick.AddListener(SkipAndSubmitQuestionnaire);
                    skipButtonComponent.onClick.AddListener(skipButtonNetUI.BroadcastClick);
                    skipButton.SetActive(true);
                }
                else
                {
                    skipButtonComponent.onClick.RemoveAllListeners();
                    skipButtonComponent.onClick.AddListener(SkipQuestion);
                    skipButtonComponent.onClick.AddListener(skipButtonNetUI.BroadcastClick);
                    skipButton.SetActive(true);
                }
            }
            else
            {
                skipButtonComponent.onClick.RemoveAllListeners();
                skipButton.SetActive(false);
            }

            if (ReplayManager.Instance != null)
            {
                prevButtonComponent.onClick.AddListener(delegate
                {
                    ReplayManager.Instance.WriteUIEventDataEntry("button", "click", prevButtonComponent.gameObject,
                        prevButtonComponent.GetComponent<InteractableUI>().guidString);
                });

                nextButtonComponent.onClick.AddListener(delegate
                {
                    ReplayManager.Instance.WriteUIEventDataEntry("button", "click", nextButtonComponent.gameObject,
                        nextButtonComponent.GetComponent<InteractableUI>().guidString);
                });

                skipButtonComponent.onClick.AddListener(delegate
                {
                    ReplayManager.Instance.WriteUIEventDataEntry("button", "click", skipButtonComponent.gameObject,
                        skipButtonComponent.GetComponent<InteractableUI>().guidString);
                });
            }

            initialized = true;
        }

        private void LoadNextQuestion()
        {
            if (questionnaireQuestionObjects[currQuestionIdx].transform.GetChild(1).GetComponentInChildren<Toggle>() != null)
            {
                Toggle toggled = null;
                foreach (Toggle t in questionnaireQuestionObjects[currQuestionIdx].transform.GetChild(1).GetComponentsInChildren<Toggle>())
                {
                    if (t.isOn)
                    {
                        toggled = t;
                        break;
                    }
                }

                if (toggled == null)
                    return;
            }

            participantDidSkip.Add(false);
            questionnaireQuestionObjects[currQuestionIdx].SetActive(false);
            questionnaireQuestionObjects[currQuestionIdx + 1].SetActive(true);
            currQuestionIdx++;
            SetUpButtons();
        }

        private void LoadPrevQuestion()
        {
            if (currQuestionIdx == 0)
                return;

            participantDidSkip.RemoveAt(participantDidSkip.Count - 1);
            questionnaireQuestionObjects[currQuestionIdx].SetActive(false);
            questionnaireQuestionObjects[currQuestionIdx - 1].SetActive(true);
            currQuestionIdx--;
            SetUpButtons();
        }

        private void SkipQuestion()
        {
            participantDidSkip.Add(true);
            questionnaireQuestionObjects[currQuestionIdx].SetActive(false);
            questionnaireQuestionObjects[currQuestionIdx + 1].SetActive(true);
            currQuestionIdx++;
            SetUpButtons();
        }

        private void SubmitQuestionnaire()
        {
            if (questionnaireQuestionObjects[currQuestionIdx].transform.GetChild(1).GetComponentInChildren<Toggle>() != null)
            {
                Toggle toggled = null;
                foreach (Toggle t in questionnaireQuestionObjects[currQuestionIdx].transform.GetChild(1).GetComponentsInChildren<Toggle>())
                {
                    if (t.isOn)
                    {
                        toggled = t;
                        break;
                    }
                }

                if (toggled == null)
                    return;
            }

            participantDidSkip.Add(false);
            StartCoroutine(RecordAndDestroy());
        }

        private void SkipAndSubmitQuestionnaire()
        {
            participantDidSkip.Add(true);
            StartCoroutine(RecordAndDestroy());
        }

        IEnumerator RecordAndDestroy()
        {
            for (int i = 0; i < questionnaireQuestionObjects.Count; i++)
            {
                if (questionnaireQuestionObjects[i].transform.GetChild(1).GetComponentInChildren<Toggle>() != null)
                {
                    if (participantDidSkip[i])
                    {
                        WriteQuestionnaireDataEntry(gameObject.name + "_" + questionnaireQuestionObjects[i].name,
                                             questionnaireQuestionObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text,
                                             "skipped");
                    }
                    else
                    {
                        List<Toggle> toggled = new List<Toggle>();
                        foreach (Toggle t in questionnaireQuestionObjects[i].transform.GetChild(1).GetComponentsInChildren<Toggle>())
                        {
                            if (t.isOn)
                            {
                                toggled.Add(t);
                            }
                        }

                        string joinedString = "";
                        for (int j = 0; j < toggled.Count; j++)
                        {
                            if (j < toggled.Count - 1)
                            {
                                joinedString += toggled[j].name;
                                joinedString += ",";
                            }
                            else
                            {
                                joinedString += toggled[j].name;
                            }
                        }

                        WriteQuestionnaireDataEntry(gameObject.name + "_" + questionnaireQuestionObjects[i].name,
                                             questionnaireQuestionObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text,
                                             joinedString);
                    }
                }
                else
                {
                    if (participantDidSkip[i])
                    {
                        WriteQuestionnaireDataEntry(gameObject.name + "_" + questionnaireQuestionObjects[i].name,
                                             questionnaireQuestionObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text,
                                             "skipped");
                    }
                    else
                    {
                        WriteQuestionnaireDataEntry(gameObject.name + "_" + questionnaireQuestionObjects[i].name,
                                             questionnaireQuestionObjects[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text,
                                             questionnaireQuestionObjects[i].transform.GetChild(1).GetComponent<Slider>().value.ToString());
                    }
                }

                yield return new WaitForSeconds(0.05f);
            }

            PhotonNetwork.Destroy(gameObject);
        }

        private void WriteQuestionnaireDataEntry(string location, string question, string user_val)
        {
            int frameCount = Time.frameCount;

            string[] values = { frameCount.ToString(), location, question, user_val };
            string entry = string.Join(",", values);

            questionnaireDataWriter.WriteLine(entry);
        }

        private void OnDestroy()
        {
            questionnaireDataWriter?.Close();
        }
    }
}
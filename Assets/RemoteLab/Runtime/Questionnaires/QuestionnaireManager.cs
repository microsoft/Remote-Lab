using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Text.RegularExpressions;

namespace RemoteLab
{
    public class QuestionnaireManager : MonoBehaviour
    {
        [Header("Questionnaire Content")]
        [SerializeField] QuestionnaireContent questionnaireContent;

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

        private List<GameObject> questionnaireQuestionObjects;
        private int currQuestionIdx;

        // Data Logging
        private string questionnaireTitle;
        private string questionnaireDataSavePath;
        private StreamWriter questionnaireDataWriter;
        private List<bool> participantDidSkip;

        // Questionnaire Events
        public UnityEvent OnQuestionnaireInitialized;

        private void Start()
        {
            InitializeRecording();

            SetUpQuestionnaire();
            SetUpButtons();

            print("Finished Questionnaire init");
            OnQuestionnaireInitialized?.Invoke();
        }

        private void InitializeRecording()
        {
            questionnaireTitle = ((gameObject.name).Replace(" ", "")).ToLower();
            participantDidSkip = new List<bool>();

            if (ReplayManager.Instance == null || !ReplayManager.Instance.enabled)
                return;

            questionnaireDataSavePath = Application.dataPath + "/" + questionnaireTitle + "_questionnaire_data.csv";
            questionnaireDataWriter = new StreamWriter(questionnaireDataSavePath);

            string[] transformDataHeaders = { "FrameCount", "Location", "Question", "User Value" };
            questionnaireDataWriter.WriteLine(string.Join(",", transformDataHeaders));
        }

        private void SetUpQuestionnaire()
        {
            questionnaireQuestionObjects = new List<GameObject>();
            currQuestionIdx = 0;

            Regex questionRgx = new Regex(@"Q\d{1,}");
            bool foundExisting = false;

            foreach (Transform child in transform)
            {
                if (questionRgx.IsMatch(child.name))
                {
                    questionnaireQuestionObjects.Add(child.gameObject);
                    foundExisting = true;
                }
            }


            if (foundExisting)
                return;

            for (int questionIdx = 0; questionIdx < questionnaireContent.questionnaireQuestions.Count; questionIdx++)
            {
                QuestionnaireQuestion questionnaireQuestion = questionnaireContent.questionnaireQuestions[questionIdx];
                GameObject questionnaireQuestionObj = null;

                if (questionnaireQuestion.choicesType == ChoicesType.Single)
                {
                    // Instantiate Questionnaire Template object
                    questionnaireQuestionObj = Instantiate(singleAnswerTemplate, new Vector3(0, 0, 0), Quaternion.identity);
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
                        GameObject instantiatedChoicePrefab = Instantiate(singleAnswerChoicePrefab,
                                                                          new Vector3(choicesParentRectTransform.offsetMin.x + (((choicesParentRectTransform.offsetMax.x - choicesParentRectTransform.offsetMin.x) / (questionnaireQuestion.choiceLabels.Count - 1)) * choiceLabelIdx), 0, 0),
                                                                          Quaternion.identity);
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
                else if (questionnaireQuestion.choicesType == ChoicesType.Multiple)
                {
                    // Instantiate Multiple Answers Template object
                    questionnaireQuestionObj = Instantiate(multipleAnswersTemplate, new Vector3(0, 0, 0), Quaternion.identity);
                    questionnaireQuestionObj.transform.SetParent(transform, false);
                    questionnaireQuestionObj.name = "Q" + (questionIdx + 1);

                    // Assign question string to question text (0)
                    questionnaireQuestionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.question;

                    // Instantiate choice prefabs & Set their parent (1) & Assign an appropriate label to each of them
                    Transform choicesParent = questionnaireQuestionObj.transform.GetChild(1);
                    for (int choiceLabelIdx = 0; choiceLabelIdx < questionnaireQuestion.choiceLabels.Count; choiceLabelIdx++)
                    {
                        RectTransform choicesParentRectTransform = choicesParent.GetComponent<RectTransform>();
                        GameObject instantiatedChoicePrefab = Instantiate(multipleAnswersChoicePrefab,
                                                                          new Vector3(choicesParentRectTransform.offsetMin.x + (((choicesParentRectTransform.offsetMax.x - choicesParentRectTransform.offsetMin.x) / (questionnaireQuestion.choiceLabels.Count - 1)) * choiceLabelIdx), 0, 0),
                                                                          Quaternion.identity);
                        instantiatedChoicePrefab.transform.SetParent(choicesParentRectTransform, false);
                        instantiatedChoicePrefab.name = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                        instantiatedChoicePrefab.GetComponentInChildren<TextMeshProUGUI>().text = questionnaireQuestion.choiceLabels[choiceLabelIdx];
                    }

                    // Assign Min Score Label (2) & Max Score Label (3)
                    questionnaireQuestionObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMinScore;
                    questionnaireQuestionObj.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = questionnaireQuestion.labelForMaxScore;
                }
                else if (questionnaireQuestion.choicesType == ChoicesType.Slider)
                {
                    // Instantiate Slider Template object
                    questionnaireQuestionObj = Instantiate(sliderTemplate, new Vector3(0, 0, 0), Quaternion.identity);
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
                }
            }
        }

        private void SetUpButtons()
        {
            // Set up an OnClick event for the prev button based on 
            TextMeshProUGUI prevButtonText = prevButton.GetComponentInChildren<TextMeshProUGUI>();
            Button prevButtonComponent = prevButton.GetComponent<Button>();
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
            }

            // Set up an OnClick event for the next button based on number of questions left
            TextMeshProUGUI nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            Button nextButtonComponent = nextButton.GetComponent<Button>();
            if (currQuestionIdx >= questionnaireQuestionObjects.Count - 1)
            {
                nextButtonText.text = "Submit";
                nextButtonComponent.onClick.RemoveAllListeners();
                nextButtonComponent.onClick.AddListener(SubmitQuestionnaire);
            }
            else
            {
                nextButtonText.text = "Next Question";
                nextButtonComponent.onClick.RemoveAllListeners();
                nextButtonComponent.onClick.AddListener(LoadNextQuestion);
            }

            // Set up an OnClick event for the skip button
            Button skipButtonComponent = skipButton.GetComponent<Button>();
            if (questionnaireContent.questionnaireQuestions[currQuestionIdx].canSkipQuestion)
            {
                if (currQuestionIdx >= questionnaireQuestionObjects.Count - 1)
                {
                    skipButtonComponent.onClick.RemoveAllListeners();
                    skipButtonComponent.onClick.AddListener(SkipAndSubmitQuestionnaire);
                    skipButton.SetActive(true);
                }
                else
                {
                    skipButtonComponent.onClick.RemoveAllListeners();
                    skipButtonComponent.onClick.AddListener(SkipQuestion);
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
        }

        private void LoadNextQuestion()
        {
            // Check if questionnaire question has at least one selected choice.
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
            // Check if questionnaire question has at least one selected choice.
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
            if (ReplayManager.Instance == null || !ReplayManager.Instance.enabled)
                yield break;

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

            Destroy(gameObject);
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
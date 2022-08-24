using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum ChoicesType { None, Single, Multiple, Slider }

[System.Serializable]
public class QuestionnaireQuestion
{
    [TextArea(10, 14)] public string question;
    public ChoicesType choicesType;
    public List<string> choiceLabels; // Only for single & multiple.
    public float minScore; // Only for slider.
    public float maxScore; // Only for slider.
    public bool wholeNumber; // Only for slider.
    public string labelForMinScore;
    public string labelForMaxScore;
    public bool canSkipQuestion;
}


[CreateAssetMenu(fileName = "QuestionnaireContent", menuName = "QuestionnaireContent", order = 1)]
public class QuestionnaireContent : ScriptableObject
{
    public List<QuestionnaireQuestion> questionnaireQuestions;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(QuestionnaireQuestion))]
public class QuestionnaireQuestionPropertyDrawer : PropertyDrawer
{

    private int GetEnumValueIndex(ChoicesType sat)
    {
        return Array.IndexOf(Enum.GetValues(typeof(ChoicesType)), sat);
    }

    private ChoicesType GetEnumValueFromIndex(int index)
    {
        return (ChoicesType)Enum.GetValues(typeof(ChoicesType)).GetValue(index);
    }

    private void ShowTypeField(Rect position, SerializedProperty property)
    {
        Rect rectLabel = new Rect(position.min.x, position.min.y, position.width, position.height);
        EditorGUI.LabelField(rectLabel, "Choice Type");

        int oldInt = property.enumValueIndex;
        ChoicesType oldType = GetEnumValueFromIndex(property.enumValueIndex);
        ChoicesType newType = oldType;
        float toggleWidth = position.width / 4f;

        Rect rectSingleAnswer = new Rect(position.min.x + toggleWidth, position.min.y, toggleWidth, position.height);
        if (EditorGUI.ToggleLeft(rectSingleAnswer, "Single", newType == ChoicesType.Single))
        {
            newType = ChoicesType.Single;
        }
        else if (newType == ChoicesType.Single)
        {
            newType = ChoicesType.None;
        }

        Rect rectMultiAnswer = new Rect(position.min.x + 2 * toggleWidth, position.min.y, toggleWidth, position.height);
        if (EditorGUI.ToggleLeft(rectMultiAnswer, "Multiple", newType == ChoicesType.Multiple))
        {
            newType = ChoicesType.Multiple;
        }
        else if (newType == ChoicesType.Multiple)
        {
            newType = ChoicesType.None;
        }

        Rect rectSlider = new Rect(position.min.x + 3 * toggleWidth, position.min.y, toggleWidth, position.height);
        if (EditorGUI.ToggleLeft(rectSlider, "Slider", newType == ChoicesType.Slider))
        {
            newType = ChoicesType.Slider;
        }
        else if (newType == ChoicesType.Slider)
        {
            newType = ChoicesType.None;
        }

        property.enumValueIndex = GetEnumValueIndex(newType);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // TODO: fix bug, long questions will bleed to the side of the UI
        EditorGUI.BeginProperty(position, label, property);
        Rect rectFoldout = new Rect(position.min.x, position.min.y, position.size.x, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(rectFoldout, property.isExpanded, label);
        if (property.isExpanded)
        {
            Rect rectQuestion = new Rect(position.min.x, position.min.y + EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight * 4);
            EditorGUI.PropertyField(rectQuestion, property.FindPropertyRelative("question"));
            // Rect rectType = new Rect(position.min.x + EditorGUIUtility.labelWidth, position.min.y + 5 * EditorGUIUtility.singleLineHeight, position.size.x - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect rectType = new Rect(position.min.x, position.min.y + 5 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
            SerializedProperty propType = property.FindPropertyRelative("choicesType");
            ShowTypeField(rectType, propType);
            var curProp = GetEnumValueFromIndex(propType.enumValueIndex);
            if (curProp == ChoicesType.None)
            {
                // show no fields
            }
            else
            {
                if (curProp == ChoicesType.Slider)
                {
                    Rect rectMinScore = new Rect(position.min.x, position.min.y + 7 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMinScore, property.FindPropertyRelative("minScore"));
                    Rect rectMaxScore = new Rect(position.min.x, position.min.y + 8 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMaxScore, property.FindPropertyRelative("maxScore"));
                    Rect rectWholeNumber = new Rect(position.min.x, position.min.y + 9 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectWholeNumber, property.FindPropertyRelative("wholeNumber"));
                    Rect rectMinLabel = new Rect(position.min.x, position.min.y + 10 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMinLabel, property.FindPropertyRelative("labelForMinScore"));
                    Rect rectMaxLabel = new Rect(position.min.x, position.min.y + 11 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMaxLabel, property.FindPropertyRelative("labelForMaxScore"));
                    Rect rectCanSkipQuestion = new Rect(position.min.x, position.min.y + 12 * EditorGUIUtility.singleLineHeight, position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectCanSkipQuestion, property.FindPropertyRelative("canSkipQuestion"));
                }
                else
                {
                    Rect rectChoiceLabels = new Rect(position.x, position.y + 6 * EditorGUIUtility.singleLineHeight, position.size.x, position.y);
                    EditorGUI.PropertyField(rectChoiceLabels, property.FindPropertyRelative("choiceLabels"), includeChildren: true);
                    Rect rectMinLabel = new Rect(position.min.x, position.min.y + 7 * EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels")), position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMinLabel, property.FindPropertyRelative("labelForMinScore"));
                    Rect rectMaxLabel = new Rect(position.min.x, position.min.y + 8 * EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels")), position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectMaxLabel, property.FindPropertyRelative("labelForMaxScore"));
                    Rect rectCanSkipQuestion = new Rect(position.min.x, position.min.y + 9 * EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels")), position.size.x, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(rectCanSkipQuestion, property.FindPropertyRelative("canSkipQuestion"));
                }
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int totalLines = 1;
        float padding = 0;

        if (property.isExpanded)
        {
            totalLines += 5; // for type field
            SerializedProperty propType = property.FindPropertyRelative("choicesType");
            switch (GetEnumValueFromIndex(propType.enumValueIndex))
            {
                case ChoicesType.None:
                    break;
                case ChoicesType.Single:
                    totalLines += 4;
                    // totalLines += (int)Math.Ceiling(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels")) / EditorGUIUtility.singleLineHeight);
                    padding = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels"));
                    break;
                case ChoicesType.Multiple:
                    // if inner list is expanded, then increase total lines by more than 3. If not, increase by 3.
                    totalLines += 4;
                    // totalLines += (int)Math.Ceiling(EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels")) / EditorGUIUtility.singleLineHeight);
                    padding = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("choiceLabels"));
                    break;
                case ChoicesType.Slider:
                    totalLines += 7;
                    break;
            }
        }

        return EditorGUIUtility.singleLineHeight * totalLines + EditorGUIUtility.standardVerticalSpacing * (totalLines - 1) + padding;
    }
}
#endif
// End of File.
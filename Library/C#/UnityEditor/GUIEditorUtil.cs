using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    public static class GUIEditorUtil
    {
        #region [创建GUI]

        public static void Title(string title)
        {
            var os = GUI.skin.label.fontSize;
            GUILayout.Space(10);

            GUI.skin.label.fontSize = 25;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(title);

            GUILayout.Space(5);
            GUI.skin.label.fontSize = os;
        }

        public static void Text(string text, int fontSize = 12)
        {
            GUI.skin.button.richText = true;
            GUI.skin.label.richText = true;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = fontSize;
            GUILayout.Label(text);
        }

        public static string TextInput(string lable, string inputStr,int space = 0)
        {
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 12;

            inputStr = EditorGUILayout.TextField(lable, inputStr);
            if(space!=0)
                GUILayout.Space(space);
            return inputStr;
        }
        public static int IntInput(string text, int index, float align_up = 0, float align_down = 20f)
        {
            GUILayout.Space(align_up);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 12;
            if (string.IsNullOrEmpty(text))
                index = EditorGUILayout.IntField(index);
            else
                index = EditorGUILayout.IntField(text, index);
            GUILayout.Space(align_down);
            return index;
        }
        public static float FloatInput(string text, float index, float align_up = 0, float align_down = 20f)
        {
            GUILayout.Space(align_up);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 12;
            if (string.IsNullOrEmpty(text))
                index = EditorGUILayout.FloatField(index);
            else
                index = EditorGUILayout.FloatField(text, index);
            GUILayout.Space(align_down);
            return index;
        }

        public static int PopUp(string label, int index, string[] options, float align_up = 0, float align_down = 20f)
        {
            GUILayout.Space(align_up);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 12;
            index = EditorGUILayout.Popup(label, index, options, GUILayout.ExpandWidth(false));
            GUILayout.Space(align_down);
            return index;

        }

        public static bool Toggle(bool bl, string text, float align_up = 0, float align_down = 20f)
        {
            GUILayout.Space(align_up);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 15;
            bl = EditorGUILayout.ToggleLeft(text, bl);
            GUILayout.Space(align_down);
            return bl;
        }

        public static bool Button(string text)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            return GUILayout.Button(text);
        }

        public static void Button(string text,Action action)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            if (GUILayout.Button(text))
                action();
        }

        public static void Horizontal(Action action) {
            GUILayout.BeginHorizontal();
            action();
            GUILayout.EndHorizontal();
        }

        public static T ObjectField<T>(string label,T obj, params GUILayoutOption[] options) where T: UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(T),true, options) as T;
        }

        public static void List() { 
        //EditorGUI.ObjectField
        }

        #endregion
    }
}
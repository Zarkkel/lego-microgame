﻿using LEGOModelImporter;
using System.Collections.Generic;
using System.IO;
using Unity.LEGO.Behaviours;
using Unity.LEGO.Behaviours.Actions;
using Unity.LEGO.Behaviours.Triggers;
using UnityEditor;
using UnityEngine;

namespace Unity.LEGO.EditorExt
{

    [CustomEditor(typeof(Model))]
    public class ModelEditor : LEGOModelImporter.ModelEditor
    {
        static Dictionary<string, Texture> s_BehaviourTextures;

        static GUIStyle s_ImageStyle;
        static GUIStyle s_LabelStyle;

        Model m_Model;

        List<(Editor, string, Texture)> m_BehaviourEditorAndNameAndTextures = new List<(Editor, string, Texture)>();

        protected override void OnEnable()
        {
            base.OnEnable();

            if (s_BehaviourTextures == null)
            {
                s_BehaviourTextures = new Dictionary<string, Texture>();

                s_BehaviourTextures.Add("Blue", AssetDatabase.LoadAssetAtPath<Texture>("Assets/LEGO/Textures/LEGO Behaviour Icons/Script Blue.png"));
                s_BehaviourTextures.Add("Yellow", AssetDatabase.LoadAssetAtPath<Texture>("Assets/LEGO/Textures/LEGO Behaviour Icons/Script Yellow.png"));
                s_BehaviourTextures.Add("Red", AssetDatabase.LoadAssetAtPath<Texture>("Assets/LEGO/Textures/LEGO Behaviour Icons/Script Red.png"));
                s_BehaviourTextures.Add("Green", AssetDatabase.LoadAssetAtPath<Texture>("Assets/LEGO/Textures/LEGO Behaviour Icons/Script Green.png"));
            }

            m_Model = (Model)target;
        }

        void OnDisable()
        {
            foreach(var editorAndNameAndTexture in m_BehaviourEditorAndNameAndTextures)
            {
                DestroyImmediate(editorAndNameAndTexture.Item1);
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_ImageStyle == null)
            {
                s_ImageStyle = new GUIStyle(EditorStyles.label);
                s_ImageStyle.fixedWidth = 48;
                s_ImageStyle.fixedHeight = 48;
                s_ImageStyle.padding = new RectOffset(0, 10, 0, 0);

                s_LabelStyle = new GUIStyle(EditorStyles.boldLabel);
                s_LabelStyle.fixedHeight = 48;
                s_LabelStyle.alignment = TextAnchor.MiddleLeft;
            }

            UpdateBehaviourEditorList();

            GUILayout.Label("LEGO Behaviours", EditorStyles.boldLabel);

            foreach(var editorAndNameAndTexture in m_BehaviourEditorAndNameAndTextures)
            {
                if (editorAndNameAndTexture.Item1.serializedObject.targetObject != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(editorAndNameAndTexture.Item3, s_ImageStyle);
                    GUILayout.Label(editorAndNameAndTexture.Item2, s_LabelStyle);
                    EditorGUILayout.EndHorizontal();
                    editorAndNameAndTexture.Item1.OnInspectorGUI();
                }
            }

            GUILayout.Space(16);

            // Clone of ModelEditor from package - hides absolute path.

            serializedObject.Update();

            var doReimport = false;
            string newReimportPath = null;

            GUILayout.Label("Import", EditorStyles.boldLabel);
            if (m_Model.autoGenerated)
            {
                EditorGUILayout.HelpBox("This model was created by brick building. You cannot reimport it.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Pivot", ObjectNames.NicifyVariableName(m_Model.pivot.ToString()));
                EditorGUILayout.LabelField("File Path", m_Model.relativeFilePath, EditorStyles.wordWrappedLabel);

                GUILayout.Space(16);

                // Check if part of prefab instance and prevent reimport.
                if (PrefabUtility.IsPartOfAnyPrefab(target))
                {
                    EditorGUILayout.HelpBox("You cannot reimport a prefab instance. Please perform reimporting on the prefab itself.", MessageType.Warning);
                }
                else
                {
                    if (File.Exists(m_Model.relativeFilePath) || File.Exists(m_Model.absoluteFilePath))
                    {
                        doReimport = GUILayout.Button("Reimport");
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Could not find original file.", MessageType.Warning);
                    }

                    if (GUILayout.Button("Reimport From New File"))
                    {
                        var path = EditorUtility.OpenFilePanelWithFilters("Select model file", "Packages/com.unity.lego.modelimporter/Models", new string[] { "All model files", "ldr,io,lxfml,lxf", "LDraw files", "ldr", "Studio files", "io", "LXFML files", "lxfml", "LXF files", "lxf" });
                        if (path.Length != 0)
                        {
                            newReimportPath = path;
                            doReimport = true;
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (doReimport)
            {
                ImportModel.ReimportModel(m_Model, newReimportPath);
            }
        }

        void OnSceneGUI()
        {
            foreach (var editorAndNameAndTexture in m_BehaviourEditorAndNameAndTextures)
            {
                if (editorAndNameAndTexture.Item1.target != null)
                {
                    ((LEGOBehaviourEditor)editorAndNameAndTexture.Item1).OnSceneGUI();
                }
            }
        }

        void UpdateBehaviourEditorList()
        {
            foreach (var editorAndNameAndTexture in m_BehaviourEditorAndNameAndTextures)
            {
                DestroyImmediate(editorAndNameAndTexture.Item1);
            }

            m_BehaviourEditorAndNameAndTextures.Clear();
            var behaviours = m_Model.GetComponentsInChildren<LEGOBehaviour>();

            foreach (var behaviour in behaviours)
            {
                var texture = s_BehaviourTextures["Green"];
                var behaviourType = behaviour.GetType();
                if (behaviourType.IsSubclassOf(typeof(MovementAction)))
                {
                    texture = s_BehaviourTextures["Blue"];
                }
                else if (behaviourType.IsSubclassOf(typeof(Trigger)))
                {
                    texture = s_BehaviourTextures["Yellow"];
                }
                else if (behaviourType == typeof(HazardAction) || behaviourType == typeof(LoseAction))
                {
                    texture = s_BehaviourTextures["Red"];
                }

                m_BehaviourEditorAndNameAndTextures.Add((CreateEditor(behaviour), ObjectNames.NicifyVariableName(behaviourType.Name), texture));
            }
        }
    }
}
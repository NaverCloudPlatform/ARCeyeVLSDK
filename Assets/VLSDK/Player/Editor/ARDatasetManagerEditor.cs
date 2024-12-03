using UnityEngine;
using UnityEditor;
using System.IO;

namespace ARCeye.Dataset
{
    [CustomEditor(typeof(ARDatasetManager))]
    public class ARDatasetManagerEditor : Editor
    {
        private bool m_IsDragging = false;
        private bool m_IsPlaying = true;

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        public override void OnInspectorGUI()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            EditorGUILayout.LabelField("Dataset", EditorStyles.boldLabel);

            DrawDatasetSelectorArea();

            EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
            
            DrawProgress();

            DrawControlArea();

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if(Event.current != null && Event.current.type == EventType.Used)
            {
                m_IsDragging = true;
            }
            else if(m_IsPlaying)
            {
                m_IsDragging = false;
            }
        }

        void OnEditorUpdate()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            if (datasetManager.isUpdating)
            {
                Repaint();
            }
            else if (m_IsDragging)
            {
                // 슬라이더 조작 중에는 업데이트를 멈춤
                datasetManager.isUpdating = false;
            }
        }

        private void DrawDatasetSelectorArea()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;
            
            // 데이터셋 이름 출력.            
            if(!string.IsNullOrEmpty(datasetManager.datasetPath))
            {
                string directoryName = new DirectoryInfo(datasetManager.datasetPath).Name;
                EditorGUILayout.LabelField(directoryName);
            }

            // 데이터셋 경로 선택.
            string datasetRootPath = Application.persistentDataPath;

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if(GUILayout.Button("Select Dataset Directory"))
            {
                string path = EditorUtility.OpenFolderPanel("Select a dataset directory", datasetRootPath, "");

                if(CheckPathValidation(path))
                {
                    datasetManager.datasetPath = path;
                }
                else
                {
                    Debug.LogError("Selected directory is not a valid dataset directory");
                }
            }
            GUI.backgroundColor = originalColor;

            if(GUILayout.Button("Open Persistent Data Path"))
            {
                EditorUtility.RevealInFinder(datasetRootPath);
            }
        }

        private bool CheckPathValidation(string directoryPath)
        {
            string dataBinPath = directoryPath + "/data.bin";
            return File.Exists(dataBinPath);
        }

        private void DrawProgress()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;
            datasetManager.progress = EditorGUILayout.Slider("Progress", datasetManager.progress, 0.0f, 1.0f);
        }

        private void DrawControlArea()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            GUILayout.BeginHorizontal();

            GUILayout.Label("Play Speed");

            float speed = datasetManager.playSpeed;

            if(GUILayout.Button($"x{speed.ToString("N0")}"))
            {
                datasetManager.TogglePlaySpeed();
            }

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();

            GUI.enabled = !datasetManager.isUpdating;

            if(GUILayout.Button("Play"))
            {
                m_IsPlaying = true;
                m_IsDragging = false;
                datasetManager.isUpdating = true;
            }

            GUI.enabled = datasetManager.isUpdating;

            if(GUILayout.Button("Pause"))
            {
                m_IsPlaying = false;
                datasetManager.isUpdating = false;
            }

            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }
    }
}
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
        private bool m_Scrubbing = false;   // 동영상 타임라인을 드래그하는 중

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

            if (datasetManager.IsVideoMode)
            {
                EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawProgress();
                DrawControlArea();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

                DrawProgress();
                DrawControlArea();
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (Event.current != null && Event.current.type == EventType.Used)
            {
                m_IsDragging = true;
            }
            else if (m_IsPlaying)
            {
                m_IsDragging = false;
            }
        }

        void OnEditorUpdate()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            if (datasetManager.IsUpdating || m_Scrubbing)
            {
                Repaint();
            }
            else if (m_IsDragging)
            {
                // 슬라이더 조작 중에는 업데이트를 멈춤
                datasetManager.IsUpdating = false;
            }
        }

        private void DrawDatasetSelectorArea()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            // 데이터셋 이름 출력.            
            if (!string.IsNullOrEmpty(datasetManager.DatasetPath))
            {
                string directoryName = new DirectoryInfo(datasetManager.DatasetPath).Name;
                EditorGUILayout.LabelField(directoryName);
            }

            // 데이터셋 경로 선택.
            string datasetRootPath = LoadPreviousDatasetPath();

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            // 동영상 데이터셋(ANMetaRecorder) 파일 선택.
            if (GUILayout.Button("Select Video Dataset"))
            {
                string path = EditorUtility.OpenFilePanel("Select a video dataset file", datasetRootPath, "mp4,mov,m4v");

                if (!string.IsNullOrEmpty(path))
                {
                    datasetManager.DatasetPath = path;
                    SaveDatasetPath();
                }
            }

            GUI.backgroundColor = originalColor;

            // 구버전 데이터셋(data.bin 폴더) 선택. deprecated, 당분간 함께 동작.
            if (GUILayout.Button("Select Dataset Directory (Legacy)"))
            {
                string path = EditorUtility.OpenFolderPanel("Select a dataset directory", datasetRootPath, "");

                if (CheckPathValidation(path))
                {
                    datasetManager.DatasetPath = path;
                    SaveDatasetPath();
                }
                else
                {
                    Debug.LogError("Selected directory is not a valid dataset directory");
                }
            }

            if (GUILayout.Button("Open Dataset Path"))
            {
                EditorUtility.RevealInFinder(datasetRootPath);
            }
        }

        private string LoadPreviousDatasetPath()
        {
            string prevDatasetPath = EditorPrefs.GetString("DatasetPath", Application.persistentDataPath);

            if (Directory.Exists(prevDatasetPath))
            {
                return prevDatasetPath;
            }
            else
            {
                string parentPath = Directory.GetParent(prevDatasetPath).FullName;
                if (Directory.Exists(parentPath))
                {
                    return parentPath;
                }
                else
                {
                    return Application.persistentDataPath;
                }
            }
        }

        private void SaveDatasetPath()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;
            EditorPrefs.SetString("DatasetPath", datasetManager.DatasetPath);
        }

        private bool CheckPathValidation(string directoryPath)
        {
            string dataBinPath = directoryPath + "/data.bin";
            return File.Exists(dataBinPath);
        }

        private void DrawProgress()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            if (datasetManager.IsVideoMode)
            {
                float total = datasetManager.GetTotalSeconds();

                // 우측 숫자 필드 없는 타임라인. 한 줄 높이 rect를 명시적으로 확보해
                // 아래 시간 라벨 줄과 겹치지 않게 함. 재생에 의한 값 변화는 seek 대상이 아님(EndChangeCheck로 구분).
                Rect sliderRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

                EditorGUI.BeginChangeCheck();
                float progress = GUI.HorizontalSlider(sliderRect, datasetManager.Progress, 0.0f, 1.0f);

                if (EditorGUI.EndChangeCheck())
                {
                    // 드래그 중에는 재생 쪽 Progress 갱신을 멈춰 핸들이 마우스를 그대로 따라오게 함.
                    datasetManager.IsUpdating = false;
                    m_Scrubbing = true;
                    datasetManager.Progress = progress;
                    datasetManager.Seek(progress * total);
                }

                // 마우스를 떼면(hotControl 해제) 재생 쪽 Progress 갱신 재개.
                if (m_Scrubbing && GUIUtility.hotControl == 0)
                {
                    m_Scrubbing = false;
                    datasetManager.IsUpdating = true;
                }

                // 현재 시간 / 전체 시간.
                GUILayout.BeginHorizontal();
                GUILayout.Label(FormatTime(datasetManager.Progress * total), EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label(FormatTime(total), EditorStyles.miniLabel);
                GUILayout.EndHorizontal();
            }
            else
            {
                datasetManager.Progress = EditorGUILayout.Slider("Progress", datasetManager.Progress, 0.0f, 1.0f);
            }
        }

        private void DrawControlArea()
        {
            ARDatasetManager datasetManager = (ARDatasetManager)target;

            if (datasetManager.IsVideoMode)
            {
                DrawVideoControls(datasetManager);
            }
            else
            {
                DrawLegacyControls(datasetManager);
            }
        }

        // 동영상 모드 컨트롤: Play/Pause 토글 + 재생 속도 선택.
        private void DrawVideoControls(ARDatasetManager datasetManager)
        {
            GUILayout.Space(2);

            // Play / Pause 토글 (네이티브 아이콘).
            Texture icon = EditorGUIUtility.IconContent(m_IsPlaying ? "PauseButton" : "PlayButton").image;
            if (GUILayout.Button(new GUIContent(m_IsPlaying ? " Pause" : " Play", icon), GUILayout.Height(26)))
            {
                m_IsPlaying = !m_IsPlaying;
                m_IsDragging = false;

                if (m_IsPlaying)
                {
                    datasetManager.Play();
                }
                else
                {
                    datasetManager.Pause();
                }
            }

            GUILayout.Space(2);

            // 재생 속도. 현재 선택된 속도를 강조.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed", GUILayout.Width(48));

            float[] speeds = datasetManager.PlaySpeeds;
            int currentIndex = datasetManager.PlaySpeedIndex;

            for (int i = 0; i < speeds.Length; i++)
            {
                Color prev = GUI.backgroundColor;
                if (i == currentIndex)
                {
                    GUI.backgroundColor = new Color(0.35f, 0.65f, 1.0f);
                }

                if (GUILayout.Button($"x{speeds[i]:0}", EditorStyles.miniButton))
                {
                    datasetManager.SetPlaySpeed(i);
                }

                GUI.backgroundColor = prev;
            }

            GUILayout.EndHorizontal();
        }

        // 구버전(정지영상) 모드 컨트롤: IsUpdating 기반 재생/일시정지.
        private void DrawLegacyControls(ARDatasetManager datasetManager)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Play Speed");
            if (GUILayout.Button($"x{datasetManager.PlaySpeed:0}"))
            {
                datasetManager.TogglePlaySpeed();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUI.enabled = !datasetManager.IsUpdating;
            if (GUILayout.Button("Play"))
            {
                m_IsPlaying = true;
                m_IsDragging = false;
                datasetManager.IsUpdating = true;
            }

            GUI.enabled = datasetManager.IsUpdating;
            if (GUILayout.Button("Pause"))
            {
                m_IsPlaying = false;
                datasetManager.IsUpdating = false;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds))
            {
                seconds = 0f;
            }

            int minutes = (int)(seconds / 60f);
            int secs = (int)(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
    }
}
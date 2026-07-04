using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#if UNITY_URP_ACTIVE || true
using UnityEngine.Rendering.Universal;
#endif

namespace TombOfServilii
{
    public class SceneSetupHelper : EditorWindow
    {
        [MenuItem("Tomb of Servilii/Auto Setup Scene")]
        public static void AutoSetup()
        {
            Debug.Log("SceneSetupHelper: Starting automated scene setup...");

            // 0. Auto-create and populate default ScriptableObject data assets
            CreateDefaultDataAssets();

            // 1. Find Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = GameObject.FindObjectOfType<Camera>();
            }

            if (mainCam == null)
            {
                Debug.LogError("SceneSetupHelper: Could not find any Camera in the scene. Please add a Camera first!");
                return;
            }

            // Ensure Main Camera has PlayerInteraction
            PlayerInteraction interaction = mainCam.GetComponent<PlayerInteraction>();
            if (interaction == null)
            {
                interaction = mainCam.gameObject.AddComponent<PlayerInteraction>();
                Undo.RegisterCreatedObjectUndo(interaction, "Add Player Interaction");
            }

            // Ensure FPSHands layer exists
            int handLayer = LayerMask.NameToLayer("FPSHands");
            if (handLayer == -1)
            {
                Debug.LogWarning("SceneSetupHelper: 'FPSHands' layer is not defined in your Project Settings. Please add it to Tags and Layers!");
                handLayer = 0; // Default fallback
            }

            // 2. Setup Hand Camera (Overlay)
            Camera handCam = null;
            Transform handCamTransform = mainCam.transform.Find("HandCamera");
            if (handCamTransform != null)
            {
                handCam = handCamTransform.GetComponent<Camera>();
            }
            else
            {
                GameObject handCamObj = new GameObject("HandCamera");
                handCamObj.transform.SetParent(mainCam.transform);
                handCamObj.transform.localPosition = Vector3.zero;
                handCamObj.transform.localRotation = Quaternion.identity;
                handCam = handCamObj.AddComponent<Camera>();
                Undo.RegisterCreatedObjectUndo(handCamObj, "Create Hand Camera");
            }

            handCam.clearFlags = CameraClearFlags.Nothing;
            handCam.cullingMask = 1 << handLayer;
            handCam.depth = mainCam.depth + 1;

            // Register in URP stack
            #if UNITY_URP_ACTIVE || true
            var mainCamData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (mainCamData == null)
            {
                mainCamData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            mainCamData.renderType = CameraRenderType.Base;

            var handCamData = handCam.GetComponent<UniversalAdditionalCameraData>();
            if (handCamData == null)
            {
                handCamData = handCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            handCamData.renderType = CameraRenderType.Overlay;

            if (!mainCamData.cameraStack.Contains(handCam))
            {
                mainCamData.cameraStack.Add(handCam);
                EditorUtility.SetDirty(mainCam);
            }
            #endif

            // 3. Create Hands container
            GameObject handsContainer = null;
            Transform handsTransform = mainCam.transform.Find("FPSHands");
            if (handsTransform != null)
            {
                handsContainer = handsTransform.gameObject;
            }
            else
            {
                handsContainer = new GameObject("FPSHands");
                handsContainer.transform.SetParent(mainCam.transform);
                handsContainer.transform.localPosition = new Vector3(0.3f, -0.3f, 0.4f);
                handsContainer.transform.localRotation = Quaternion.identity;
                handsContainer.layer = handLayer;
                handsContainer.AddComponent<Animator>();
                handsContainer.AddComponent<HandAnimationController>();
                Undo.RegisterCreatedObjectUndo(handsContainer, "Create FPSHands");
            }

            // Link hand controller to player interaction
            var handAnimCtrl = handsContainer.GetComponent<HandAnimationController>();
            var serializedInteraction = new SerializedObject(interaction);
            serializedInteraction.FindProperty("handAnimator").objectReferenceValue = handAnimCtrl;
            serializedInteraction.ApplyModifiedProperties();

            // 4. Find or Create HUD Canvas
            GameObject canvasObj = GameObject.Find("HUDCanvas");
            if (canvasObj == null) canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Canvas existingCanvas = GameObject.FindObjectOfType<Canvas>();
                if (existingCanvas != null)
                {
                    canvasObj = existingCanvas.gameObject;
                }
            }
            if (canvasObj == null)
            {
                canvasObj = new GameObject("HUDCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUDCanvas");
            }

            // 5. Setup Subtitle UI under Canvas
            Transform subtitlePanelTrans = FindChildRecursive(canvasObj.transform, "SubtitlePanel");
            GameObject subtitlePanel = null;
            if (subtitlePanelTrans != null)
            {
                subtitlePanel = subtitlePanelTrans.gameObject;
            }
            else
            {
                subtitlePanel = new GameObject("SubtitlePanel");
                subtitlePanel.transform.SetParent(canvasObj.transform, false);
                
                Image img = subtitlePanel.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.4f);

                RectTransform rect = subtitlePanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 30f);
                rect.sizeDelta = new Vector2(0f, 80f);

                subtitlePanel.AddComponent<CanvasGroup>();
                Undo.RegisterCreatedObjectUndo(subtitlePanel, "Create Subtitle Panel");
            }

            TextMeshProUGUI subtitleTextComp = subtitlePanel.GetComponentInChildren<TextMeshProUGUI>();
            if (subtitleTextComp == null)
            {
                GameObject textObj = new GameObject("SubtitleText");
                textObj.transform.SetParent(subtitlePanel.transform, false);
                subtitleTextComp = textObj.AddComponent<TextMeshProUGUI>();
                subtitleTextComp.fontSize = 24;
                subtitleTextComp.alignment = TextAlignmentOptions.Center;
                subtitleTextComp.text = "";

                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            // Setup SubtitleManager
            SubtitleManager subtitleManager = GameObject.FindObjectOfType<SubtitleManager>();
            if (subtitleManager == null)
            {
                GameObject smObj = new GameObject("SubtitleManager");
                subtitleManager = smObj.AddComponent<SubtitleManager>();
                Undo.RegisterCreatedObjectUndo(smObj, "Create SubtitleManager");
            }

            var serializedSM = new SerializedObject(subtitleManager);
            serializedSM.FindProperty("subtitleText").objectReferenceValue = subtitleTextComp;
            serializedSM.FindProperty("canvasGroup").objectReferenceValue = subtitlePanel.GetComponent<CanvasGroup>();
            serializedSM.ApplyModifiedProperties();

            // Setup InteractHintText under Canvas
            Transform hintTrans = FindChildRecursive(canvasObj.transform, "InteractHintText");
            GameObject hintObj = null;
            if (hintTrans != null)
            {
                hintObj = hintTrans.gameObject;
            }
            else
            {
                hintObj = new GameObject("InteractHintText");
                hintObj.transform.SetParent(canvasObj.transform, false);
                
                TextMeshProUGUI hintTextComp = hintObj.AddComponent<TextMeshProUGUI>();
                hintTextComp.fontSize = 24;
                hintTextComp.alignment = TextAlignmentOptions.Center;
                hintTextComp.text = "Press E to Open Gate";
                hintTextComp.color = Color.white;

                // Position it near the center or bottom-center of the screen
                RectTransform rect = hintObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.4f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, 60f);

                hintObj.SetActive(false);
                Undo.RegisterCreatedObjectUndo(hintObj, "Create InteractHintText");
            }

            // Link hint UI to PlayerInteraction
            serializedInteraction.FindProperty("interactHintUI").objectReferenceValue = hintObj;
            serializedInteraction.ApplyModifiedProperties();

            // 6. Setup Info Menu UI Panel
            Transform infoMenuTrans = FindChildRecursive(canvasObj.transform, "InfoMenuPanel");
            GameObject infoMenuPanel = null;
            if (infoMenuTrans != null)
            {
                infoMenuPanel = infoMenuTrans.gameObject;
            }
            else
            {
                infoMenuPanel = new GameObject("InfoMenuPanel");
                infoMenuPanel.transform.SetParent(canvasObj.transform, false);
                Image img = infoMenuPanel.AddComponent<Image>();
                img.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Glassmorphism Dark Slate

                RectTransform rect = infoMenuPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.9f);
                rect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(infoMenuPanel, "Create Info Menu Panel");
            }

            // Setup Title Window
            TextMeshProUGUI titleText = null;
            Transform titleTrans = FindChildRecursive(infoMenuPanel.transform, "TopicTitleText");
            if (titleTrans != null)
            {
                titleText = titleTrans.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject titleObj = new GameObject("TopicTitleText");
                titleObj.transform.SetParent(infoMenuPanel.transform, false);
                titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.fontSize = 32;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = new Color(1f, 0.84f, 0f); // Gold #FFD700
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.text = "Select a Topic";

                RectTransform rect = titleObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.38f, 0.8f);
                rect.anchorMax = new Vector2(0.95f, 0.93f);
                rect.sizeDelta = Vector2.zero;
            }

            // Setup Content Window
            TextMeshProUGUI contentText = null;
            Transform contentTrans = FindChildRecursive(infoMenuPanel.transform, "TopicContentText");
            if (contentTrans != null)
            {
                contentText = contentTrans.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject contentObj = new GameObject("TopicContentText");
                contentObj.transform.SetParent(infoMenuPanel.transform, false);
                contentText = contentObj.AddComponent<TextMeshProUGUI>();
                contentText.fontSize = 20;
                contentText.color = new Color(0.9f, 0.92f, 0.95f); // Soft white
                contentText.alignment = TextAlignmentOptions.TopLeft;
                contentText.lineSpacing = 1.3f;
                contentText.text = "Please click on a topic to read and listen to the information.";

                RectTransform rect = contentObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.38f, 0.22f);
                rect.anchorMax = new Vector2(0.95f, 0.76f);
                rect.sizeDelta = Vector2.zero;
            }

            // Setup Start Quiz Button
            Button startQuizBtn = null;
            Transform startQuizTrans = FindChildRecursive(infoMenuPanel.transform, "Btn_StartQuiz");
            if (startQuizTrans != null)
            {
                startQuizBtn = startQuizTrans.GetComponent<Button>();
            }
            else
            {
                GameObject btnObj = new GameObject("Btn_StartQuiz");
                btnObj.transform.SetParent(infoMenuPanel.transform, false);
                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.18f, 0.8f, 0.44f); // Emerald Green #2ECC71
                startQuizBtn = btnObj.AddComponent<Button>();

                GameObject btnTxtObj = new GameObject("Text");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                var txt = btnTxtObj.AddComponent<TextMeshProUGUI>();
                txt.text = "Start Quiz ➔";
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 18;

                RectTransform rect = btnObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.72f, 0.06f);
                rect.anchorMax = new Vector2(0.95f, 0.16f);
                rect.sizeDelta = Vector2.zero;

                RectTransform txtRect = btnTxtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(btnObj, "Create Start Quiz Button");
            }

            // Create VerticalLayoutGroup container for Topic Buttons
            Transform groupTrans = FindChildRecursive(infoMenuPanel.transform, "TopicButtonsGroup");
            GameObject groupObj = null;
            if (groupTrans != null)
            {
                groupObj = groupTrans.gameObject;
            }
            else
            {
                groupObj = new GameObject("TopicButtonsGroup");
                groupObj.transform.SetParent(infoMenuPanel.transform, false);
                
                RectTransform rect = groupObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.15f);
                rect.anchorMax = new Vector2(0.32f, 0.85f);
                rect.sizeDelta = Vector2.zero;

                VerticalLayoutGroup layout = groupObj.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 15f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                Undo.RegisterCreatedObjectUndo(groupObj, "Create TopicButtonsGroup");
            }

            // Setup InfoMenuManager
            InfoMenuManager infoMenuManager = GameObject.FindObjectOfType<InfoMenuManager>();
            if (infoMenuManager == null)
            {
                GameObject managerObj = new GameObject("InfoMenuManager");
                infoMenuManager = managerObj.AddComponent<InfoMenuManager>();
                managerObj.AddComponent<AudioSource>(); // Narration source
                Undo.RegisterCreatedObjectUndo(managerObj, "Create InfoMenuManager");
            }

            // Find and bind the 5 Topic Buttons persistently
            string[] topicBtnNames = { "WhatIsIt", "WhoBuiltIt", "WhyBuiltIt", "Design", "History" };
            for (int i = 0; i < 5; i++)
            {
                Button btn = null;
                Transform btnTrans = infoMenuPanel.transform.Find(topicBtnNames[i]);
                if (btnTrans == null)
                {
                    btnTrans = infoMenuPanel.transform.Find("Btn_" + topicBtnNames[i]);
                }
                if (btnTrans == null && groupObj != null)
                {
                    btnTrans = groupObj.transform.Find(topicBtnNames[i]);
                }
                if (btnTrans == null && groupObj != null)
                {
                    btnTrans = groupObj.transform.Find("Btn_" + topicBtnNames[i]);
                }

                if (btnTrans != null)
                {
                    btn = btnTrans.GetComponent<Button>();
                    btnTrans.SetParent(groupObj.transform, false);
                }
                else
                {
                    // Create it if it doesn't exist
                    GameObject btnObj = new GameObject(topicBtnNames[i]);
                    btnObj.transform.SetParent(groupObj.transform, false);
                    Image btnImg = btnObj.AddComponent<Image>();
                    btnImg.color = new Color(0.12f, 0.14f, 0.19f, 1f); // Premium button slate background
                    btn = btnObj.AddComponent<Button>();

                    GameObject txtObj = new GameObject("Text");
                    txtObj.transform.SetParent(btnObj.transform, false);
                    var txt = txtObj.AddComponent<TextMeshProUGUI>();
                    txt.text = topicBtnNames[i];
                    txt.color = Color.white;
                    txt.fontStyle = FontStyles.Bold;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontSize = 16;

                    RectTransform rect = btnObj.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(0f, 55f);

                    RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                    txtRect.anchorMin = Vector2.zero;
                    txtRect.anchorMax = Vector2.one;
                    txtRect.sizeDelta = Vector2.zero;

                    LayoutElement le = btnObj.AddComponent<LayoutElement>();
                    le.preferredHeight = 55f;

                    Undo.RegisterCreatedObjectUndo(btnObj, "Create " + topicBtnNames[i]);
                }

                // Ensure layout element exists for layout groups
                if (btn != null)
                {
                    LayoutElement le = btn.GetComponent<LayoutElement>();
                    if (le == null)
                    {
                        le = btn.gameObject.AddComponent<LayoutElement>();
                    }
                    le.preferredHeight = 55f;

                    // Style buttons
                    FormatButtonText(btn, topicBtnNames[i]);
                    Image btnImg = btn.GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.color = new Color(0.12f, 0.14f, 0.19f, 1f);
                    }

                    ClearPersistentListeners(btn);
                    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(btn.onClick, infoMenuManager.PlayTopic, i);
                    EditorUtility.SetDirty(btn);
                }
            }

            // Bind Start Quiz Button Click persistently
            if (startQuizBtn != null)
            {
                FormatButtonText(startQuizBtn, "Start Quiz ➔");
                ClearPersistentListeners(startQuizBtn);
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(startQuizBtn.onClick, infoMenuManager.OnClickStartQuiz);
                EditorUtility.SetDirty(startQuizBtn);
            }

            // 7. Setup Quiz UI Panel
            Transform quizPanelTrans = FindChildRecursive(canvasObj.transform, "QuizPanel");
            GameObject quizPanel = null;
            if (quizPanelTrans != null)
            {
                quizPanel = quizPanelTrans.gameObject;
            }
            else
            {
                quizPanel = new GameObject("QuizPanel");
                quizPanel.transform.SetParent(canvasObj.transform, false);
                Image img = quizPanel.AddComponent<Image>();
                img.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Glassmorphism Dark Slate

                RectTransform rect = quizPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.2f, 0.15f);
                rect.anchorMax = new Vector2(0.8f, 0.85f);
                rect.sizeDelta = Vector2.zero;

                quizPanel.SetActive(false);
                Undo.RegisterCreatedObjectUndo(quizPanel, "Create Quiz Panel");
            }            // Quiz Progress Text
            TMP_Text progressText = null;
            Transform progressTrans = FindChildRecursive(quizPanel.transform, "ProgressText");
            if (progressTrans != null)
            {
                progressText = progressTrans.GetComponent<TMP_Text>();
                progressText.color = new Color(0.6f, 0.65f, 0.72f); // Soft gray
            }
            else
            {
                GameObject go = new GameObject("ProgressText");
                go.transform.SetParent(quizPanel.transform, false);
                progressText = go.AddComponent<TextMeshProUGUI>();
                progressText.fontSize = 16;
                progressText.color = new Color(0.6f, 0.65f, 0.72f); // Soft gray
                progressText.alignment = TextAlignmentOptions.TopLeft;
                progressText.text = "Question 1 of 5";

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.88f);
                rect.anchorMax = new Vector2(0.95f, 0.95f);
                rect.sizeDelta = Vector2.zero;
            }

            // Quiz Question Text
            TMP_Text questionText = null;
            Transform questionTrans = FindChildRecursive(quizPanel.transform, "QuestionText");
            if (questionTrans != null)
            {
                questionText = questionTrans.GetComponent<TMP_Text>();
                questionText.color = Color.white;
            }
            else
            {
                GameObject go = new GameObject("QuestionText");
                go.transform.SetParent(quizPanel.transform, false);
                questionText = go.AddComponent<TextMeshProUGUI>();
                questionText.fontSize = 22;
                questionText.color = Color.white;
                questionText.fontStyle = FontStyles.Bold;
                questionText.alignment = TextAlignmentOptions.TopLeft;
                questionText.text = "Question Text Placeholder";

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.68f);
                rect.anchorMax = new Vector2(0.95f, 0.85f);
                rect.sizeDelta = Vector2.zero;
            }

            // Create OptionButtonsGroup VerticalLayoutGroup container
            Transform optGroupTrans = FindChildRecursive(quizPanel.transform, "OptionButtonsGroup");
            GameObject optGroupObj = null;
            if (optGroupTrans != null)
            {
                optGroupObj = optGroupTrans.gameObject;
            }
            else
            {
                optGroupObj = new GameObject("OptionButtonsGroup");
                optGroupObj.transform.SetParent(quizPanel.transform, false);

                RectTransform rect = optGroupObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.16f);
                rect.anchorMax = new Vector2(0.95f, 0.62f);
                rect.sizeDelta = Vector2.zero;

                VerticalLayoutGroup layout = optGroupObj.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                Undo.RegisterCreatedObjectUndo(optGroupObj, "Create OptionButtonsGroup");
            }

            // Quiz 4 Option Buttons & Text Labels
            Button[] answerButtons = new Button[4];
            TMP_Text[] answerTexts = new TMP_Text[4];
            string[] optionNames = { "Btn_OptionA", "Btn_OptionB", "Btn_OptionC", "Btn_OptionD" };
            for (int i = 0; i < 4; i++)
            {
                Transform btnTrans = quizPanel.transform.Find(optionNames[i]);
                if (btnTrans == null && optGroupObj != null)
                {
                    btnTrans = optGroupObj.transform.Find(optionNames[i]);
                }

                if (btnTrans != null)
                {
                    answerButtons[i] = btnTrans.GetComponent<Button>();
                    answerTexts[i] = btnTrans.GetComponentInChildren<TMP_Text>();
                    btnTrans.SetParent(optGroupObj.transform, false);
                }
                else
                {
                    GameObject btnObj = new GameObject(optionNames[i]);
                    btnObj.transform.SetParent(optGroupObj.transform, false);
                    Image img = btnObj.AddComponent<Image>();
                    img.color = new Color(0.12f, 0.14f, 0.19f, 1f); // Premium Slate #1F2430
                    answerButtons[i] = btnObj.AddComponent<Button>();

                    GameObject btnTxtObj = new GameObject("Text");
                    btnTxtObj.transform.SetParent(btnObj.transform, false);
                    answerTexts[i] = btnTxtObj.AddComponent<TextMeshProUGUI>();
                    answerTexts[i].color = Color.white;
                    answerTexts[i].fontSize = 16;
                    answerTexts[i].alignment = TextAlignmentOptions.MidlineLeft;
                    answerTexts[i].text = $"{(char)('A' + i)}. Option";

                    RectTransform rect = btnObj.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(0f, 48f);

                    RectTransform txtRect = btnTxtObj.GetComponent<RectTransform>();
                    txtRect.anchorMin = new Vector2(0.03f, 0f);
                    txtRect.anchorMax = new Vector2(0.97f, 1f);
                    txtRect.sizeDelta = Vector2.zero;

                    LayoutElement le = btnObj.AddComponent<LayoutElement>();
                    le.preferredHeight = 48f;

                    Undo.RegisterCreatedObjectUndo(btnObj, "Create Quiz Button " + i);
                }

                // Ensure styling is active on existing/new buttons
                if (answerButtons[i] != null)
                {
                    LayoutElement le = answerButtons[i].GetComponent<LayoutElement>();
                    if (le == null) le = answerButtons[i].gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 48f;

                    Image img = answerButtons[i].GetComponent<Image>();
                    if (img != null) img.color = new Color(0.12f, 0.14f, 0.19f, 1f);

                    FormatButtonText(answerButtons[i], null, TextAlignmentOptions.MidlineLeft);
                }
            }

            // Quiz Next Button
            Button nextButton = null;
            Transform nextTrans = FindChildRecursive(quizPanel.transform, "Btn_Next");
            if (nextTrans != null)
            {
                nextButton = nextTrans.GetComponent<Button>();
                FormatButtonText(nextButton, "Next ➔");
            }
            else
            {
                GameObject go = new GameObject("Btn_Next");
                go.transform.SetParent(quizPanel.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.18f, 0.49f, 0.96f, 1f); // Accent Blue #2E7DF6
                nextButton = go.AddComponent<Button>();

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(go.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = "Next ➔";
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 18;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.72f, 0.05f);
                rect.anchorMax = new Vector2(0.95f, 0.14f);
                rect.sizeDelta = Vector2.zero;

                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(go, "Create Quiz Next Button");
            }

            // 8. Setup Results Panel UI
            Transform resultsTrans = FindChildRecursive(canvasObj.transform, "ResultsPanel");
            GameObject resultsPanel = null;
            if (resultsTrans != null)
            {
                resultsPanel = resultsTrans.gameObject;
            }
            else
            {
                resultsPanel = new GameObject("ResultsPanel");
                resultsPanel.transform.SetParent(canvasObj.transform, false);
                Image img = resultsPanel.AddComponent<Image>();
                img.color = new Color(0.06f, 0.08f, 0.12f, 0.95f); // Glassmorphism Dark Slate

                RectTransform rect = resultsPanel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.25f, 0.2f);
                rect.anchorMax = new Vector2(0.75f, 0.8f); // Center modal
                rect.sizeDelta = Vector2.zero;

                resultsPanel.SetActive(false);
                Undo.RegisterCreatedObjectUndo(resultsPanel, "Create Results Panel");
            }

            // Results Header Text
            Transform resHeaderTrans = FindChildRecursive(resultsPanel.transform, "Header");
            if (resHeaderTrans == null)
            {
                GameObject go = new GameObject("Header");
                go.transform.SetParent(resultsPanel.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = "RESULT";
                txt.fontSize = 28;
                txt.color = new Color(1f, 0.84f, 0f); // Gold #FFD700
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontStyle = FontStyles.Bold;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.8f);
                rect.anchorMax = new Vector2(0.95f, 0.95f);
                rect.sizeDelta = Vector2.zero;
            }

            // Results Score Text
            TMP_Text scoreText = null;
            Transform scoreTrans = FindChildRecursive(resultsPanel.transform, "ScoreText");
            if (scoreTrans != null)
            {
                scoreText = scoreTrans.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject go = new GameObject("ScoreText");
                go.transform.SetParent(resultsPanel.transform, false);
                scoreText = go.AddComponent<TextMeshProUGUI>();
                scoreText.fontSize = 24;
                scoreText.color = new Color(0.96f, 0.65f, 0.14f); // Orange Gold #F5A623
                scoreText.alignment = TextAlignmentOptions.Center;
                scoreText.text = "You scored 0 out of 5";

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.6f);
                rect.anchorMax = new Vector2(0.95f, 0.75f);
                rect.sizeDelta = Vector2.zero;
            }

            // Results Answer Key Text
            TMP_Text correctAnswersText = null;
            Transform keyTrans = FindChildRecursive(resultsPanel.transform, "CorrectAnswersText");
            if (keyTrans != null)
            {
                correctAnswersText = keyTrans.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject labelGo = new GameObject("KeyLabel");
                labelGo.transform.SetParent(resultsPanel.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "CORRECT ANSWERS:";
                label.fontSize = 16;
                label.color = Color.grey;
                label.alignment = TextAlignmentOptions.Center;

                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.05f, 0.42f);
                labelRect.anchorMax = new Vector2(0.95f, 0.52f);
                labelRect.sizeDelta = Vector2.zero;

                GameObject go = new GameObject("CorrectAnswersText");
                go.transform.SetParent(resultsPanel.transform, false);
                correctAnswersText = go.AddComponent<TextMeshProUGUI>();
                correctAnswersText.fontSize = 20;
                correctAnswersText.alignment = TextAlignmentOptions.Center;
                correctAnswersText.text = "1.B   2.A   3.B   4.C   5.C";

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.3f);
                rect.anchorMax = new Vector2(0.95f, 0.42f);
                rect.sizeDelta = Vector2.zero;
            }

            // Results Continue/Review Button
            Button reviewBtn = null;
            Transform reviewTrans = FindChildRecursive(resultsPanel.transform, "Btn_Review");
            if (reviewTrans != null)
            {
                reviewBtn = reviewTrans.GetComponent<Button>();
                FormatButtonText(reviewBtn, "Review & Continue ➔");
            }
            else
            {
                GameObject go = new GameObject("Btn_Review");
                go.transform.SetParent(resultsPanel.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.18f, 0.8f, 0.44f); // Emerald Green #2ECC71
                reviewBtn = go.AddComponent<Button>();

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(go.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = "Review & Continue ➔";
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 16;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.25f, 0.08f);
                rect.anchorMax = new Vector2(0.75f, 0.18f);
                rect.sizeDelta = Vector2.zero;

                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(go, "Create Review Continue Button");
            }

            // 9. Setup Thank You Panel
            Transform thankYouTrans = canvasObj.transform.Find("ThankYouPanel");
            GameObject thankYouPanel = null;
            if (thankYouTrans != null)
            {
                thankYouPanel = thankYouTrans.gameObject;
            }
            else
            {
                thankYouPanel = new GameObject("ThankYouPanel");
                thankYouPanel.transform.SetParent(canvasObj.transform, false);
                Image img = thankYouPanel.AddComponent<Image>();
                img.color = new Color(0.02f, 0.02f, 0.03f, 0.85f); // Sleek dark glass overlay

                RectTransform rect = thankYouPanel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one; // Full-screen overlay
                rect.sizeDelta = Vector2.zero;

                thankYouPanel.AddComponent<CanvasGroup>();
                thankYouPanel.SetActive(false);
                Undo.RegisterCreatedObjectUndo(thankYouPanel, "Create Thank You Panel");
            }

            // Inside ThankYouPanel, Centered Dialogue Box
            Transform centerBoxTrans = FindChildRecursive(thankYouPanel.transform, "CenterBox");
            GameObject centerBox = null;
            if (centerBoxTrans != null)
            {
                centerBox = centerBoxTrans.gameObject;
            }
            else
            {
                centerBox = new GameObject("CenterBox");
                centerBox.transform.SetParent(thankYouPanel.transform, false);
                Image img = centerBox.AddComponent<Image>();
                img.color = new Color(0.06f, 0.08f, 0.12f, 0.98f); // Glassmorphism Dark Slate

                RectTransform rect = centerBox.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.28f, 0.22f);
                rect.anchorMax = new Vector2(0.72f, 0.78f);
                rect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(centerBox, "Create Center Dialogue Box");
            }

            // Center Box elements
            Transform iconTrans = FindChildRecursive(centerBox.transform, "Icon");
            if (iconTrans == null)
            {
                GameObject go = new GameObject("Icon");
                go.transform.SetParent(centerBox.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = "🏛️";
                txt.fontSize = 55;
                txt.alignment = TextAlignmentOptions.Center;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.72f);
                rect.anchorMax = new Vector2(0.95f, 0.92f);
                rect.sizeDelta = Vector2.zero;
            }

            Transform tyTitleTrans = FindChildRecursive(centerBox.transform, "Title");
            if (tyTitleTrans == null)
            {
                GameObject go = new GameObject("Title");
                go.transform.SetParent(centerBox.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = "Thank You!";
                txt.fontSize = 42;
                txt.color = new Color(1f, 0.84f, 0f); // Gold #FFD700
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontStyle = FontStyles.Bold;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.5f);
                rect.anchorMax = new Vector2(0.95f, 0.72f);
                rect.sizeDelta = Vector2.zero;
            }

            Transform tyBodyTrans = FindChildRecursive(centerBox.transform, "Body");
            if (tyBodyTrans == null)
            {
                GameObject go = new GameObject("Body");
                go.transform.SetParent(centerBox.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = "Thank you for visiting the Tomb of the Servilii.\nWe hope you enjoyed this virtual tour.";
                txt.fontSize = 18;
                txt.alignment = TextAlignmentOptions.Center;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.28f);
                rect.anchorMax = new Vector2(0.95f, 0.48f);
                rect.sizeDelta = Vector2.zero;
            }

            // Buttons: Exit & Restart side-by-side
            Button exitBtn = null;
            Transform exitTrans = FindChildRecursive(centerBox.transform, "Btn_Exit");
            if (exitTrans != null)
            {
                exitBtn = exitTrans.GetComponent<Button>();
                FormatButtonText(exitBtn, "Exit");
            }
            else
            {
                GameObject go = new GameObject("Btn_Exit");
                go.transform.SetParent(centerBox.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.18f, 0.2f, 0.26f, 1f); // Dark Charcoal Slate
                exitBtn = go.AddComponent<Button>();

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(go.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = "Exit";
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 16;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.15f, 0.1f);
                rect.anchorMax = new Vector2(0.45f, 0.22f);
                rect.sizeDelta = Vector2.zero;

                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(go, "Create Exit Button");
            }

            Button restartBtn = null;
            Transform restartTrans = FindChildRecursive(centerBox.transform, "Btn_Restart");
            if (restartTrans != null)
            {
                restartBtn = restartTrans.GetComponent<Button>();
                FormatButtonText(restartBtn, "Restart");
            }
            else
            {
                GameObject go = new GameObject("Btn_Restart");
                go.transform.SetParent(centerBox.transform, false);
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.18f, 0.49f, 0.96f, 1f); // Accent Blue #2E7DF6
                restartBtn = go.AddComponent<Button>();

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(go.transform, false);
                var txt = txtObj.AddComponent<TextMeshProUGUI>();
                txt.text = "Restart";
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize = 16;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.55f, 0.1f);
                rect.anchorMax = new Vector2(0.85f, 0.22f);
                rect.sizeDelta = Vector2.zero;

                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Undo.RegisterCreatedObjectUndo(go, "Create Restart Button");
            }

            // Footer Text on Overlay Panel (outside dialog box)
            Transform footerTrans = thankYouPanel.transform.Find("FooterText");
            if (footerTrans == null)
            {
                GameObject go = new GameObject("FooterText");
                go.transform.SetParent(thankYouPanel.transform, false);
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = "Virtual Tour — Tomb of the Servilii | Educational Project";
                txt.fontSize = 12;
                txt.color = Color.grey;
                txt.alignment = TextAlignmentOptions.Center;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.02f);
                rect.anchorMax = new Vector2(0.9f, 0.08f);
                rect.sizeDelta = Vector2.zero;
            }

            // 10. Create QuizManager GameObject
            QuizManager quizManager = GameObject.FindObjectOfType<QuizManager>();
            if (quizManager == null)
            {
                GameObject qmObj = new GameObject("QuizManager");
                quizManager = qmObj.AddComponent<QuizManager>();
                Undo.RegisterCreatedObjectUndo(qmObj, "Create QuizManager");
            }

            var serializedQM = new SerializedObject(quizManager);
            serializedQM.FindProperty("quizPanel").objectReferenceValue = quizPanel;
            serializedQM.FindProperty("resultsPanel").objectReferenceValue = resultsPanel;
            serializedQM.FindProperty("thankYouPanel").objectReferenceValue = thankYouPanel;
            serializedQM.FindProperty("progressText").objectReferenceValue = progressText;
            serializedQM.FindProperty("questionText").objectReferenceValue = questionText;
            serializedQM.FindProperty("nextButton").objectReferenceValue = nextButton;
            serializedQM.FindProperty("scoreText").objectReferenceValue = scoreText;
            serializedQM.FindProperty("correctAnswersText").objectReferenceValue = correctAnswersText;
            serializedQM.FindProperty("defaultButtonColor").colorValue = new Color(0.12f, 0.14f, 0.19f, 1f); // #1F2430 Slate

            // Link options array
            var arrButtonsProp = serializedQM.FindProperty("answerButtons");
            var arrTextsProp = serializedQM.FindProperty("answerTexts");
            for (int i = 0; i < 4; i++)
            {
                arrButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = answerButtons[i];
                arrTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = answerTexts[i];
            }

            // Auto-bind Quiz Questions if present in Project Assets
            var questionsProp = serializedQM.FindProperty("questions");
            string[] questionFiles = { "Q1", "Q2", "Q3", "Q4", "Q5" };
            for (int i = 0; i < 5; i++)
            {
                if (questionsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:QuestionData " + questionFiles[i]);
                    if (guids.Length == 0)
                    {
                        guids = AssetDatabase.FindAssets("t:QuestionData Q" + (i + 1));
                    }
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        questionsProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<QuestionData>(path);
                    }
                }
            }
            serializedQM.ApplyModifiedProperties();

            // Link results panel continue to QuizManager persistently
            ClearPersistentListeners(reviewBtn);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(reviewBtn.onClick, quizManager.OnReviewClicked);
            EditorUtility.SetDirty(reviewBtn);

            // 11. Create ThankYouManager GameObject
            ThankYouManager thankYouManager = GameObject.FindObjectOfType<ThankYouManager>();
            if (thankYouManager == null)
            {
                GameObject tyObj = new GameObject("ThankYouManager");
                thankYouManager = tyObj.AddComponent<ThankYouManager>();
                Undo.RegisterCreatedObjectUndo(tyObj, "Create ThankYouManager");
            }

            var serializedTYM = new SerializedObject(thankYouManager);
            serializedTYM.FindProperty("thankYouPanel").objectReferenceValue = thankYouPanel;
            serializedTYM.FindProperty("thankYouCanvasGroup").objectReferenceValue = thankYouPanel.GetComponent<CanvasGroup>();
            serializedTYM.ApplyModifiedProperties();

            // Link ThankYouManager buttons persistently
            ClearPersistentListeners(exitBtn);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(exitBtn.onClick, thankYouManager.OnExitClicked);
            EditorUtility.SetDirty(exitBtn);

            ClearPersistentListeners(restartBtn);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(restartBtn.onClick, thankYouManager.OnRestartClicked);
            EditorUtility.SetDirty(restartBtn);

            // 11.b Create Managers Parent & GameManager & AudioManager
            GameObject managersObj = GameObject.Find("Managers");
            if (managersObj == null)
            {
                managersObj = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managersObj, "Create Managers Parent");
            }

            AudioManager audioManager = managersObj.GetComponent<AudioManager>();
            if (audioManager == null)
            {
                audioManager = managersObj.AddComponent<AudioManager>();
            }

            GameManager gameManager = managersObj.GetComponent<GameManager>();
            if (gameManager == null)
            {
                gameManager = managersObj.AddComponent<GameManager>();
            }

            // Bind GameManager references
            var serializedGM = new SerializedObject(gameManager);
            CharacterController ccObj = GameObject.FindObjectOfType<CharacterController>();
            if (ccObj != null)
            {
                serializedGM.FindProperty("playerRoot").objectReferenceValue = ccObj.gameObject;
            }
            else
            {
                serializedGM.FindProperty("playerRoot").objectReferenceValue = mainCam.transform.parent != null ? mainCam.transform.parent.gameObject : mainCam.gameObject;
            }
            serializedGM.FindProperty("handAnim").objectReferenceValue = handAnimCtrl;
            serializedGM.FindProperty("infoMenuPanel").objectReferenceValue = infoMenuPanel;
            serializedGM.FindProperty("quizPanel").objectReferenceValue = quizPanel;
            serializedGM.FindProperty("resultsPanel").objectReferenceValue = resultsPanel;
            serializedGM.FindProperty("thankYouPanel").objectReferenceValue = thankYouPanel;
            serializedGM.ApplyModifiedProperties();

            // Bind AudioManager clips automatically if present in Project Assets
            var serializedAM = new SerializedObject(audioManager);
            if (serializedAM.FindProperty("welcomeVoice").objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioClip Welcome");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    serializedAM.FindProperty("welcomeVoice").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            if (serializedAM.FindProperty("gateSFX").objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioClip creak");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    serializedAM.FindProperty("gateSFX").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            if (serializedAM.FindProperty("ambientWind").objectReferenceValue == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:AudioClip wind");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    serializedAM.FindProperty("ambientWind").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            var narrationsProp = serializedAM.FindProperty("topicNarrations");
            string[] topicFiles = { "What it is", "Who built", "Why built", "Design", "History" };
            for (int i = 0; i < 5; i++)
            {
                if (narrationsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:AudioClip " + topicFiles[i]);
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        narrationsProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
            }
            serializedAM.ApplyModifiedProperties();

            // Link references to InfoMenuManager
            var serializedIMM = new SerializedObject(infoMenuManager);
            serializedIMM.FindProperty("menuPanel").objectReferenceValue = infoMenuPanel;
            serializedIMM.FindProperty("quizPanel").objectReferenceValue = quizPanel;
            serializedIMM.FindProperty("startQuizButton").objectReferenceValue = startQuizBtn;
            serializedIMM.FindProperty("contentTextWindow").objectReferenceValue = contentText;
            serializedIMM.FindProperty("titleTextWindow").objectReferenceValue = titleText;
            serializedIMM.FindProperty("narrationAudioSource").objectReferenceValue = infoMenuManager.GetComponent<AudioSource>();

            // Auto-bind Topics if present in Project Assets
            var topicsProp = serializedIMM.FindProperty("topics");
            string[] topicAssetNames = { "WhatIsIt", "WhoBuilt", "WhyBuilt", "Design", "History" };
            for (int i = 0; i < 5; i++)
            {
                if (topicsProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:TopicData " + topicAssetNames[i]);
                    if (guids.Length == 0)
                    {
                        guids = AssetDatabase.FindAssets("t:TopicData Topic_" + topicAssetNames[i]);
                    }
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        topicsProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<TopicData>(path);
                    }
                }
            }
            serializedIMM.ApplyModifiedProperties();

            // 12. Find Gate in scene and setup
            GameObject gateObj = GameObject.Find("Gate");
            if (gateObj == null)
            {
                gateObj = GameObject.FindWithTag("Gate");
            }
            if (gateObj == null)
            {
                foreach (var go in GameObject.FindObjectsOfType<GameObject>())
                {
                    if (go.name.ToLower().Contains("gate"))
                    {
                        gateObj = go;
                        break;
                    }
                }
            }

            if (gateObj != null)
            {
                gateObj.tag = "Gate";
                Collider existingCollider = gateObj.GetComponentInChildren<Collider>();
                if (existingCollider == null)
                {
                    BoxCollider box = gateObj.AddComponent<BoxCollider>();
                    box.size = new Vector3(3f, 3f, 0.5f);
                    box.center = new Vector3(0f, 1.5f, 0f);
                    Debug.Log("SceneSetupHelper: Added a default BoxCollider to Gate because no collider was found on it or its children.");
                }

                GateInteraction gateInt = gateObj.GetComponent<GateInteraction>();
                if (gateInt == null)
                {
                    gateInt = gateObj.AddComponent<GateInteraction>();
                }

                AudioSource sfxCreak = GetOrCreateAudioSource(gateObj, "SFX_Creak");
                AudioSource narrationWelcome = GetOrCreateAudioSource(gateObj, "Narration_Welcome");
                AudioSource ambientWind = GetOrCreateAudioSource(gateObj, "Ambient_Wind");

                Transform leftDoorTrans = null;
                Transform rightDoorTrans = null;

                if (gateObj.transform.childCount >= 2)
                {
                    System.Collections.Generic.List<Transform> validChildren = new System.Collections.Generic.List<Transform>();
                    for (int i = 0; i < gateObj.transform.childCount; i++)
                    {
                        Transform child = gateObj.transform.GetChild(i);
                        string childName = child.name.ToLower();
                        if (childName.Contains("sfx") || childName.Contains("narration") || childName.Contains("ambient"))
                        {
                            continue;
                        }
                        validChildren.Add(child);
                    }

                    // Scan valid children for L/R identifiers
                    foreach (var child in validChildren)
                    {
                        string childName = child.name.ToLower();
                        if (childName.Contains("left") || childName.Contains("_l"))
                        {
                            leftDoorTrans = child;
                        }
                        else if (childName.Contains("right") || childName.Contains("_r"))
                        {
                            rightDoorTrans = child;
                        }
                    }

                    // Fallback to first two children if names don't explicitly contain L/R
                    if (leftDoorTrans == null && rightDoorTrans == null && validChildren.Count >= 2)
                    {
                        leftDoorTrans = validChildren[0];
                        rightDoorTrans = validChildren[1];
                    }
                }

                var serializedGate = new SerializedObject(gateInt);
                if (leftDoorTrans != null && rightDoorTrans != null)
                {
                    serializedGate.FindProperty("doorTransform").objectReferenceValue = leftDoorTrans;
                    serializedGate.FindProperty("secondaryDoorTransform").objectReferenceValue = rightDoorTrans;
                    // Left door swings +90, Right door swings -90
                    serializedGate.FindProperty("openRotationOffset").vector3Value = new Vector3(0f, 90f, 0f);
                    serializedGate.FindProperty("secondaryOpenRotationOffset").vector3Value = new Vector3(0f, -90f, 0f);
                }
                else
                {
                    serializedGate.FindProperty("doorTransform").objectReferenceValue = gateObj.transform;
                    serializedGate.FindProperty("secondaryDoorTransform").objectReferenceValue = null;
                }

                serializedGate.FindProperty("creakAudioSource").objectReferenceValue = sfxCreak;
                serializedGate.FindProperty("narrationAudioSource").objectReferenceValue = narrationWelcome;
                serializedGate.FindProperty("ambientAudioSource").objectReferenceValue = ambientWind;
                serializedGate.ApplyModifiedProperties();

                Debug.Log($"SceneSetupHelper: Set up Gate on GameObject: '{gateObj.name}'");
            }

            // 13. Setup Info Trigger Zone
            GameObject triggerObj = GameObject.Find("InfoTriggerZone");
            if (triggerObj == null)
            {
                triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                triggerObj.name = "InfoTriggerZone";
                triggerObj.transform.position = new Vector3(0f, 1f, 8f);
                triggerObj.transform.localScale = new Vector3(4f, 3f, 1f);
                
                triggerObj.GetComponent<Collider>().isTrigger = true;

                DestroyImmediate(triggerObj.GetComponent<MeshRenderer>());
                DestroyImmediate(triggerObj.GetComponent<MeshFilter>());

                InfoTriggerZone trigger = triggerObj.AddComponent<InfoTriggerZone>();
                
                var serializedTrigger = new SerializedObject(trigger);
                serializedTrigger.FindProperty("infoMenuManager").objectReferenceValue = infoMenuManager;
                serializedTrigger.ApplyModifiedProperties();

                Undo.RegisterCreatedObjectUndo(triggerObj, "Create InfoTriggerZone");
            }

            Debug.Log("SceneSetupHelper: Automated setup finished successfully! All changes have been registered to Unity's Undo stack.");
        }

        private static AudioSource GetOrCreateAudioSource(GameObject parent, string childName)
        {
            Transform childTrans = parent.transform.Find(childName);
            if (childTrans != null)
            {
                return childTrans.GetComponent<AudioSource>();
            }

            GameObject childObj = new GameObject(childName);
            childObj.transform.SetParent(parent.transform);
            childObj.transform.localPosition = Vector3.zero;
            childObj.transform.localRotation = Quaternion.identity;
            
            AudioSource src = childObj.AddComponent<AudioSource>();
            src.playOnAwake = false;
            if (childName.Contains("Ambient"))
            {
                src.loop = true;
            }

            return src;
        }

        private static void CreateDefaultDataAssets()
        {
            // Ensure directories exist
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Data/Topics"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Topics");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Data/Quiz"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Quiz");
            }

            // 1. Create Topics
            string[] topicFiles = { "Topic_WhatIsIt", "Topic_WhoBuiltIt", "Topic_WhyBuiltIt", "Topic_Design", "Topic_History" };
            string[] topicTitles = {
                "What is it?",
                "Who built it?",
                "Why was it built?",
                "Architectural Design",
                "Historical Importance"
            };
            string[] topicContents = {
                "The Tomb of the Servilii is a historic monument located along the ancient Via Appia in Rome. It represents a typical Roman aristocratic family tomb, constructed during the late Republic and early Empire to house the remains of the distinguished Servilia family.",
                "The monument was commissioned by the Servilia family (Gens Servilia), one of the most prominent patrician clans of ancient Rome. Members of this family served as consuls, generals, and senators, holding significant power throughout Roman history.",
                "Like many Roman tombs along major consular roads, it was built outside the city walls (due to Roman law) to preserve the memory of the deceased, showcase the family's patrician status, wealth, and civic contributions to travelers entering Rome.",
                "The tomb is characterized by a concrete core faced with high-quality travertine stone blocks. It sits on a large rectangular podium, originally decorated with relief carvings, inscriptions, and ornamental friezes typical of classical Roman funerary architecture.",
                "Standing along the Via Appia, the tomb has witnessed centuries of Roman history. Today, its ruins offer modern archaeologists vital insights into Roman engineering, burial laws, artistic customs, and the social stratification of Roman nobility."
            };
            string[] topicSubtitles = {
                "The Tomb of the Servilii is a historic monument located along the ancient Via Appia in Rome.",
                "The monument was commissioned by the Servilia family, one of the most prominent clans of Rome.",
                "It was built outside the city walls to preserve memory and showcase the family's patrician status.",
                "The tomb is made of a concrete core faced with high-quality travertine stone blocks.",
                "Standing along the Via Appia, the tomb offers insights into Roman engineering and customs."
            };

            for (int i = 0; i < topicFiles.Length; i++)
            {
                string path = $"Assets/Data/Topics/{topicFiles[i]}.asset";
                TopicData topic = AssetDatabase.LoadAssetAtPath<TopicData>(path);
                if (topic == null)
                {
                    topic = ScriptableObject.CreateInstance<TopicData>();
                    AssetDatabase.CreateAsset(topic, path);
                }
                topic.topicTitle = topicTitles[i];
                topic.contentText = topicContents[i];
                topic.subtitleText = topicSubtitles[i];
                EditorUtility.SetDirty(topic);
            }

            // 2. Create Questions
            string[] qFiles = { "Q1_WhatIsIt", "Q2_Location", "Q3_Family", "Q4_Material", "Q5_Purpose" };
            string[] qTexts = {
                "What is the Tomb of the Servilii?",
                "Where is the tomb located?",
                "Which family built this tomb?",
                "What material faced the exterior of the tomb?",
                "Why was the tomb built outside the city walls?"
            };
            string[][] qOptions = {
                new string[] { "A Roman temple", "A Roman family tomb", "A Roman market", "A Roman theater" },
                new string[] { "Via Flaminia", "Via Appia", "Via Aurelia", "Via Ostiense" },
                new string[] { "The Julii", "The Servilii", "The Claudii", "The Cornelii" },
                new string[] { "Marble", "Granite", "Travertine", "Brick" },
                new string[] { "To store treasure", "As a place of worship", "To honor the family and display status", "Military monument" }
            };
            int[] qCorrectIndices = { 1, 1, 1, 2, 2 }; // B, B, B, C, C

            for (int i = 0; i < qFiles.Length; i++)
            {
                string path = $"Assets/Data/Quiz/{qFiles[i]}.asset";
                QuestionData q = AssetDatabase.LoadAssetAtPath<QuestionData>(path);
                if (q == null)
                {
                    q = ScriptableObject.CreateInstance<QuestionData>();
                    AssetDatabase.CreateAsset(q, path);
                }
                q.questionText = qTexts[i];
                q.options = qOptions[i];
                q.correctAnswerIndex = qCorrectIndices[i];
                EditorUtility.SetDirty(q);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ClearPersistentListeners(Button button)
        {
            if (button == null) return;
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void FormatButtonText(Button button, string text, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            if (button == null) return;
            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    tmp.text = text;
                }
                tmp.color = Color.white;
                tmp.alignment = alignment;
                tmp.fontSize = 16;
                tmp.fontStyle = FontStyles.Bold;

                RectTransform txtRect = tmp.rectTransform;
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.offsetMin = new Vector2(15f, 0f);
                txtRect.offsetMax = new Vector2(-15f, 0f);
            }
        }
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MapModS.UI
{
    // All the following was modified from the GUI implementation of BenchwarpMod by homothetyhk
    public class GUIController : MonoBehaviour
    {
        public Dictionary<string, Texture2D> Images = new();

        public static GUIController Instance;

        private GameObject _pauseCanvas;
        private GameObject _mapCanvas;
        private GameObject _transitionCanvas;
        private GameObject _lookupCanvas;
        public Font TrajanBold { get; private set; }
        public Font TrajanNormal { get; private set; }
        public Font Perpetua { get; private set; }
        private Font Arial { get; set; }

        public static void Setup()
        {
            GameObject GUIObj = new("MapModS GUI");
            Instance = GUIObj.AddComponent<GUIController>();
            DontDestroyOnLoad(GUIObj);
            Instance.LoadResources();
        }

        public static void Unload()
        {
            if (Instance != null)
            {
                Instance.StopAllCoroutines();

                Destroy(Instance._pauseCanvas);
                Destroy(Instance._mapCanvas);
                Destroy(Instance._transitionCanvas);
                Destroy(Instance._lookupCanvas);
                Destroy(Instance.gameObject);
            }
        }

        public void BuildMenus()
        {
            _transitionCanvas = new GameObject();
            _transitionCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler transitionScaler = _transitionCanvas.AddComponent<CanvasScaler>();
            transitionScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            transitionScaler.referenceResolution = new Vector2(1920f, 1080f);

            TransitionText.BuildText(_transitionCanvas);

            DontDestroyOnLoad(_transitionCanvas);

            _transitionCanvas.SetActive(true);

            TransitionText.Initialize();
            StartCoroutine("UpdateSelectedScene");

            _lookupCanvas = new GameObject();
            _lookupCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler lookupScaler = _lookupCanvas.AddComponent<CanvasScaler>();
            lookupScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            lookupScaler.referenceResolution = new Vector2(1920f, 1080f);

            LookupText.BuildText(_lookupCanvas);

            DontDestroyOnLoad(_lookupCanvas);

            _lookupCanvas.SetActive(false);

            LookupText.Initialize();
            StartCoroutine("UpdateSelectedPin");
        }

        public void Update()
        {
            try
            {
                //PauseMenu.Update();
                TransitionText.Update();
                LookupText.Update();
            }
            catch (Exception e)
            {
                MapModS.Instance.LogError(e);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Member is actually used")]
        IEnumerator UpdateSelectedScene()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                TransitionText.UpdateSelectedScene();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Member is actually used")]
        IEnumerator UpdateSelectedPin()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                LookupText.UpdateSelectedPinCoroutine();
            }
        }

        private void LoadResources()
        {
            TrajanBold = Modding.CanvasUtil.TrajanBold;
            TrajanNormal = Modding.CanvasUtil.TrajanNormal;
            Perpetua = Modding.CanvasUtil.GetFont("Perpetua");

            try
            {
                Arial = Font.CreateDynamicFontFromOSFont
                (
                    Font.GetOSInstalledFontNames().First(x => x.ToLower().Contains("arial")),
                    13
                );
            }
            catch
            {
                MapModS.Instance.LogWarn("Unable to find Arial! Using Perpetua.");
                Arial = Modding.CanvasUtil.GetFont("Perpetua");
            }

            if (TrajanBold == null || TrajanNormal == null || Arial == null)
            {
                MapModS.Instance.LogError("Could not find game fonts");
            }

            Assembly asm = Assembly.GetExecutingAssembly();

            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.StartsWith("MapModS.Resources.GUI.")) continue;

                try
                {
                    using Stream imageStream = asm.GetManifestResourceStream(res);
                    byte[] buffer = new byte[imageStream.Length];
                    imageStream.Read(buffer, 0, buffer.Length);

                    Texture2D tex = new(1, 1);
                    tex.LoadImage(buffer.ToArray());

                    string[] split = res.Split('.');
                    string internalName = split[split.Length - 2];

                    Images.Add(internalName, tex);
                }
                catch (Exception e)
                {
                    MapModS.Instance.LogError("Failed to load image: " + res + "\n" + e);
                }
            }
        }
    }
}
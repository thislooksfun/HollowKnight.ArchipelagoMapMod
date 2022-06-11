﻿using MagicUI.Core;
using MagicUI.Elements;
using MapModS.Data;
using MapModS.Map;
using MapModS.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MapModS.UI
{
    internal class TransitionPersistent
    {
        private static LayoutRoot layout;

        private static TextObject route;

        public static Pathfinder pf;

        public static string lastStartScene = "";
        public static string lastFinalScene = "";
        public static string lastStartTransition = "";
        public static string lastFinalTransition = "";
        public static int transitionsCount = 0;
        public static string selectedScene = "None";
        public static List<string> selectedRoute = new();
        public static List<List<string>> rejectedRoutes = new();

        private static bool Condition()
        {
            return TransitionData.TransitionModeActive()
                && !GUI.lockToggleEnable
                && (GUI.worldMapOpen
                    || GUI.quickMapOpen
                    || (!GameManager.instance.IsGamePaused()
                        && (MapModS.GS.routeTextInGame == RouteTextInGame.ShowNextTransitionOnly
                            || MapModS.GS.routeTextInGame == RouteTextInGame.Show)));
        }

        public static void Build()
        {
            if (layout == null)
            {
                layout = new(true, "Transition Persistent");
                layout.VisibilityCondition = Condition;

                route = UIExtensions.TextFromEdge(layout, "Route", false);

                UpdateAll();
            }

            pf = new();
        }

        public static void Destroy()
        {
            layout?.Destroy();
            layout = null;

            ResetRoute();
        }

        public static void UpdateAll()
        {
            UpdateRoute();
        }

        public static void UpdateRoute()
        {
            string text = "\n";

            if (GUI.worldMapOpen)
            {
                text += "\n";
            }

            if (selectedRoute.Any())
            {
                if (MapModS.GS.routeTextInGame == RouteTextInGame.ShowNextTransitionOnly
                    && !GUI.quickMapOpen && !GUI.worldMapOpen)
                {
                    text += " -> " + selectedRoute.First().ToCleanName();
                }
                else
                {
                    foreach (string transition in selectedRoute)
                    {
                        if (text.Length > 128)
                        {
                            text += " -> ... -> " + selectedRoute.Last().ToCleanName();
                            break;
                        }

                        text += " -> " + transition.ToCleanName();
                    }
                }
            }

            route.Text = text;
        }

        private static Thread colorUpdateThread;

        // Called every 0.1 seconds
        public static void UpdateSelectedScene()
        {
            if (layout == null
                || !GUI.worldMapOpen
                || GUI.lockToggleEnable
                || GameManager.instance.IsGamePaused()
                || !TransitionData.TransitionModeActive())
            {
                return;
            }

            if (colorUpdateThread != null && colorUpdateThread.IsAlive) return;

            colorUpdateThread = new(() =>
            {
                if (Transition.GetRoomClosestToMiddle(selectedScene, out selectedScene))
                {
                    Transition.SetSelectedRoomColor(selectedScene);
                    UpdateAll();
                    TransitionWorldMap.UpdateAll();
                }
            });

            colorUpdateThread.Start();
        }

        private static Thread searchThread;
        private static Stopwatch attackHoldTimer = new();

        // Called every frame
        public static void Update()
        {
            if (!TransitionData.TransitionModeActive()
                || !GUI.worldMapOpen
                || GUI.lockToggleEnable
                || GameManager.instance.IsGamePaused()
                || InputHandler.Instance == null)
            {
                return;
            }

            // Use menu selection button for control
            if (InputHandler.Instance.inputActions.menuSubmit.WasPressed
                && (searchThread == null || !searchThread.IsAlive))
            {
                searchThread = new(GetRoute);
                searchThread.Start();
                attackHoldTimer.Reset();
            }

            // Hold attack to benchwarp in world map
            if (selectedRoute.Any() && selectedRoute.First().IsBenchwarpTransition() && Dependencies.HasDependency("Benchwarp"))
            {
                if (InputHandler.Instance.inputActions.attack.WasPressed)
                {
                    attackHoldTimer.Restart();
                }

                if (InputHandler.Instance.inputActions.attack.WasReleased)
                {
                    attackHoldTimer.Reset();
                }

                if (attackHoldTimer.ElapsedMilliseconds >= 500)
                {
                    attackHoldTimer.Reset();

                    GameManager.instance.StartCoroutine(CloseInventoryBenchwarp());
                }
            }
        }

        private static IEnumerator CloseInventoryBenchwarp()
        {
            GameManager.instance.inventoryFSM.SendEvent("HERO DAMAGED");
            yield return new WaitWhile(() => GameManager.instance.inventoryFSM.ActiveStateName == "Closed");
            Dependencies.DoBenchwarp(PathfinderData.GetBenchwarpScene(selectedRoute.First()));
        }

        public static void GetRoute()
        {
            if (pf == null) return;

            if (lastStartScene != Utils.CurrentScene() || lastFinalScene != selectedScene)
            {
                rejectedRoutes.Clear();
            }

            try
            {
                selectedRoute = pf.ShortestRoute(Utils.CurrentScene(), selectedScene, rejectedRoutes, MapModS.GS.allowBenchWarpSearch, false);
            }
            catch (Exception e)
            {
                MapModS.Instance.LogError(e);
            }

            AfterGetRoute();
        }

        public static void ReevaluateRoute(ItemChanger.Transition lastTransition)
        {
            if (pf == null) return;

            rejectedRoutes.Clear();

            try
            {
                selectedRoute = pf.ShortestRoute(lastTransition.ToString(), lastFinalTransition.GetAdjacentTransition(), rejectedRoutes, MapModS.GS.allowBenchWarpSearch, true);
            }
            catch (Exception e)
            {
                MapModS.Instance.LogError(e);
            }

            AfterGetRoute();
        }

        public static void AfterGetRoute()
        {
            if (!selectedRoute.Any())
            {
                ResetRoute();
            }
            else
            {
                lastStartScene = Utils.CurrentScene();
                lastFinalScene = selectedRoute.Last().GetAdjacentScene();
                lastStartTransition = selectedRoute.First();
                lastFinalTransition = selectedRoute.Last(); 
                transitionsCount = selectedRoute.Count();

                rejectedRoutes.Add(selectedRoute);
            }

            UpdateAll();
            TransitionWorldMap.UpdateInstructions();
            TransitionWorldMap.UpdateRouteSummary();

            RouteCompass.UpdateCompass();
        }

        public static void ResetRoute()
        {
            lastStartScene = "";
            lastFinalScene = "";
            lastStartTransition = "";
            lastFinalTransition = "";
            transitionsCount = 0;
            selectedRoute.Clear();
            rejectedRoutes.Clear();
        }

        public static void UpdateRoute(ItemChanger.Transition lastTransition)
        {
            if (!selectedRoute.Any()) return;

            string transition = selectedRoute.First();

            // Check adjacent transition matches the route's transition
            if (lastTransition.ToString() == transition.GetAdjacentTransition())
            {
                selectedRoute.Remove(transition);
                UpdateAll();
                TransitionWorldMap.UpdateInstructions();
                TransitionWorldMap.UpdateRouteSummary();

                if (!selectedRoute.Any())
                {
                    rejectedRoutes.Clear();
                }

                return;
            }
            else if (transition.ToString().Contains("Tram_")
                && (lastTransition.SceneName == "Room_Tram_RG" || lastTransition.SceneName == "Room_Tram"))
            {
                return;
            }

            HandleOffRoute(lastTransition);
        }

        public static void HandleOffRoute(ItemChanger.Transition lastTransition)
        {
            switch (MapModS.GS.whenOffRoute)
            {
                case OffRouteBehaviour.Cancel:
                    ResetRoute();
                    UpdateAll();
                    TransitionWorldMap.UpdateAll();
                    RouteCompass.UpdateCompass();
                    break;
                case OffRouteBehaviour.Reevaluate:
                    ReevaluateRoute(lastTransition);
                    break;
                default:
                    break;
            }
        }
    }
}

﻿using LunaClient.Base;
using LunaClient.Systems.Lock;
using LunaClient.VesselStore;
using LunaCommon.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LunaClient.Windows.Locks
{
    /// <summary>
    /// Here we should only display the statistics for systems that contain ROUTINES or code that executes on every fixedupdate/update/lateupdate
    /// </summary>
    public partial class LocksWindow : Window<LocksWindow>
    {
        #region Fields & properties

        private static bool _display;
        public override bool Display
        {
            get => base.Display && _display && MainSystem.NetworkState >= ClientState.Running && HighLogic.LoadedScene >= GameScenes.SPACECENTER;
            set => base.Display = _display = value;
        }

        private float WindowHeight { get; } = 400;
        private float WindowWidth { get; } = 400;

        private DateTime _lastUpdateTime = DateTime.MinValue;

        private static readonly List<VesselLockDisplay> VesselLocks = new List<VesselLockDisplay>();
        private static string _asteroidLockOwner = string.Empty;
        private static readonly StringBuilder StrBuilder = new StringBuilder();

        #endregion

        public override void Update()
        {
            base.Update();
            if (Display && DateTime.Now - _lastUpdateTime > TimeSpan.FromSeconds(3))
            {
                _lastUpdateTime = DateTime.Now;

                var asteroidLock = LockSystem.LockQuery.AsteroidLock();
                _asteroidLockOwner = asteroidLock?.PlayerName;

                for (var i = 0; i < FlightGlobals.Vessels.Count; i++)
                {
                    var existingVesselLock = VesselLocks.FirstOrDefault(v => v.VesselId == FlightGlobals.Vessels[i].id);
                    if (existingVesselLock == null)
                    {
                        VesselLocks.Add(new VesselLockDisplay
                        {
                            VesselId = FlightGlobals.Vessels[i].id,
                            Loaded = FlightGlobals.Vessels[i].loaded,
                            Packed = FlightGlobals.Vessels[i].packed,
                            VesselName = FlightGlobals.Vessels[i].name,
                            ControlLockOwner = LockSystem.LockQuery.GetControlLockOwner(FlightGlobals.Vessels[i].id),
                            UpdateLockOwner = LockSystem.LockQuery.GetUpdateLockOwner(FlightGlobals.Vessels[i].id),
                            UnloadedUpdateLockOwner = LockSystem.LockQuery.GetUnloadedUpdateLockOwner(FlightGlobals.Vessels[i].id),
                            ExistsInStore = VesselsProtoStore.AllPlayerVessels.ContainsKey(FlightGlobals.Vessels[i].id)
                        });
                    }
                    else
                    {
                        existingVesselLock.Loaded = FlightGlobals.Vessels[i].loaded;
                        existingVesselLock.Packed = FlightGlobals.Vessels[i].packed;
                        existingVesselLock.VesselName = FlightGlobals.Vessels[i].name;
                        existingVesselLock.ControlLockOwner = LockSystem.LockQuery.GetControlLockOwner(FlightGlobals.Vessels[i].id);
                        existingVesselLock.UpdateLockOwner = LockSystem.LockQuery.GetUpdateLockOwner(FlightGlobals.Vessels[i].id);
                        existingVesselLock.UnloadedUpdateLockOwner = LockSystem.LockQuery.GetUnloadedUpdateLockOwner(FlightGlobals.Vessels[i].id);
                        existingVesselLock.ExistsInStore = VesselsProtoStore.AllPlayerVessels.ContainsKey(FlightGlobals.Vessels[i].id);
                    }
                }

                VesselLocks.RemoveAll(v => FlightGlobals.Vessels.All(ev => ev.id != v.VesselId));
            }
        }

        public override void OnGui()
        {
            base.OnGui();
            if (Display)
            {
                WindowRect = FixWindowPos(GUILayout.Window(6717 + MainSystem.WindowOffset, WindowRect, DrawContent, "Locks", WindowStyle, LayoutOptions));
            }
            CheckWindowLock();
        }

        public override void SetStyles()
        {
            WindowRect = new Rect(Screen.width - (WindowWidth + 50), Screen.height / 2f - WindowHeight / 2f, WindowWidth,
                WindowHeight);
            MoveRect = new Rect(0, 0, 10000, 20);

            LayoutOptions = new GUILayoutOption[4];
            LayoutOptions[0] = GUILayout.MinWidth(WindowWidth);
            LayoutOptions[1] = GUILayout.MaxWidth(WindowWidth);
            LayoutOptions[2] = GUILayout.MinHeight(WindowHeight);
            LayoutOptions[3] = GUILayout.MaxHeight(WindowHeight);
            
            TextAreaOptions = new GUILayoutOption[1];
            TextAreaOptions[0] = GUILayout.ExpandWidth(true);
        }

        public override void RemoveWindowLock()
        {
            if (IsWindowLocked)
            {
                IsWindowLocked = false;
                InputLockManager.RemoveControlLock("LMP_LocksWindowsLock");
            }
        }

        private void CheckWindowLock()
        {
            if (Display)
            {
                if (MainSystem.NetworkState < ClientState.Running || HighLogic.LoadedSceneIsFlight)
                {
                    RemoveWindowLock();
                    return;
                }

                Vector2 mousePos = Input.mousePosition;
                mousePos.y = Screen.height - mousePos.y;

                var shouldLock = WindowRect.Contains(mousePos);

                if (shouldLock && !IsWindowLocked)
                {
                    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, "LMP_LocksWindowsLock");
                    IsWindowLocked = true;
                }
                if (!shouldLock && IsWindowLocked)
                    RemoveWindowLock();
            }

            if (!Display && IsWindowLocked)
                RemoveWindowLock();
        }
    }
}

// Copyright (c) 2014, Eric Harris (ozraven)
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the copyright holder nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL ERIC HARRIS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using UnityEngine;

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
    internal abstract class GUIWindow
    {
        private static volatile int NextWindowId = 5000;

        private Rect minimumDimensions;
        private Scene scene;
        private int windowId;
        protected bool visible;
        protected Rect window;
        private string title;
        private string configurationNodeName;
        private GUIWindow parent;
        private List<GUIWindow> children;
        protected bool mouseOver;
        private bool renderError;

        private const ControlTypes EditorLockControlTypes = ControlTypes.CAMERACONTROLS
                                                | ControlTypes.EDITOR_ICON_HOVER
                                                | ControlTypes.EDITOR_ICON_PICK
                                                | ControlTypes.EDITOR_PAD_PICK_PLACE
                                                | ControlTypes.EDITOR_PAD_PICK_COPY
                                                | ControlTypes.EDITOR_EDIT_STAGES
                                                | ControlTypes.EDITOR_ROTATE_PARTS
                                                | ControlTypes.EDITOR_OVERLAYS;
        private volatile bool editorLocked;
        private string editorLockToken;
        
        [Flags]
        public enum Scene
        {
            None = 0,
            Loading = 1,                    // SceneFlags.LOADING
            LoadingBuffer = 2,              // SceneFlags.LOADINGBUFFER
            MainMenu = 4,                   // SceneFlags.MAINMENU
            Settings = 8,                   // SceneFlags.SETTINGS
            Credits = 16,                   // SceneFlags.CREDITS
            SpaceCenter = 32,               // SceneFlags.SPACECENTER
            VehicleAssemblyBuilding = 64,   // SceneFlags.EDITOR
            Flight = 128,                   // SceneFlags.FLIGHT
            TrackingStation = 256,          // SceneFlags.TRACKSTATION
            SpaceplaneHangar = 512,         // SceneFlags.SPH
            PSYSTEM = 1024,                 // SceneFlags.PSYSTEM
            Editor = VehicleAssemblyBuilding | SpaceplaneHangar
        }

        #region Scene Conversion

        private static Scene SceneFromGameScenes(GameScenes gameScene)
        {
            Scene result = Scene.None;

            switch(gameScene)
            {
            case GameScenes.LOADING:
                result = Scene.Loading;
                break;
            case GameScenes.LOADINGBUFFER:
                result = Scene.LoadingBuffer;
                break;
            case GameScenes.MAINMENU:
                result = Scene.MainMenu;
                break;
            case GameScenes.SETTINGS:
                result = Scene.Settings;
                break;
            case GameScenes.CREDITS:
                result = Scene.Credits;
                break;
            case GameScenes.SPACECENTER:
                result = Scene.SpaceCenter;
                break;
            case GameScenes.EDITOR:
                result = Scene.VehicleAssemblyBuilding;
                break;
            case GameScenes.FLIGHT:
                result = Scene.Flight;
                break;
            case GameScenes.TRACKSTATION:
                result = Scene.TrackingStation;
                break;
            case GameScenes.SPH:
                result = Scene.SpaceplaneHangar;
                break;
            case GameScenes.PSYSTEM:
                result = Scene.PSYSTEM;
                break;
            default:
                throw new ArgumentOutOfRangeException("gameScene", string.Format(CultureInfo.InvariantCulture, "Unknown gameScene {0}", gameScene));
            }

            return result;
        }

        #endregion

        protected GUIWindow(Scene scene, Rect defaultDimensions, Rect minimumDimensions, string title, string configurationNodeName)
        {
            this.minimumDimensions = minimumDimensions;
            this.windowId = NextWindowId++;
            this.scene = scene;
            this.window = defaultDimensions;
            this.title = title;
            this.configurationNodeName = configurationNodeName;
            this.children = new List<GUIWindow>();
            this.mouseOver = false;
            this.renderError = false;
            this.editorLockToken = string.Format(CultureInfo.InvariantCulture, "PartWizard.Window({0})", this.windowId);
            this.editorLocked = false;
        }

        public bool Visible
        {
            get
            {
                return this.visible;
            }
        }

        protected void SetTitle(string newTitle)
        {
            this.title = newTitle;
        }

        public virtual void Show()
        {
            this.Show(null);
        }

        public void Show(GUIWindow parentWindow)
        {
            this.window = Configuration.GetValue(this.configurationNodeName, this.window);

            this.window.x = Mathf.Clamp(this.window.x, this.minimumDimensions.x, Screen.width - this.window.width);
            this.window.y = Mathf.Clamp(this.window.y, this.minimumDimensions.y, Screen.height - this.window.height);
            this.window.width = Mathf.Clamp(this.window.width, this.minimumDimensions.width, Screen.width - this.window.x);
            this.window.height = Mathf.Clamp(this.window.height, this.minimumDimensions.height, Screen.height - this.window.y);

            this.visible = true;

            if(parentWindow != null)
            {
                this.parent = parentWindow;
                this.parent.children.Add(this);
            }
        }

        public virtual void Hide()
        {
            this.visible = false;

            Configuration.SetValue(this.configurationNodeName, this.window);

            Configuration.Save();

            foreach(GUIWindow child in this.children)
            {
                child.Hide();
            }

            this.parent = null;
            this.children.Clear();
        }

        public void Render()
        {
            // Editor locking logic adapted from m4v's RCSBuildAid.

            Scene loadedScene = GUIWindow.SceneFromGameScenes(HighLogic.LoadedScene);

            if((this.scene & loadedScene) == loadedScene)
            {
                if(this.visible)
                {
                    GUI.skin.window.clipping = TextClipping.Clip;

                    this.window = GUILayout.Window(this.windowId, this.window, this.InternalRender, this.title);

                    foreach(GUIWindow child in this.children)
                    {
                        child.Render();
                    }

                    if(Event.current.type == EventType.Repaint)
                    {
                        this.mouseOver = GUIControls.MouseOverWindow(ref this.window);

                        if(this.mouseOver && !this.editorLocked)
                        {
                            InputLockManager.SetControlLock(GUIWindow.EditorLockControlTypes, this.editorLockToken);
                            this.editorLocked = true;
                        }
                        else if(!this.mouseOver && this.editorLocked)
                        {
                            InputLockManager.RemoveControlLock(this.editorLockToken);
                            this.editorLocked = false;
                        }
                    }
                }
                else if(this.editorLocked)
                {
                    InputLockManager.RemoveControlLock(this.editorLockToken);
                    this.editorLocked = false;
                }
            }
        }
        
        private void InternalRender(int renderingWindowId)
        {
            GUIControls.BeginLayout();

            try
            {
                if(GUIControls.TitleBarButton(this.window))
                {
                    this.Hide();

                    GUI.DragWindow();
                }
                else
                {
                    if(!this.renderError)
                    {
                        this.OnRender();
                    }
                    else
                    {
                        this.OnErrorRender();
                    }
                }
            }
            catch(Exception)
            {
                this.renderError = true;

                throw;
            }
            finally
            {
                GUIControls.EndLayout();
            }
        }

        public abstract void OnRender();

        protected virtual void OnErrorRender()
        {
            GUILayout.BeginVertical();

            GUILayoutOption maxWidth = GUILayout.MaxWidth(this.window.width);
            GUILayoutOption lockWidth = GUILayout.ExpandWidth(false);
            GUILayoutOption lockHeight = GUILayout.ExpandHeight(false);

            GUILayoutOption informationLabelMaxHeight = GUILayout.MaxHeight(this.window.height * 0.75f);    // Magically use 3/4ths of the window for the top information label.
            GUILayout.Label(string.Format(CultureInfo.CurrentCulture, Localized.GuiRenderErrorTextFormat, PartWizardPlugin.Name), maxWidth, informationLabelMaxHeight, lockWidth, lockHeight);

            // Fix up the path for the current environment.
            string platformCompatibleRootPath = KSPUtil.ApplicationRootPath.Replace('/', Path.DirectorySeparatorChar);
            // Trim off the extra path components to get the actual KSP root path.
            string actualRootPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(platformCompatibleRootPath)));
            string kspDataPath = Path.Combine(actualRootPath, "KSP_Data");
            string kspLogFile = Path.Combine(kspDataPath, "output_log.txt");

            GUIStyle textFieldStyle = new GUIStyle();
            textFieldStyle.wordWrap = true;
            textFieldStyle.normal.textColor = Color.white;

            GUILayoutOption informationFieldMaxHeight = GUILayout.MaxHeight(this.window.height * 0.25f);    // Magically use 1/4th of the window for the bottom information field.
            GUILayout.TextField(kspLogFile, textFieldStyle, maxWidth, informationFieldMaxHeight, lockWidth, lockHeight);

            GUILayout.EndVertical();
        }
    }
}

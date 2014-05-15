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

using KSP.IO;

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
    public class PartWizardWindow
    {
        private const float DefaultX = 300;
        private const float DefaultY = 200;
        private const float DefaultWidth = 250;
        private const float DefaultHeight = 400;
        
        private readonly Color TooltipLabelColor = Color.yellow;
        private readonly Color PartCountLabelColor = Color.white;

        private const string WindowPositionConfigurationName = "PART_WIZARD_WINDOW";

        private string pluginName;
        private string windowTitle;

        private bool visible;
        
        private int partWizardWindowId;

        // Early instantiation of this so that when we save the configuration we don't end up with all zeros. (Rect is a struct, so the default constructor is used as structs
        // are non-nullable entities.)
        private Rect window = new Rect(DefaultX, DefaultY, DefaultWidth, DefaultHeight);

        private GUIStyle tooltipLabelStyle;
        private GUIStyle partCountLabelStyle;

        private GUILayoutOption PartActionButtonWidth = GUILayout.Width(22);

        private volatile bool controllingPartHighlight = false;
        private uint highlightedPartId = 0;

        private Vector2 scrollPosition;

        private enum ViewType
        {
            All = 0,
            Hidden = 1
        }

        private ViewType viewType = ViewType.All;

        private GUIStyle selectedViewTypeStyle;
        private GUIStyle unselectedViewTypeStyle;

        private GUIContent[] viewTypeContents;

        private bool renderError = false;

        public bool Visible
        {
            get
            {
                return this.visible;
            }
        }
        
        public PartWizardWindow(string name, string version)
        {
            this.pluginName = name;
            this.windowTitle = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, version);

            this.tooltipLabelStyle = new GUIStyle();
            this.tooltipLabelStyle.normal.textColor = TooltipLabelColor;

            this.partCountLabelStyle = new GUIStyle();
            this.partCountLabelStyle.normal.textColor = PartCountLabelColor;
            this.partCountLabelStyle.alignment = TextAnchor.LowerRight;

            this.selectedViewTypeStyle = new GUIStyle("button");
            this.selectedViewTypeStyle.onNormal.textColor = Color.green;
            this.selectedViewTypeStyle.onHover.textColor = Color.green;

            this.unselectedViewTypeStyle = new GUIStyle("button");

            this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden) };
        }

        public void Show(int windowId)
        {
            this.partWizardWindowId = windowId;

            this.window = Configuration.GetValue(PartWizardWindow.WindowPositionConfigurationName, new Rect(PartWizardWindow.DefaultX, PartWizardWindow.DefaultY, PartWizardWindow.DefaultWidth, PartWizardWindow.DefaultHeight));

            this.window.x = Mathf.Clamp(this.window.x, 0, Screen.width - this.window.width);
            this.window.y = Mathf.Clamp(this.window.y, 0, Screen.height - this.window.height);

            this.visible = true;
        }

        public void Hide()
        {
            this.visible = false;

            if(!this.renderError)
            {
                Configuration.SetValue(PartWizardWindow.WindowPositionConfigurationName, this.window);

                Configuration.Save();
            }
        }

        public void Render()
        {
            if(this.visible && (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH))
            {
                GUI.skin.window.clipping = TextClipping.Clip;
                if(Event.current.type == EventType.Layout)
                {
                    this.window = GUILayout.Window(this.partWizardWindowId, this.window, OnRender, this.windowTitle);
                }
            }
        }
        
        private void OnRender(int windowId)
        {
            GUIControls.BeginLayout();

            try
            {
                if(GUIControls.TitleBarButton(this.window))
                {
                    this.Hide();
                }

                if(!renderError)
                {
                    List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();

                    GUILayout.BeginVertical();

                    #region Display Mode Control

                    GUILayout.BeginHorizontal();

                    this.viewType = (ViewType)GUIControls.HorizontalToggleSet((int)this.viewType, this.viewTypeContents, this.selectedViewTypeStyle, this.unselectedViewTypeStyle);

                    GUILayout.EndHorizontal();

                    if(this.viewType == ViewType.Hidden)
                    {
                        parts = parts.FindAll((p) => { return p.partInfo.category == PartCategories.none; });
                    }

                    #endregion

                    this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, false, false);

                    foreach(Part part in parts)
                    {
                        bool mouseOver = false;         // Must be updated (|=) by each control that can trigger part highlighting when the mouse is over the part in the list.

                        GUILayout.BeginHorizontal();

                        #region Part Label

                        bool labelMouseOver = false;    // Will be set to true if the mouse is over the part's label.

                        // We want to draw the part's name first, but it won't exist when the time comes to render it if the user chooses to delete it. DisplayName() must be
                        // careful to make sure the part still exists.
                        GUIControls.MouseOverLabel(new GUIContent(part.partInfo.title, part.partInfo.name), out labelMouseOver);

                        mouseOver |= labelMouseOver;

                        #endregion

                        // Only enable the following buttons if there is no actively selected part, but we want to have them drawn.
                        GUI.enabled = !EditorLogic.SelectedPart;

                        #region Break Symmetry Button

                        bool breakSymmetryMouseOver = false;

                        string breakabilityReport = default(string);
                        GUI.enabled = !EditorLogic.SelectedPart && PartWizard.HasBreakableSymmetry(part, out breakabilityReport);

                        string breakSymmetryTooltip = GUI.enabled ? Localized.BreakSymmetryDescription : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.BreakSymmetryButtonText, breakSymmetryTooltip), out breakSymmetryMouseOver, PartActionButtonWidth))
                        {
                            PartWizard.BreakSymmetry(part);

                            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[PartWizard] {0} symmetry broken", part.name));
                        }

                        mouseOver |= breakSymmetryMouseOver;

                        #endregion

                        #region Delete Button

                        bool deleteButtonMouseOver = false;     // Will be set to true if the mouse is over the part's delete button.

                        bool deleted = false;                   // Will be set to true if the delete button was pressed.

                        GUI.enabled = !EditorLogic.SelectedPart && PartWizard.IsDeleteable(part);

                        string deleteTooltip = GUI.enabled ? Localized.DeletePartDescription : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.DeletePartButtonText, deleteTooltip), out deleteButtonMouseOver, PartActionButtonWidth))
                        {
                            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[PartWizard] deleting part {0}", part.name));

                            PartWizard.Delete(part);

                            // Set a flag so additional GUI logic can decide what to do in the case where a part is deleted.
                            deleted = true;
                        }

                        mouseOver |= deleteButtonMouseOver;

                        #endregion

                        GUI.enabled = true;

                        GUILayout.EndHorizontal();      // End of row for this part.

                        if(!deleted)
                        {
                            if(mouseOver)
                            {
                                part.SetHighlight(true);

                                this.controllingPartHighlight = true;
                                this.highlightedPartId = part.uid;
                            }
                            else if(controllingPartHighlight && part.uid == this.highlightedPartId)
                            {
                                part.SetHighlight(false);

                                this.controllingPartHighlight = false;
                                this.highlightedPartId = 0;
                            }
                        }

                        // If we deleted a part, then just jump out of the loop since the parts list has been modified.
                        if(deleted)
                        {
                            break;
                        }
                    }

                    GUILayout.EndScrollView();

                    #region Status Area

                    GUILayout.Space(3);

                    string status = default(string);

                    if(!string.IsNullOrEmpty(GUI.tooltip))
                    {
                        status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTooltipTextFormat, parts.Count, GUI.tooltip);
                    }
                    else
                    {
                        status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTextFormat, parts.Count);
                    }

                    GUILayout.Label(status, this.tooltipLabelStyle);

                    GUILayout.EndVertical();

                    #endregion
                }
                else
                {
                    #region Error Mode GUI

                    GUILayout.BeginVertical();

                    GUILayoutOption maxWidth = GUILayout.MaxWidth(this.window.width);
                    GUILayoutOption maxHeight = GUILayout.MaxHeight(this.window.height / 2);    // Magic number 2 because we're going to have only 2 GUI controls, below.
                    GUILayoutOption lockWidth = GUILayout.ExpandWidth(false);
                    GUILayoutOption lockHeight = GUILayout.ExpandHeight(false);

                    GUILayout.Label(string.Format(CultureInfo.CurrentCulture, Localized.GuiRenderErrorTextFormat, this.pluginName), maxWidth, maxHeight, lockWidth, lockHeight);

                    // Fix up the path for the current environment.
                    string platformCompatibleRootPath = KSPUtil.ApplicationRootPath.Replace('/', Path.DirectorySeparatorChar);
                    // Trim off the extra path components to get the actual KSP root path.
                    string actualRootPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(platformCompatibleRootPath)));
                    string kspDataPath = Path.Combine(actualRootPath, "KSP_Data");
                    string kspLogFile = Path.Combine(kspDataPath, "output_log.txt");

                    GUIStyle textFieldStyle = new GUIStyle();
                    textFieldStyle.wordWrap = true;
                    textFieldStyle.normal.textColor = Color.white;

                    GUILayout.TextField(kspLogFile, textFieldStyle, maxWidth, maxHeight, lockWidth, lockHeight);

                    GUILayout.EndVertical();

                    #endregion
                }
            }
            catch(Exception e)
            {
                this.renderError = true;

                Debug.LogError("PartWizard :: Window rendering error, details follow:");
                Debug.LogException(e);

                throw;
            }
            finally
            {
                GUI.DragWindow();

                GUIControls.EndLayout();
            }
        }
    }
}

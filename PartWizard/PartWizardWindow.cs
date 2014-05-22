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
#if TEST
    using Part = global::PartWizard.Test.MockPart;
    using EditorLogic = global::PartWizard.Test.MockEditorLogic;
    using ShipConstruct = global::PartWizard.Test.MockShipConstruct;
#endif

    internal sealed class PartWizardWindow : Window
    {
        private const float DefaultX = 300;
        private const float DefaultY = 200;
        private const float DefaultWidth = 250;
        private const float DefaultHeight = 400;
        
        private readonly Color TooltipLabelColor = Color.yellow;
        private readonly Color PartCountLabelColor = Color.white;

        private const string WindowPositionConfigurationName = "PART_WIZARD_WINDOW";

        private string pluginName;

        private GUIStyle tooltipLabelStyle;
        private GUIStyle partCountLabelStyle;

        //private volatile bool controllingPartHighlight = false;
        //private Part highlightedPart = null;

        private Vector2 scrollPosition;

        private HighlightTracker highlight;

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

        private SymmetryEditorWindow symmetryEditorWindow;

        public PartWizardWindow(string name, string version)
            : base(Scene.Editor, new Rect(DefaultX, DefaultY, DefaultWidth, DefaultHeight), new Rect(DefaultX + DefaultWidth, DefaultY, DefaultWidth, DefaultHeight), name, WindowPositionConfigurationName)
        {
            this.pluginName = name;
            this.Title = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, version);

            this.tooltipLabelStyle = new GUIStyle(GUIControls.PanelStyle);
            this.tooltipLabelStyle.normal.textColor = TooltipLabelColor;

            this.partCountLabelStyle = new GUIStyle();
            this.partCountLabelStyle.normal.textColor = PartCountLabelColor;
            this.partCountLabelStyle.alignment = TextAnchor.LowerRight;

            this.selectedViewTypeStyle = new GUIStyle("button");
            this.selectedViewTypeStyle.onNormal.textColor = Color.green;
            this.selectedViewTypeStyle.onHover.textColor = Color.green;

            this.unselectedViewTypeStyle = new GUIStyle("button");

            this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden) };

            this.symmetryEditorWindow = new SymmetryEditorWindow();

            this.highlight = new HighlightTracker();
        }

        public override void Hide()
        {
            this.visible = false;

            GameEvents.onPartRemove.Remove(this.OnPartRemoved);

            if(!this.renderError)
            {
                base.Hide();
            }
        }

        public override void Show()
        {
            GameEvents.onPartRemove.Add(this.OnPartRemoved);

            base.Show();
        }

        private void OnPartRemoved(GameEvents.HostTargetAction<Part, Part> e)
        {
            if(this.symmetryEditorWindow.Visible)
            {
                if(PartWizard.IsSibling(e.target, this.symmetryEditorWindow.Part))
                {
                    this.symmetryEditorWindow.Part = null;
                }
            }
        }

        public override void OnRender()
        {
            try
            {
                // TODO: Splitting 4X in to two 2X, once the Symmetry Editor window closes and the mouse cursor stays over this window, both symmetrical parts are green
                // rather than the intended root = green, counterparts = yellow.

                if(!renderError)
                {
                    List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();

                    this.highlight.BeginTracking();

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

                    #region Part List

                    GUILayout.BeginVertical(GUIControls.PanelStyle);

                    this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, false, false);

                    foreach(Part part in parts)
                    {
                        GUIControls.BeginMouseOverHorizontal();

                        #region Part Label

                        GUILayout.Label(new GUIContent(part.partInfo.title, part.partInfo.name));

                        #endregion

                        // Only enable the following buttons if there is no actively selected part, but we want to have them drawn.
                        GUI.enabled = EditorLogic.SelectedPart == null;
                        
                        // TODO: Log symmetry modifications.

                        #region Break Symmetry Button

                        string breakabilityReport = default(string);
                        GUI.enabled = EditorLogic.SelectedPart == null && PartWizard.HasBreakableSymmetry(part, out breakabilityReport);

                        string breakSymmetryTooltip = GUI.enabled ? Localized.BreakSymmetryDescription : default(string);

                        bool breakSymmetryMouseOver = false;
                        if(GUIControls.MouseOverButton(new GUIContent(Localized.BreakSymmetryButtonText, breakSymmetryTooltip), out breakSymmetryMouseOver, Configuration.PartActionButtonWidth))
                        {
                            this.symmetryEditorWindow.Part = part;

                            if(!this.symmetryEditorWindow.Visible)
                            {
                                this.symmetryEditorWindow.Show(this);

                                // Short circuit the mouse over for breaking symmetry when showing the Symmetry Editor in case it appears over top of this
                                // button and immediately begins highlighting parts. This would cause *this* window's highlighting to be stuck on the part.
                                breakSymmetryMouseOver = false;
                            }
                            else
                            {
                                this.symmetryEditorWindow.Hide();
                            }
                        }

                        breakSymmetryMouseOver &= GUI.enabled;  // Clear mouse over flag if the symmetry button was disabled.

                        #endregion

                        #region Delete Button

                        bool deleted = false;                   // Will be set to true if the delete button was pressed.

                        GUI.enabled = EditorLogic.SelectedPart == null && PartWizard.IsDeleteable(part);

                        string deleteTooltip = GUI.enabled 
                            ? ((part.symmetryCounterparts.Count == 0) ? Localized.DeletePartSingularDescription : Localized.DeletePartPluralDescription)
                            : default(string);

                        bool deleteButtonMouseOver = false;     // Will be set to true if the mouse is over the part's delete button.
                        if(GUIControls.MouseOverButton(new GUIContent(Localized.DeletePartButtonText, deleteTooltip), out deleteButtonMouseOver, Configuration.PartActionButtonWidth))
                        {
                            Log.Write("Deleting part {0}.", part.name);

                            PartWizard.Delete(part);

                            // Set a flag so additional GUI logic can decide what to do in the case where a part is deleted.
                            deleted = true;
                        }

                        deleteButtonMouseOver &= GUI.enabled;   // Clear mouse over flag if the delete button was disabled.

                        #endregion

                        GUI.enabled = true;

                        bool groupMouseOver = false;
                        GUIControls.EndMouseOverHorizontal(out groupMouseOver);     // End of row for this part.

                        // If we deleted a part, then just jump out of the loop since the parts list has been modified.
                        if(deleted)
                        {
                            break;
                        }

                        #region Part Highlighting Control

                        if(breakSymmetryMouseOver)
                        {
                            this.highlight.Add(part, Configuration.HighlightColorEditableSymmetryRoot, Configuration.HighlightColorEditableSymmetryCounterparts);
                        }
                        else if(deleteButtonMouseOver)
                        {
                            this.highlight.Add(part, Configuration.HighlightColorDeletablePart, Configuration.HighlightColorDeletableCounterparts, true);
                        }
                        else if(groupMouseOver)
                        {
                            Color highlightColor = (part.uid != EditorLogic.startPod.uid) ? Configuration.HighlightColorSinglePart : Configuration.HighlightColorRootPart;

                            this.highlight.Add(part, highlightColor, Configuration.HighlightColorCounterparts, false);
                        }

                        #endregion
                    }
                    
                    GUILayout.EndScrollView();

                    GUILayout.EndVertical();

                    #endregion

                    #region Status Area

                    GUILayout.Space(3);

                    string status = default(string);

                    if(!string.IsNullOrEmpty(GUI.tooltip))
                    {
                        if(parts.Count != 1)
                        {
                            status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelPluralTooltipTextFormat, parts.Count, GUI.tooltip);
                        }
                        else
                        {
                            status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelSingularTooltipTextFormat, parts.Count, GUI.tooltip);
                        }
                    }
                    else
                    {
                        if(parts.Count != 1)
                        {
                            status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelPluralTextFormat, parts.Count);
                        }
                        else
                        {
                            status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelSingularTextFormat, parts.Count);
                        }
                    }

                    GUILayout.Label(status, this.tooltipLabelStyle);

                    #endregion

                    GUILayout.EndVertical();
                }
                else
                {
                    #region Error Mode GUI

                    GUILayout.BeginVertical();

                    GUILayoutOption maxWidth = GUILayout.MaxWidth(this.window.width);
                    GUILayoutOption maxHeight = GUILayout.MaxHeight(this.window.height / 2);    // Magic number 2 because we're going to have only 2 GUI controls, below.
                    GUILayoutOption lockWidth = GUILayout.ExpandWidth(false);
                    GUILayoutOption lockHeight = GUILayout.ExpandHeight(false);

                    GUILayout.Label(string.Format(CultureInfo.CurrentCulture, Localized.GuiRenderErrorTextFormat, this.pluginName), GUIControls.PanelStyle, maxWidth, maxHeight, lockWidth, lockHeight);

                    // Fix up the path for the current environment.
                    string platformCompatibleRootPath = KSPUtil.ApplicationRootPath.Replace('/', Path.DirectorySeparatorChar);
                    // Trim off the extra path components to get the actual KSP root path.
                    string actualRootPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(platformCompatibleRootPath)));
                    string kspDataPath = Path.Combine(actualRootPath, "KSP_Data");
                    string kspLogFile = Path.Combine(kspDataPath, "output_log.txt");

                    GUIStyle textFieldStyle = new GUIStyle(GUIControls.PanelStyle);
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

                Log.Trace("Window rendering error, details follow:");
                Debug.LogException(e);

                throw;
            }
            finally
            {
                GUI.DragWindow();

                if(!this.renderError && this.visible && this.mouseOver)
                {
                    this.highlight.EndTracking();
                }
                else
                {
                    this.highlight.CancelTracking();
                }
            }
        }
    }
}

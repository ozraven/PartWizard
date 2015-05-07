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

using UnityEngine;

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
#if TEST
    using Part = global::PartWizard.Test.MockPart;
    using EditorLogic = global::PartWizard.Test.MockEditorLogic;
    using ShipConstruct = global::PartWizard.Test.MockShipConstruct;
    using GameEvents = global::PartWizard.Test.MockGameEvents;
#endif

    internal sealed class PartWizardWindow : GUIWindow
    {
        private static readonly Rect DefaultDimensions = new Rect(280, 160, 300, 400);
        private static readonly Rect MinimumDimensions = new Rect(0, 0, DefaultDimensions.width, DefaultDimensions.height);
        
        private readonly Color TooltipLabelColor = Color.yellow;

        private const string WindowPositionConfigurationName = "PART_WIZARD_WINDOW";

        private GUIStyle tooltipLabelStyle;
        
        private Vector2 scrollPosition;

        private IHighlightTracker highlight;

        private enum ViewType
        {
            All = 0,
            Hidden = 1,
            Unavailable = 2
        }

        private ViewType viewType = ViewType.All;

        private GUIStyle selectedViewTypeStyle;
        private GUIStyle unselectedViewTypeStyle;
        private GUIStyle actionEditorModePartButtonStyle;

        private GUIContent[] viewTypeContents;
        
        private SymmetryEditorWindow symmetryEditorWindow;

        public PartWizardWindow(string name, string version)
            : base(Scene.Editor, PartWizardWindow.DefaultDimensions, PartWizardWindow.MinimumDimensions, name, WindowPositionConfigurationName)
        {
            this.SetTitle(string.Format(CultureInfo.CurrentCulture, "{0} ({1})", name, version));

            this.tooltipLabelStyle = new GUIStyle(GUIControls.PanelStyle);
            this.tooltipLabelStyle.normal.textColor = TooltipLabelColor;
            this.tooltipLabelStyle.alignment = TextAnchor.MiddleLeft;

            this.selectedViewTypeStyle = new GUIStyle("button");
            this.selectedViewTypeStyle.onNormal.textColor = Color.green;
            this.selectedViewTypeStyle.onHover.textColor = Color.green;

            this.actionEditorModePartButtonStyle = new GUIStyle("button");
            this.actionEditorModePartButtonStyle.onHover.textColor = Color.blue;
            this.actionEditorModePartButtonStyle.alignment = TextAnchor.MiddleLeft;

            this.unselectedViewTypeStyle = new GUIStyle("button");

            this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden) };

            this.symmetryEditorWindow = new SymmetryEditorWindow();

            this.highlight = new HighlightTracker2();
        }

        public override void Hide()
        {
            GameEvents.onPartRemove.Remove(this.OnPartRemoved);

            base.Hide();
        }

        public override void Show()
        {
            GameEvents.onPartRemove.Add(this.OnPartRemoved);

            if(ResearchAndDevelopment.Instance != null)
            {
                this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden), new GUIContent(Localized.ViewTypeUnavailable) };
            }
            else
            {
                this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden) };
            }

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
                List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();

                this.highlight.BeginTracking();

                GUILayout.BeginVertical();

                #region Display Mode Control

                GUILayout.BeginHorizontal();

                this.viewType = (ViewType)GUIControls.HorizontalToggleSet((int)this.viewType, this.viewTypeContents, this.selectedViewTypeStyle, this.unselectedViewTypeStyle);

                GUILayout.EndHorizontal();

                List<Part> buyableParts = null;

                if(this.viewType == ViewType.Hidden)
                {
                    parts = parts.FindAll((p) => { return p.partInfo.category == PartCategories.none; });
                }
                else if(this.viewType == ViewType.Unavailable)
                {
                    parts = parts.FindAll((p) => {
                        bool result = false;

                        // Get the R&D technology state for the current part.
                        ProtoTechNode techState = ResearchAndDevelopment.Instance.GetTechState(p.partInfo.TechRequired);

                        // If there is a state or the technology is locked or the part hasn't been purchased...
                        if(techState == null || techState.state != RDTech.State.Available || !techState.partsPurchased.Contains(p.partInfo))
                        {
                            // ...add it to the list.
                            result = true;
                        }

                        return result;
                    });

                    // Stash the filtered list in to the buyable list.
                    buyableParts = parts;
                    // Create a new collection with a copy of all the buyable parts.
                    parts = new List<Part>(buyableParts);
                    // Create a hash set to act as a filter for duplicate parts.
                    HashSet<string> duplicatePartFilter = new HashSet<string>();
                    // Remove each part that has already been added to the hash filter.
                    parts.RemoveAll((p) => !duplicatePartFilter.Add(p.name));

                    // Here parts is a list of unique buyable parts and buyableParts is all of the buyable parts, including duplicates.
                }

                #endregion

                #region Part List

                GUILayout.BeginVertical(GUIControls.PanelStyle);

                this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, false, false);

                int totalEntryCost = 0;

                foreach(Part part in parts)
                {
                    totalEntryCost += part.partInfo.entryCost;

                    GUIControls.BeginMouseOverHorizontal();

                    bool actionEditorPartButtonMouseOver = false;

                    #region Part Label

                    if(EditorLogic.fetch.editorScreen != EditorScreen.Actions)
                    {
                        GUILayout.Label(new GUIContent(part.partInfo.title, part.partInfo.name));
                    }
                    else
                    {
                        if(GUIControls.MouseOverButton(new GUIContent(part.partInfo.title, part.partInfo.name), out actionEditorPartButtonMouseOver, this.actionEditorModePartButtonStyle))
                        {
                            // Each part gets the EditorActionPartSelector added to it when the editor switches to the Actions screen. (And it
                            // gets taken away when leaving that screen.)
                            EditorActionPartSelector selector = part.GetComponent<EditorActionPartSelector>();

                            // Make sure we have it...
                            if(selector != null)
                            {
                                // ...and select it.
                                selector.Select();

                                Log.Write("Action editor selecting part {0}.", part.name);
                            }
                        }
                    }

                    #endregion

                    GUILayout.FlexibleSpace();

                    // Only enable the following buttons if there is no actively selected part, but we want to have them drawn.
                    GUI.enabled = EditorLogic.SelectedPart == null;

                    bool deleted = false;                   // Will be set to true if the delete button was pressed.
                    bool bought = false;                    // Will be set to true if the buy button was pressed.

                    bool breakSymmetryMouseOver = false;    // Will be set to true if the mouse is over the part's break symmetry button.
                    bool deleteButtonMouseOver = false;     // Will be set to true if the mouse is over the part's delete button.
                    bool buyButtonMouseOver = false;        // Will be set to true if the mouse is over the part's buy button.

                    string deleteTooltip = default(string);
                    string buyTooltip = default(string);

                    if(this.viewType == ViewType.All || this.viewType == ViewType.Hidden)
                    {
                        #region Break Symmetry Button

                        string breakabilityReport = default(string);
                        GUI.enabled = EditorLogic.SelectedPart == null && EditorLogic.fetch.editorScreen == EditorScreen.Parts && PartWizard.HasBreakableSymmetry(part, out breakabilityReport);

                        string breakSymmetryTooltip = GUI.enabled ? Localized.BreakSymmetryDescription : default(string);

                        breakSymmetryMouseOver = false;
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
                        }

                        breakSymmetryMouseOver &= GUI.enabled;  // Clear mouse over flag if the symmetry button was disabled.

                        #endregion

                        #region Delete Button

                        GUI.enabled = EditorLogic.SelectedPart == null && EditorLogic.fetch.editorScreen == EditorScreen.Parts && PartWizard.IsDeleteable(part);

                        deleteTooltip = GUI.enabled
                            ? ((part.symmetryCounterparts.Count == 0) ? Localized.DeletePartSingularDescription : Localized.DeletePartPluralDescription)
                            : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.DeletePartButtonText, deleteTooltip), out deleteButtonMouseOver, Configuration.PartActionButtonWidth))
                        {
                            PartWizard.Delete(part);

                            // Set a flag so additional GUI logic can decide what to do in the case where a part is deleted.
                            deleted = true;
                        }

                        deleteButtonMouseOver &= GUI.enabled;   // Clear mouse over flag if the delete button was disabled.

                        #endregion
                    }
                    else // this.viewType == ViewType.Unavailable
                    {
                        #region Buy Button

                        GUI.enabled = EditorLogic.SelectedPart == null && (double)part.partInfo.entryCost <= Funding.Instance.Funds && PartWizard.IsBuyable(part);

                        buyTooltip = GUI.enabled ? string.Format(Localized.BuyPartDescriptionTextFormat, part.partInfo.entryCost) : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.BuyPartButtonText, buyTooltip), out buyButtonMouseOver, Configuration.PartActionButtonWidth))
                        {
                            Log.Write("Buying part {0}.", part.name);

                            PartWizard.Buy(part, true);

                            // Set a flag so additional GUI logic can decide what to do in the case where a part is bought.
                            bought = true;
                        }

                        buyButtonMouseOver &= GUI.enabled;  // Clear mouse over flag if the buy button was disabled.

                        #endregion
                    }

                    GUI.enabled = true;

                    bool groupMouseOver = false;
                    GUIControls.EndMouseOverHorizontal(out groupMouseOver);     // End of row for this part.

                    // If we deleted a part, then just jump out of the loop since the parts list has been modified.
                    if(deleted || bought)
                    {
                        break;
                    }

                    #region Part Highlighting Control

                    if(breakSymmetryMouseOver)
                    {
                        this.highlight.Add(part, Configuration.HighlightColorEditableSymmetryRoot, Configuration.HighlightColorEditableSymmetryCounterparts, true);
                    }
                    else if(deleteButtonMouseOver)
                    {
                        this.highlight.Add(part, Configuration.HighlightColorDeletablePart, Configuration.HighlightColorDeletableCounterparts, true);
                    }
                    else if(buyButtonMouseOver)
                    {
                        // TODO: Duplicate code!
                        buyableParts.ForEach((p) => {
                            if(part.name == p.name)
                            {
                                this.highlight.Add(p, Configuration.HighlightColorBuyablePart);
                            }
                        });
                    }
                    else if(groupMouseOver)
                    {
                        if(viewType != ViewType.Unavailable)
                        {
                            Color highlightColor = (part == EditorLogic.RootPart) ? Configuration.HighlightColorRootPart : Configuration.HighlightColorSinglePart;
                            Color counterpartHighlightColor = Configuration.HighlightColorCounterparts;

                            if(EditorLogic.fetch.editorScreen == EditorScreen.Actions)
                            {
                                highlightColor = Configuration.HighlightColorActionEditorTarget;
                                counterpartHighlightColor = Configuration.HighlightColorActionEditorTarget;
                            }

                            this.highlight.Add(part, highlightColor, counterpartHighlightColor, false);
                        }
                        else
                        {
                            // TODO: Duplicate code!
                            buyableParts.ForEach((p) => {
                                if(part.name == p.name)
                                {
                                    this.highlight.Add(p, Configuration.HighlightColorBuyablePart, false);
                                }
                            });
                        }
                    }
                    else if(actionEditorPartButtonMouseOver)
                    {
                        this.highlight.Add(part, Configuration.HighlightColorActionEditorTarget, Configuration.HighlightColorActionEditorTarget);
                    }

                    #endregion
                }
                    
                GUILayout.EndScrollView();

                GUILayout.EndVertical();

                if(viewType == ViewType.Unavailable)
                {
                    int buyableEntryCost = 0;
                    bool enableBulkBuy = false;

                    foreach(Part p in parts)
                    {
                        buyableEntryCost += p.partInfo.entryCost;

                        enableBulkBuy |= PartWizard.IsBuyable(p);
                    }

                    GUI.enabled = parts.Count > 0 && (double)buyableEntryCost <= Funding.Instance.Funds && enableBulkBuy;

                    bool buyAllMouseOver = false;
                    if(GUIControls.MouseOverButton(new GUIContent(string.Format(Localized.BuyAllButtonTextFormat, buyableEntryCost)), out buyAllMouseOver))
                    {
                        foreach(Part part in parts)
                        {
                            Log.Write("Buying part {0}.", part.name);
                            
                            PartWizard.Buy(part);
                        }

                        PartWizard.SaveGame();
                    }

                    // TODO: Highlight all parts that will be bought by clicking Buy All.
                    if(buyAllMouseOver)
                    {
                        buyableParts.ForEach((p) => {
                            this.highlight.Add(p, Configuration.HighlightColorBuyablePart);
                        });
                    }

                    GUI.enabled = true;
                }

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

                if(this.Visible && this.mouseOver)
                {
                    this.highlight.EndTracking();
                }
                else
                {
                    this.highlight.CancelTracking();
                }
            }
            catch(Exception e)
            {
                Log.Write("PartWizardWindow.OnRender() unexpected exception caught.");

                Log.Write(e.Message);
                Log.Write(e.StackTrace);

                this.highlight.CancelTracking();

                throw;
            }
            finally
            {
                GUI.DragWindow();
            }
        }
    }
}

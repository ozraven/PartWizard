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
#endif

    internal sealed class SymmetryEditorWindow : GUIWindow
    {
        private const float DefaultWidth = 300;
        private static readonly Rect DefaultDimensions = new Rect(Screen.width - DefaultWidth, 160, DefaultWidth, 400);
        private static readonly Rect MinimumDimensions = new Rect(0, 0, DefaultDimensions.width, DefaultDimensions.height);
        
        private Part part;

        private Vector2 scrollPosition;

        private List<PartGroup> symmetryGroups;

        private HighlightTracker highlight;

        private static readonly GUIContent RemoveGroupButtonText = new GUIContent(Localized.RemoveGroupButtonText);
        private static readonly GUIContent MoveDownButtonText = new GUIContent(Localized.DownButtonSymbol);
        private static readonly GUIContent MoveUpButtonText = new GUIContent(Localized.UpButtonSymbol);
        private static readonly GUIContent AddGroupButtonText = new GUIContent(Localized.AddGroupButtonText);

        public SymmetryEditorWindow()
            : base(Scene.Editor, SymmetryEditorWindow.DefaultDimensions, SymmetryEditorWindow.MinimumDimensions, Localized.SymmetryEditor, "SYMMETRY_EDITOR_WINDOW")
        {
            this.highlight = new HighlightTracker();

            this.symmetryGroups = new List<PartGroup>();
        }

        public Part Part
        {
            get
            {
                return this.part;
            }
            set
            {
                this.highlight.CancelTracking();

                this.part = value;

                if(this.part != null)
                {
                    this.symmetryGroups.Clear();

                    this.symmetryGroups.Add(new PartGroup(this.part));

                    foreach(Part counterpart in this.part.symmetryCounterparts)
                    {
                        this.symmetryGroups.Add(new PartGroup(counterpart));
                    }

#if DEBUG
                    Log.WriteSymmetryReport(this.part);
#endif
                }
                else
                {
                    this.Hide();
                }
            }
        }

        public override void Hide()
        {
            this.part = null;
            this.symmetryGroups.Clear();

            base.Hide();
        }

        public override void OnRender()
        {
            try
            {
                GUILayout.BeginVertical();

                this.scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

                this.highlight.BeginTracking();

                if(this.mouseOver)
                {
                    this.highlight.Add(this.part, Configuration.HighlightColorSymmetryEditor, Configuration.HighlightColorSymmetryEditor);
                }

                for(int index = 0; index < this.symmetryGroups.Count; index++)
                {
                    PartGroup group = this.symmetryGroups[index];

                    GUIControls.BeginMouseOverVertical(GUIControls.PanelStyle);

                    GUILayout.BeginHorizontal();
                        
                    GUILayout.Label(new GUIContent(string.Format(CultureInfo.CurrentCulture, Localized.GroupLabelText, index + 1)));

                    // Don't allow group removal if there is only one group.
                    GUI.enabled = (this.symmetryGroups.Count > 1);

                    if(GUILayout.Button(SymmetryEditorWindow.RemoveGroupButtonText))
                    {
                        // If there's a group above, use it. If not, then use the one below.
                        PartGroup destinationGroup = (index > 0) ? this.symmetryGroups[index - 1] : this.symmetryGroups[index + 1];

                        destinationGroup.MergeFrom(group);

                        this.symmetryGroups.Remove(group);

                        break;
                    }

                    GUILayout.EndHorizontal();
                        
                    GUI.enabled = true;

                    bool mouseOverPart = false;

                    foreach(Part groupPart in group.Parts)
                    {
                        GUIControls.BeginMouseOverHorizontal();

                        GUILayout.Label(new GUIContent(groupPart.partInfo.title));

                        GUI.enabled = index < this.symmetryGroups.Count - 1;

                        if(GUILayout.Button(SymmetryEditorWindow.MoveDownButtonText, Configuration.PartActionButtonWidth))
                        {
                            PartGroup nextGroup = this.symmetryGroups[index + 1];

                            group.MoveTo(groupPart, nextGroup);

                            break;
                        }

                        GUI.enabled = index > 0;

                        if(GUILayout.Button(SymmetryEditorWindow.MoveUpButtonText, Configuration.PartActionButtonWidth))
                        {
                            PartGroup previousGroup = this.symmetryGroups[index - 1];

                            group.MoveTo(groupPart, previousGroup);

                            break;
                        }

                        GUI.enabled = true;

                        bool mouseOverPartArea = false;
                        GUIControls.EndMouseOverVertical(out mouseOverPartArea);

                        if(mouseOverPartArea)
                        {
                            this.highlight.Add(group, Configuration.HighlightColorCounterparts);
                            this.highlight.Add(groupPart, Configuration.HighlightColorSinglePart);

                            mouseOverPart = true;
                        }
                    }

                    bool groupMouseOver = false;
                    GUIControls.EndMouseOverVertical(out groupMouseOver);

                    if(!mouseOverPart && groupMouseOver)
                    {
                        this.highlight.Add(group, Configuration.HighlightColorEditableSymmetryCounterparts);
                    }
                }

                // Enable the Add Group button only if there is enough symmetrical parts to fill it.
                GUI.enabled = (this.symmetryGroups.Count < (this.part.symmetryCounterparts.Count + 1));

                if(GUILayout.Button(SymmetryEditorWindow.AddGroupButtonText))
                {
                    this.symmetryGroups.Add(new PartGroup());
                }

                GUI.enabled = true;

                GUILayout.EndScrollView();

                GUILayout.Space(4);

                GUILayout.BeginHorizontal();

                #region OK Button

                if(GUILayout.Button(Localized.OK))
                {
                    int symmetricGroupsCreated = 0;
                    int partsProcessed = 0;

                    foreach(PartGroup group in this.symmetryGroups)
                    {
                        if(group.Parts.Count > 0)
                        {
                            partsProcessed += group.Count;

                            Part symmetricRoot = group.Extract(0);

                            PartWizard.CreateSymmetry(symmetricRoot, group.Parts);

                            symmetricGroupsCreated++;

#if DEBUG
                            Log.WriteSymmetryReport(symmetricRoot);
#endif
                        }
                    }

                    Log.Write("Modified symmetry for {0}, creating {1} symmetric group(s) from {2} parts.", part.name, symmetricGroupsCreated, partsProcessed);
                        
                    this.Hide();
                }

                #endregion

                #region Cancel Button

                if(GUILayout.Button(Localized.Cancel))
                {
                    this.Hide();
                }

                #endregion

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
            catch(Exception)
            {
                highlight.CancelTracking();

                throw;
            }
            finally
            {
                GUI.DragWindow();

                if(this.visible && this.mouseOver)
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

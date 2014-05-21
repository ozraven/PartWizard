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
    internal sealed class SymmetryEditorWindow : Window
    {
        private const float DefaultX = 500;
        private const float DefaultY = 200;
        private const float DefaultWidth = 350;
        private const float DefaultHeight = 400;

        private Part part;

        private Vector2 scrollPosition;

        private List<PartGroup> symmetryGroups;

        private HighlightTracker highlight;

        public SymmetryEditorWindow()
            : base(Scene.Editor, new Rect(DefaultX, DefaultY, DefaultWidth, DefaultHeight), "NO PART", "SYMMETRY_EDITOR_WINDOW")
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
                    this.Title = string.Format(CultureInfo.CurrentCulture, Localized.SymmetryEditor, this.part.partInfo.title, this.part.partName);

                    this.symmetryGroups.Clear();

                    this.symmetryGroups.Add(new PartGroup(this.part));

                    foreach(Part counterpart in this.part.symmetryCounterparts)
                    {
                        this.symmetryGroups.Add(new PartGroup(counterpart));
                    }
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

        bool error = false;

        public override void OnRender()
        {
            // TODO: Move error failsafe to base class.
            // TODO: Make two renders, one for normal and one for the error mode.
            if(!error)
            {
                try
                {
                    GUILayout.BeginVertical();

                    this.scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);

                    this.highlight.BeginTracking();

                    if(this.mouseOver)
                    {
                        this.highlight.Add(this.part, Color.cyan, Color.cyan);
                    }

                    for(int index = 0; index < this.symmetryGroups.Count; index++)
                    {
                        PartGroup group = this.symmetryGroups[index];

                        GUIControls.BeginMouseOverVertical();

                        GUILayout.BeginHorizontal();
                        
                        GUILayout.Label(new GUIContent(string.Format("Group {0}", index + 1)));

                        // Don't allow group removal if there is only one group.
                        GUI.enabled = (this.symmetryGroups.Count > 1);

                        if(GUILayout.Button(new GUIContent("Remove", "Remove group")))
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

                            if(GUILayout.Button(new GUIContent("\\/", "Move Down"), Configuration.PartActionButtonWidth))
                            {
                                PartGroup nextGroup = this.symmetryGroups[index + 1];

                                group.MoveTo(groupPart, nextGroup);

                                break;
                            }

                            GUI.enabled = index > 0;

                            if(GUILayout.Button(new GUIContent("/\\", "Move Up"), Configuration.PartActionButtonWidth))
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
                                this.highlight.Add(group, Color.blue);
                                this.highlight.Add(groupPart, Color.green);

                                mouseOverPart = true;
                            }
                        }

                        bool groupMouseOver = false;
                        GUIControls.EndMouseOverVertical(out groupMouseOver);

                        if(!mouseOverPart && groupMouseOver)
                        {
                            this.highlight.Add(group, Color.magenta);
                        }
                    }

                    // Enable the Add Group button only if there is enough symmetrical parts to fill it.
                    GUI.enabled = (this.symmetryGroups.Count < (this.part.symmetryCounterparts.Count + 1));

                    if(GUILayout.Button(new GUIContent("Add Group")))
                    {
                        this.symmetryGroups.Add(new PartGroup());
                    }

                    GUI.enabled = true;

                    GUILayout.EndScrollView();

                    GUILayout.Space(4);

                    GUILayout.BeginHorizontal();

                    if(GUILayout.Button("OK"))
                    {
                        foreach(PartGroup group in this.symmetryGroups)
                        {
                            if(group.Parts.Count > 0)
                            {
                                Part symmetricRoot = group.Extract(0);

                                PartWizard.CreateSymmetry(symmetricRoot, group.Parts);
                            }
                        }

                        this.Hide();
                    }

                    if(GUILayout.Button("Cancel"))
                    {
                        this.Hide();
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }
                catch(Exception e)
                {
                    error = true;

                    Debug.LogException(e);
                    
                    throw;
                }
                finally
                {
                    GUI.DragWindow();

                    if(!error && this.visible && this.mouseOver)
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
}

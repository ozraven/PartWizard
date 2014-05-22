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

using UnityEngine;

namespace PartWizard
{
#if TEST
    using Part = global::PartWizard.Test.MockPart;
#endif
    
    // TODO: Can interference from other plugins that change highlighting be avoided by tracking our GUI's layers, or by other means?
    internal sealed class HighlightTracker
    {
        #region Global Part Highlight Tracking

        private class Pair<T> where T : new()
        {
            public T Left;
            public T Right;

            public Pair()
            {
                this.Left = new T();
                this.Right = new T();
            }

            public Pair(T left, T right)
            {
                this.Left = left;
                this.Right = right;
            }

            public void Swap()
            {
                T temp = this.Left;
                this.Left = this.Right;
                this.Right = temp;
            }
        }

        private static int nextInstance = 0;
        private static Dictionary<int, Pair<Dictionary<Part, HighlightInfo>>> instanceParts = new Dictionary<int, Pair<Dictionary<Part, HighlightInfo>>>();

        #endregion

        private class HighlightInfo
        {
            public class HighlightState
            {
                public Color HighlightColor;
                public bool HighlightRecursive;

                public HighlightState(Color highlightColor, bool highlightRecursive)
                {
                    this.HighlightColor = highlightColor;
                    this.HighlightRecursive = highlightRecursive;
                }
            }

            public HighlightState Original;

            public HighlightInfo(Color originalHighlightColor)
                : this(originalHighlightColor, false)
            {
            }

            public HighlightInfo(Color originalHighlightColor, bool originalHighlightRecursive)
            {
                this.Original = new HighlightState(originalHighlightColor, originalHighlightRecursive);
            }
        }

        private int instance;
        private volatile bool tracking;

        #region Private Logic Simplifiers

        private Dictionary<Part, HighlightInfo> Parts
        {
            get
            {
                return HighlightTracker.instanceParts[this.instance].Left;
            }
        }

        private Dictionary<Part, HighlightInfo> PreviousParts
        {
            get
            {
                return HighlightTracker.instanceParts[this.instance].Right;
            }
        }

        private void Swap()
        {
            HighlightTracker.instanceParts[this.instance].Swap();
        }

        #endregion

        public HighlightTracker()
        {
            this.instance = HighlightTracker.nextInstance++;
            this.tracking = false;

            HighlightTracker.instanceParts.Add(this.instance, new Pair<Dictionary<Part, HighlightInfo>>());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CancelTracking"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EndTracking")]
        public void BeginTracking()
        {
            if(tracking)
                throw new HighlightTrackerException("Highlight tracking may not be started more than once; call EndTracking or CancelTracking.");

            this.tracking = true;
        }

        public void Add(Part part, Color color, Color symmetryColor)
        {
            this.Add(part, color, symmetryColor, false);
        }

        public void Add(Part part, Color color, Color symmetryColor, bool recursive)
        {
            if(!tracking)
                throw new HighlightTrackerException("Highlight tracking must be started before adding parts to track.");

            if(part == null)
                throw new ArgumentNullException("part");

            this.Add(part, color, recursive);

            foreach(Part counterpart in part.symmetryCounterparts)
            {
                this.Add(counterpart, symmetryColor, recursive);
            }
        }

        public void Add(Part part, Color color, bool recursive)
        {
            if(!tracking)
                throw new HighlightTrackerException("Highlight tracking must be started before adding parts to track.");

            if(part == null)
                throw new ArgumentNullException("part");

            if(!this.Parts.ContainsKey(part) && this.PreviousParts.ContainsKey(part))
            {
                HighlightInfo highlightInfo = this.PreviousParts[part];

                this.PreviousParts.Remove(part);

                this.Parts.Add(part, highlightInfo);
            }
            if(!this.Parts.ContainsKey(part) && !this.PreviousParts.ContainsKey(part))
            {
                bool found = false;

                foreach(var globalParts in HighlightTracker.instanceParts)
                {
                    if(globalParts.Key != this.instance)
                    {
                        if(globalParts.Value.Left.ContainsKey(part))
                        {
                            HighlightTracker.Transfer(part, globalParts.Value.Left, this.Parts);

                            found = true;

                            break;
                        }
                        else if(globalParts.Value.Right.ContainsKey(part))
                        {
                            HighlightTracker.Transfer(part, globalParts.Value.Right, this.Parts);

                            found = true;

                            break;
                        }
                    }
                }

                if(!found)
                {
                    HighlightInfo highlightInfo = new HighlightInfo(part.highlightColor, part.highlightRecurse);

                    this.Parts.Add(part, highlightInfo);
                }
            }
            
            // We should have it!
            Log.Assert(this.Parts.ContainsKey(part));

            part.highlightRecurse = recursive;
            part.SetHighlightColor(color);
        }

        public void Add(Part part, Color color)
        {
            this.Add(part, color, false);
        }

        private static void Transfer(Part part, Dictionary<Part, HighlightInfo> source, Dictionary<Part, HighlightInfo> destination)
        {
            HighlightInfo highlightInfo = source[part];
            source.Remove(part);

            destination.Add(part, highlightInfo);
        }

        public void Add(PartGroup group, Color color, bool recursive)
        {
            if(!tracking)
                throw new HighlightTrackerException("Highlight tracking must be started before adding part groups to track.");

            if(group == null)
                throw new ArgumentNullException("group");

            foreach(Part part in group.Parts)
            {
                this.Add(part, color, recursive);
            }
        }

        public void Add(PartGroup group, Color color)
        {
            this.Add(group, color, false);
        }

        public void EndTracking()
        {
            if(!tracking)
                throw new GUIControlsException("Highlight tracking must be started before tracking can be completed.");

            foreach(Part part in this.Parts.Keys)
            {
                part.SetHighlight(true);
            }

            HighlightTracker.Restore(this.PreviousParts);

            PreviousParts.Clear();

            this.Swap();

            this.tracking = false;
        }

        public void CancelTracking()
        {
            this.tracking = false;

            // Eliminate duplicates held in previousParts.
            foreach(Part part in this.Parts.Keys)
            {
                this.PreviousParts.Remove(part);
            }
            
            // Now previousParts and parts have all the parts we need to restore.
            HighlightTracker.Restore(this.Parts);
            HighlightTracker.Restore(this.PreviousParts);
            
            this.Parts.Clear();
            this.PreviousParts.Clear();
        }

        private static void Restore(Dictionary<Part, HighlightInfo> parts)
        {
            foreach(KeyValuePair<Part, HighlightInfo> partHighlightInfo in parts)
            {
                partHighlightInfo.Key.highlightRecurse = partHighlightInfo.Value.Original.HighlightRecursive;
                partHighlightInfo.Key.SetHighlightColor(partHighlightInfo.Value.Original.HighlightColor);
                partHighlightInfo.Key.SetHighlight(false);
            }
        }
    }
}

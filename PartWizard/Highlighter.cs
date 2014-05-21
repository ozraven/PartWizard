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

using UnityEngine;

namespace PartWizard
{
#if TEST
    using Part = global::PartWizard.Test.MockPart;
#endif

    internal static class Highlighter
    {
        // TODO: Refactor this class to have less confusing parameter-fu.

        private static Color OriginalColor = Color.clear;
        private static bool OriginalColorCaptured = false;
        public static readonly Color DefaultColor = Color.green;
        public static readonly Color DefaultCounterpartColor = Color.yellow;

        public static void Clear(Part part)
        {
            Highlighter.Set(part, false, true, false);
        }

        public static void Set(Part part, Color color, bool includeCounterparts, bool forceNonRecursive)
        {
            Highlighter.Set(part, color, true, includeCounterparts, forceNonRecursive);
        }

        public static void Set(Part part, bool active, bool includeCounterparts, bool forceNonRecursive)
        {
            Highlighter.Set(part, Highlighter.DefaultColor, active, includeCounterparts, forceNonRecursive);
        }

        private static void Set(Part part, Color activeColor, bool active, bool includeCounterparts, bool forceNonRecursive)
        {
            if(!Highlighter.OriginalColorCaptured)
            {
                Highlighter.OriginalColor = part.highlightColor;
                Highlighter.OriginalColorCaptured = true;
            }

            bool isRecursive = part.highlightRecurse;
            if(forceNonRecursive)
            {
                part.highlightRecurse = false;
            }

            Highlighter.Set(part, activeColor, Highlighter.OriginalColor, active, includeCounterparts);

            if(forceNonRecursive)
            {
                part.highlightRecurse = isRecursive;
            }
        }

        private static void Set(Part part, Color activeColor, Color inactiveColor, bool active, bool includeCounterparts)
        {
            part.SetHighlightColor(active ? activeColor : inactiveColor);
            part.SetHighlight(active);

            if(includeCounterparts && part.symmetryCounterparts != null)
            {
                foreach(Part counterpart in part.symmetryCounterparts)
                {
                    Highlighter.Set(counterpart, Highlighter.DefaultCounterpartColor, Highlighter.OriginalColor, active, false);
                }
            }
        }
        
        private static Color Create(int r, int g, int b, int a)
        {
            return new Color(r / 255, g / 255, b / 255, a / 255);
        }
    }
}

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

#if TEST

using System;
using System.Collections.Generic;

using UnityEngine;

namespace PartWizard.Test
{
    [CLSCompliant(false)]
    public sealed class MockPartInfo
    {
        public PartCategories category;
        public string name;
        public string title;
    }

    [CLSCompliant(false)]
    public abstract class MockPart
    {
        public abstract List<MockPart> children { get; }
        public abstract string name { get; }
        public abstract MockPart parent { get; }
        public abstract List<MockPart> symmetryCounterparts { get; }
        public abstract int symmetryMode { get; set; }
        public abstract uint uid { get; }
        public abstract MockPartInfo partInfo { get; }
        public abstract Color highlightColor { get; }
        public abstract bool highlightRecurse { get; set; }

        public abstract void removeChild(MockPart part);
        public abstract void SetHighlight(bool active);
        public abstract void SetHighlightColor(Color color);

        public static bool operator ==(MockPart a, MockPart b)
        {
            return object.ReferenceEquals(a, b);
        }

        public static bool operator !=(MockPart a, MockPart b)
        {
            return !object.ReferenceEquals(a, b);
        }

        public override bool Equals(object obj)
        {
            return object.Equals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

#endif
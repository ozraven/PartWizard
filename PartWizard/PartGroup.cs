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
    /// <summary>
    /// A collection of parts with some convenient methods for managing several PartGroup objects.
    /// </summary>
    internal class PartGroup
    {
        public readonly List<Part> Parts;

        public PartGroup()
        {
            this.Parts = new List<Part>();
        }

        public PartGroup(Part part)
            : this()
        {
            if(part == null)
                throw new ArgumentNullException("part");

            this.Parts.Add(part);
        }

        public int Count
        {
            get
            {
                return this.Parts.Count;
            }
        }

        public void MoveTo(Part part, PartGroup destination)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            if(destination == null)
                throw new ArgumentNullException("destination");

            if(!object.ReferenceEquals(this, destination))
            {
                this.Parts.Remove(part);
                destination.Parts.Add(part);
            }
        }

        public void MergeFrom(PartGroup source)
        {
            if(source == null)
                throw new ArgumentNullException("source");

            if(!object.ReferenceEquals(this, source))
            {
                this.Parts.AddRange(source.Parts);
                source.Parts.Clear();
            }
        }

        public Part Extract(int index)
        {
            Part result = this.Parts[index];

            this.Parts.RemoveAt(index);

            return result;
        }
    }
}

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

namespace PartWizard.Test
{
    internal sealed class MockEditorLogic
    {
        private static MockEditorLogic instance;

        public MockPart PartSelected
        {
            get
            {
                throw new MockIncompleteException("MockEditorLogic.PartSelected_get property incomplete; caller not intended for automated testing.");
            }
            set
            {
                throw new MockIncompleteException("MockEditorLogic.PartSelected_set property incomplete; caller not intended for automated testing.");
            }
        }

        public MockShipConstruct ship
        {
            get
            {
                throw new MockIncompleteException("MockEditorLogic.ship_get property incomplete; caller not intended for automated testing.");
            }
        }

        public void DestroySelectedPart()
        {
            throw new MockIncompleteException("MockEditorLogic.DestroySelectedPart() method incomplete; caller not intended for automated testing.");
        }

        public static MockEditorLogic fetch
        {
            get
            {
                if(instance == null)
                {
                    instance = new MockEditorLogic();
                }

                return instance;
            }
        }

        public static MockPart SelectedPart
        {
            get
            {
                return MockEditorLogic.fetch.PartSelected;
            }
            set
            {
                MockEditorLogic.fetch.PartSelected = value;
            }
        }

        public static MockPart startPod
        {
            get
            {
                throw new MockIncompleteException("MockEditorLogic.startPod_get property incomplete; caller not intended for automated testing.");
            }
        }
    }
}

#endif
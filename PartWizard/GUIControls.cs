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
    internal static class GUIControls
    {
        private static int titleBarButtonCount = 0;
        private static bool layoutStarted = false;

        public static readonly GUIStyle PanelStyle = new GUIStyle("box");
        public static readonly GUILayoutOption LockWidth = GUILayout.ExpandWidth(false);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "EndLayout")]
        public static void BeginLayout()
        {
            if(GUIControls.layoutStarted)
                throw new GUIControlsException("GUI layout may not be started more than once per window; call EndLayout?");

            GUIControls.layoutStarted = true;
            GUIControls.titleBarButtonCount = 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginLayout")]
        public static void EndLayout()
        {
            if(!GUIControls.layoutStarted)
                throw new GUIControlsException("GUI layout not started, call BeginLayout first.");

            GUIControls.layoutStarted = false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "selectedIndex")]
        public static int HorizontalToggleSet(int selectedIndex, GUIContent[] contents, GUIStyle selectedStyle, GUIStyle unselectedStyle, params GUILayoutOption[] options)
        {
            if(contents == null)
                throw new ArgumentNullException("contents");

            if(selectedIndex < 0 || selectedIndex > contents.Length - 1)
                throw new GUIControlsException("The selectedIndex must be within the range of the contents array.");

            int result = selectedIndex;

            GUILayout.BeginHorizontal();

            for(int index = 0; index < contents.Length; index++)
            {
                GUIStyle activeStyle = null;

                if(selectedStyle != null && unselectedStyle == null)
                {
                    activeStyle = unselectedStyle;
                }
                else if(selectedStyle == null && unselectedStyle != null)
                {
                    activeStyle = selectedStyle;
                }
                else
                {
                    activeStyle = (index == selectedIndex) ? selectedStyle : unselectedStyle;
                }

                bool clicked = GUILayout.Toggle(index == selectedIndex, contents[index], activeStyle, options);

                if(clicked && index != selectedIndex)
                {
                    result = index;
                }
            }

            GUILayout.EndHorizontal();

            return result;
        }

        /// <summary>
        /// Provides a small button that displays in the title bar of the GUILayout window.
        /// </summary>
        /// <param name="window">The Rect structure of the window where the button will be placed.</param>
        /// <returns>True if the button was clicked; false if not.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BeginLayout")]
        public static bool TitleBarButton(Rect window)
        {
            if(!GUIControls.layoutStarted)
                throw new GUIControlsException("GUI layout must be started before adding title bar buttons, call BeginLayout first.");

            const float TitleBarIconSpacing = 20;
            const float TitleBarIconPadding = 2;
            const float TitleBarIconY = 3;
            const float TitleBarIconWidth = 12;
            const float TitleBarIconHeight = 12;

            bool result = false;

            GUIControls.titleBarButtonCount++;

            float x = window.width - ((TitleBarIconSpacing * GUIControls.titleBarButtonCount) + TitleBarIconPadding);
            result = GUI.Button(new Rect(x, TitleBarIconY, TitleBarIconWidth, TitleBarIconHeight), default(string));

            return result;
        }
        
        /// <summary>
        /// Provides a GUILayout button control that can detect if the mouse is within its area.
        /// </summary>
        /// <param name="content">The content to display in the button.</param>
        /// <param name="mouseOver">Set to true if the mouse is over this button's area; false if not.</param>
        /// <param name="options">The usual GUILayoutOption parameters; see Unity documentation.</param>
        /// <returns>True if the button was clicked; false if not.</returns>
        public static bool MouseOverButton(GUIContent content, out bool mouseOver, params GUILayoutOption[] options)
        {
            bool result = GUILayout.Button(content, options);

            mouseOver = false;

            if(Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                mouseOver = true;
            }

            return result;
        }
        
        /// <summary>
        /// Provides a horizontal GUI layout area that can detect if the mouse is within its area.
        /// </summary>
        /// <param name="options">The usual GUILayoutOption parameters, see Unity documentation.</param>
        public static void BeginMouseOverHorizontal(params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
        }

        /// <summary>
        /// Completes a horizontal GUI layout area started by <see cref="BeginMouseOverHorizontal"/>.
        /// </summary>
        /// <param name="mouseOver">Set to true if the mouse is over this area; false if not.</param>
        public static void EndMouseOverHorizontal(out bool mouseOver)
        {
            mouseOver = false;

            GUILayout.EndHorizontal();

            if(Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                mouseOver = true;
            }
        }

        public static void BeginMouseOverVertical(GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(style, options);
        }

        public static void EndMouseOverVertical(out bool mouseOver)
        {
            mouseOver = false;

            GUILayout.EndVertical();

            if(Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                mouseOver = true;
            }
        }

        /// <summary>
        /// Determines if the mouse is over the specified window.
        /// </summary>
        /// <param name="window">The Rect of the window to check for mouse over status.</param>
        /// <returns>True if the mouse cursor is within the windows boundaries; false if not.</returns>
        public static bool MouseOverWindow(ref Rect window)
        {
            bool result = false;

            if(Event.current.type == EventType.Repaint && Event.current.mousePosition.x >= window.x && Event.current.mousePosition.x < window.x + window.width &&
                Event.current.mousePosition.y >= window.y && Event.current.mousePosition.y < window.y + window.height)
            {
                result = true;
            }

            return result;
        }
    }
}

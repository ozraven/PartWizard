using System;

using UnityEngine;

namespace PartWizard
{
    internal static class GUIControls
    {
        private static int titleBarButtonCount = 0;
        private static bool layoutStarted = false;

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

            bool result = false;

            GUIControls.titleBarButtonCount++;

            // TODO: Magic numbers.
            float x = window.width - ((20 * GUIControls.titleBarButtonCount) + 2);
            result = GUI.Button(new Rect(x, 3, 12, 12), default(string));

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
        /// Provides a GUILayout label control that can detect if the mouse is within its area.
        /// </summary>
        /// <param name="content">The content to display in the label.</param>
        /// <param name="mouseOver">Set to true if the mouse is over this label's area; false if not.</param>
        /// <param name="options">The usual GUILayoutOption parameters; see Unity documentation.</param>
        public static void MouseOverLabel(GUIContent content, out bool mouseOver, params GUILayoutOption[] options)
        {
            GUILayout.Label(content, options);

            mouseOver = false;

            if(Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                mouseOver = true;
            }
        }
    }
}

using System;
using System.Collections.Generic;

using KSP;

using UnityEngine;

namespace PartWizard
{
    internal static class PartWizard
    {
        /// <summary>
        /// Deletes a part.
        /// </summary>
        /// <param name="part">The part to delete.</param>
        public static void Delete(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            if(part.children != null && part.children.Count > 0)
                throw new ArgumentException("Specified part has children and may not be deleted.", "part");

            // First, get the parent part and delete the child part.
            Part parent = part.parent;
            parent.removeChild(part);

            // Second, get the editor and ask it to destroy the part.
            EditorLogic editor = EditorLogic.fetch;
            editor.PartSelected = part;
            editor.DestroySelectedPart();

            // Third, since the part is now dead, clear the selected part.
            // TODO: Test to see if this is necessary.
            editor.PartSelected = null;

            // Finally, poke the staging logic to sort out any changes due to deleting this part.
            Staging.SortIcons();
        }

        /// <summary>
        /// Breaks the symmetry of a part and all of its child parts.
        /// </summary>
        /// <param name="part">The part with symmetry to break.</param>
        public static void BreakSymmetry(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            Part symmetryRootPart = part;

            // If we don't have the symmetry root part, we need to find it.
            if(symmetryRootPart.symmetryMode != 0)
            {
                foreach(Part counterpart in symmetryRootPart.symmetryCounterparts)
                {
                    if(counterpart.symmetryMode == 0)
                    {
                        symmetryRootPart = counterpart;
                        break;
                    }
                }
            }

            // Get the prototype part's list of symmetry counterparts because each of them needs updated to break
            // symmetry.
            List<Part> counterparts = symmetryRootPart.symmetryCounterparts;

            // Begin breaking symmetry on each counterpart:
            foreach(Part counterpart in counterparts)
            {
                // Clear the properties that declare symmetry.
                counterpart.symmetryMode = 0;
                counterpart.symmetryCounterparts.Clear();

                // We must break symmetry on all children too, because otherwise the editor will be confused and let the user do
                // some odd things with part placement involving the parts that still have symmetry.
                foreach(Part childPart in counterpart.children)
                {
                    PartWizard.BreakSymmetry(childPart);
                }
            }

            // Now break symmetry on the symmetry root part and all if it's children.
            symmetryRootPart.symmetryCounterparts.Clear();

            foreach(Part childPart in symmetryRootPart.children)
            {
                PartWizard.BreakSymmetry(childPart);
            }

            // Finally, poke the staging logic to sort out any changes due to breaking the symmetry of this part.
            Staging.SortIcons();
        }

        /// <summary>
        /// Determines if a part is the "root" of a symmetrical part family.
        /// </summary>
        /// <param name="part">The part to test for being the "root."</param>
        /// <returns>True if the part is the symmetrical root, false if it is not.</returns>
        public static bool IsSymmetricalRoot(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            bool result = false;

            if(part.symmetryCounterparts.Count > 0)
            {
                if(part.parent != null)
                {
                    result = (part.parent.symmetryCounterparts.Count == 0);
                }
            }

            return result;
        }
    }
}

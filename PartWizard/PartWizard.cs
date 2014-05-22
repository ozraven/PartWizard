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

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
#if TEST
    using Part = global::PartWizard.Test.MockPart;
    using EditorLogic = global::PartWizard.Test.MockEditorLogic;
    using Staging = global::PartWizard.Test.MockStaging;
#endif

    /// <summary>
    /// Provides the addon's part manipulation capabilities.
    /// </summary>
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

#if DEBUG
        public static Part FindSymmetryRoot(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            Part result = part;

            // If we don't have the symmetry root part, we need to find it.
            if(result.symmetryMode != 0)
            {
                foreach(Part counterpart in result.symmetryCounterparts)
                {
                    if(counterpart.symmetryMode == 0)
                    {
                        result = counterpart;
                        break;
                    }
                }
            }

            return result;
        }
#endif

        public static void CreateSymmetry(Part symmetricRoot, List<Part> counterparts)
        {
            if(symmetricRoot == null)
                throw new ArgumentNullException("symmetricRoot");

            if(counterparts == null)
                throw new ArgumentNullException("counterparts");

            // The symmetric root's symmetryMode is always zero.
            symmetricRoot.symmetryMode = 0;

            // symmetryMode appears to be set to the number of counterparts.
            int prelinarySymmetryMode = counterparts.Count;

            // Clear out the current list of counterparts, we'll recreate this as we go along.
            symmetricRoot.symmetryCounterparts.Clear();
            
            // Update all the counterparts...
            for(int index = 0; index < counterparts.Count; index++)
            {
                Part counterpart = counterparts[index];

                // Each counterpart must be added to the symmetric root's list.
                symmetricRoot.symmetryCounterparts.Add(counterpart);
                
                // Clear the counterpart's list.
                counterpart.symmetryCounterparts.Clear();
                // Set the appropriate symmetry mode.
                counterpart.symmetryMode = prelinarySymmetryMode;

                // Add the symmetrical root part as a counterpart.
                counterpart.symmetryCounterparts.Add(symmetricRoot);
                
                // Add all the *other* counterparts.
                for(int siblingIndex = 0; siblingIndex < counterparts.Count; siblingIndex++)
                {
                    if(index != siblingIndex)
                    {
                        counterpart.symmetryCounterparts.Add(counterparts[siblingIndex]);
                    }
                }
            }

            // Now we must deal with the children...if they have symmetry.
            foreach(Part child in symmetricRoot.children)
            {
                if(PartWizard.HasSymmetry(child))
                {
                    // A list to hold the relevant counterparts. We only care about the counterparts that descend from the counterparts we've been given.
                    List<Part> childSymmetricCounterparts = new List<Part>();

                    // Look through all the child's counterparts to find the ones relevant to our counterparts...
                    foreach(Part childSymmetricCounterpart in child.symmetryCounterparts)
                    {
                        if(counterparts.Contains(childSymmetricCounterpart.parent))
                        {
                            // ...and add those to the list.
                            childSymmetricCounterparts.Add(childSymmetricCounterpart);
                        }
                    }

                    // We now have a part and a list of parts to make symmetrical, which we already know how to do!
                    PartWizard.CreateSymmetry(child, childSymmetricCounterparts);
                }
            }
        }

        public static bool IsSibling(Part part, Part family)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            if(family == null)
                throw new ArgumentNullException("family");

            bool result = object.ReferenceEquals(part, family);

            if(!result)
            {
                foreach(Part sibling in family.symmetryCounterparts)
                {
                    result = object.ReferenceEquals(part, sibling);

                    if(result)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Quickly determines if a part has symmetry.
        /// </summary>
        /// <param name="part">The part to test for having symmetry.</param>
        /// <returns>True if the part has symmetrical counterparts, false if not.</returns>
        public static bool HasSymmetry(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            return part.symmetryCounterparts.Count > 0;
        }
                                
        /// <summary>
        /// Determines if a part is deleteable.
        /// </summary>
        /// <param name="part">The part to test for deletability.</param>
        /// <returns>True if the part can be deleted, false if not.</returns>
        /// <remarks>
        /// This method attempts to check for all the reasons a part should not be deleted.
        /// <list type="bullet">
        ///     <listheader>
        ///         <rule>Rule</rule>
        ///         <explanation>Explanation</explanation>
        ///     </listheader>
        ///     <item>
        ///         <rule>The part must not be the root part.</rule>
        ///         <explanation>The root part is given at least some special consideration by the editor that is not handled by the Delete method.</explanation>
        ///     </item>
        ///     <item>
        ///         <rule>The part must not have any child parts attached to it.</rule>
        ///         <explanation>
        ///         Deleting this part would require "stitching" the part's parent part and all of its children, which would be nearly impossible to do
        ///         automatically.
        ///         </explanation>
        ///     </item>
        ///     <item>
        ///         <rule>If the part is symmetrical, it must be eligible for having its symmetry broken.</rule>
        ///         <explanation>See <see cref="HasBreakableSymmetry"/> for details.</explanation>
        ///     </item>
        /// </list>
        /// </remarks>
        public static bool IsDeleteable(Part part)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            string report = default(string);
            return part.parent != null && (part.children.Count == 0) && (!PartWizard.HasSymmetry(part) || PartWizard.HasBreakableSymmetry(part, out report));
        }

        /// <summary>
        /// Determines if any of a part's symmetrical counterparts are also descendants.
        /// </summary>
        /// <param name="part">The part to examine.</param>
        /// <returns>True if any of the part's symmetrical counterparts are descended from part.</returns>
        private static bool HasChildCounterparts(Part part)
        {
            bool result = false;

            foreach(Part counterpart in part.symmetryCounterparts)
            {
                if(PartWizard.IsDescendant(part, counterpart))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if any of a part's symmetrical counterparts are also ancestors.
        /// </summary>
        /// <param name="part">The part to examine.</param>
        /// <returns>True if any of the part's symmetrical counterparts are an ancestor.</returns>
        private static bool HasAncestralCounterpart(Part part)
        {
            bool result = false;

            foreach(Part counterpart in part.symmetryCounterparts)
            {
                if(result = (PartWizard.IsAncestor(part, counterpart)))
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a part is the ancestor of another part.
        /// </summary>
        /// <param name="part">The child part of a lineage to search for the possibleAncestor part.</param>
        /// <param name="possibleAncestor">The part to search for within a lineage.</param>
        /// <returns>True if the possibleAncestor is an ancestor of part, false if not.</returns>
        private static bool IsAncestor(Part part, Part possibleAncestor)
        {
            bool result = false;

            if(part.parent != null)
            {
                if(!(result = (part.uid == possibleAncestor.uid)))
                {
                    result = PartWizard.IsAncestor(part.parent, possibleAncestor);
                }
            }

            return result;
        }
        
        /// <summary>
        /// Determines if a part is descended from another part.
        /// </summary>
        /// <param name="part">The parent part of a lineage to search for the possibleDescendant part.</param>
        /// <param name="possibleDescendant">The part to search for within a lineage.</param>
        /// <returns>True if the possibleDescendant is a descendant of part, false if not.</returns>
        private static bool IsDescendant(Part part, Part possibleDescendant)
        {
            bool result = false;

            foreach(Part childPart in part.children)
            {
                if(childPart.uid == possibleDescendant.uid)
                {
                    result = true;
                    break;
                }
                else if(PartWizard.IsDescendant(childPart, possibleDescendant))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a part has breakable symmetry.
        /// </summary>
        /// <param name="part">The part to test for breakable symmetry.</param>
        /// <param name="report">A string containing a short rationale for the breakability result.</param>
        /// <returns>True if the part has symmetry that can be broken, false if not.</returns>
        /// <remarks>
        /// This method attempts to check for all the reasons a symmetrical part should not have its symmetry broken.
        /// <list type="bullet">
        ///     <listheader>
        ///         <rule>Rule</rule>
        ///         <explanation>Explanation</explanation>
        ///     </listheader>
        ///     <item>
        ///         <rule>The part must have symmetry.</rule>
        ///         <explanation>A part cannot have symmetry broken if it is not symmetrical with at least one other part.</explanation>
        ///     </item>
        ///     <item>
        ///         <rule>The symmetrical counterparts must not be descendants of the part.</rule>
        ///         <explanation>
        ///         This would create a circular reference during the breaking process. This can happen if one of the symmetrical parts or one of its descendants
        ///         has been made the root part.
        ///         </explanation>
        ///     </item>
        ///     <item>
        ///         <rule>The symmetrical counterparts must not be ancestors of the part.</rule>
        ///         <explanation>
        ///         This would create a logical error in symmetrical breakability where one counterpart may be determined breakable while another may not, based on
        ///         the rule set. This can happen if one of the symmetrical parts or one of its descendants has been made the root part.
        ///         </explanation>
        ///     </item>
        ///     <item>
        ///         <rule>The part, or one of its counterparts, must not also be the root part.</rule>
        ///         <explanation>
        ///         The symmetry breaking process requires that all child parts also have their symmetry broken. The root part's child parts include all
        ///         of the parts of a vessel, including parts that are not descended from the symmetrical root. Symmetry can not be reliable determined
        ///         due to the root part not having a parent, and as a rule, symmetrical parts share a common parent - either the same part ("symmetrical root")
        ///         or each is the child of an identical counterpart.
        ///         </explanation>
        ///     </item>
        ///     <item>
        ///         <rule>A child part must either not have symmetry or have breakable symmetry.</rule>
        ///         <explanation>
        ///         In the case where a non-symmetrical part was added as a child, symmetry is breakable. However, if a symmetrical part is added as a child, it must
        ///         be breakable due to the requirement that all symmetrical descendants of a part have their symmetry broken.
        ///         </explanation>
        ///     </item>
        /// </list>
        /// </remarks>
        public static bool HasBreakableSymmetry(Part part, out string report)
        {
            if(part == null)
                throw new ArgumentNullException("part");

            bool result = false;
            report = default(string);

            if(result = (PartWizard.HasSymmetry(part)))
            {
                if(result = !PartWizard.HasChildCounterparts(part))
                {
                    if(result = !PartWizard.HasAncestralCounterpart(part))
                    {
                        if(result = (part.parent != null))
                        {
                            foreach(Part childPart in part.children)
                            {
                                string internalBreakabilityReport = default(string);
                                if(!(result = (!PartWizard.HasSymmetry(childPart) || PartWizard.HasBreakableSymmetry(childPart, out internalBreakabilityReport))))
                                {
                                    report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableChildNotBreakable, part.name, childPart.name);

                                    break;
                                }
                            }

                            if(result)
                            {
                                foreach(Part counterpart in part.symmetryCounterparts)
                                {
                                    if(!(result = (counterpart.parent != null)))
                                    {
                                        report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableCounterpartHasNoParent, part.name, counterpart.name);

                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableNoParent, part.name);
                        }
                    }
                    else
                    {
                        report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableHasAncestralCounterpart, part.name);
                    }
                }
                else
                {
                    report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableHasDescendantCounterpart, part.name);
                }
            }
            else
            {
                report = string.Format(CultureInfo.CurrentCulture, Localized.NotBreakableNotSymmetrical, part.name);
            }

            if(result)
            {
                report = string.Format(CultureInfo.CurrentCulture, Localized.Breakable, part.name);
            }

            return result;
        }
    }
}

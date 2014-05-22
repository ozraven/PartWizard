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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

using UnityEngine;

namespace PartWizard
{
#if TEST
    using Part = global::PartWizard.Test.MockPart;
#endif

    internal static class Log
    {
        private const string Prefix = "[PartWizard]";

        private static readonly DateTime start = DateTime.Now;

        public static void Assert(bool condition)
        {
#if DEBUG || TEST
            if(!condition)
            {
                string message = string.Format(CultureInfo.InvariantCulture, "Assertion failed in {0}.", Log.GetCallingMethod(2));

#if TEST
                throw new Exception(message);
#endif

                Log.Write(message);
            }
#endif
        }

        public static void Assert(bool condition, string format, params object[] args)
        {
#if DEBUG || TEST
            if(!condition)
            {
                string message = string.Format(CultureInfo.InvariantCulture, "Assertion failed in {0}: {1}", Log.GetCallingMethod(2), string.Format(CultureInfo.InvariantCulture, format, args));

#if TEST
                throw new Exception(message);
#endif

                Log.Write(message);
            }
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Trace()
        {
            Log.Write("{0}", Log.GetCallingMethod(2));
        }

        public static void Trace(string format, params object[] args)
        {
            Log.Write("{0} {1}", Log.GetCallingMethod(2), string.Format(CultureInfo.InvariantCulture, format, args));
        }

        private static string GetCallingMethod(int skipCount)
        {
            StackFrame stackFrame = new StackFrame(skipCount);

            MethodBase method = stackFrame.GetMethod();

            return string.Concat(method.DeclaringType, ".", method.Name);
        }

        public static string Format(Rect rect)
        {
            return string.Format(CultureInfo.InvariantCulture, "[({0}, {1}) {2}x{3}]", (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }

        public static string FormatInt32(Vector2 vector)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", (int)vector.x, (int)vector.y);
        }

        public static void Write(string format, params object[] args)
        {
            UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0} {1}", Log.GeneratePrefix(), string.Format(CultureInfo.InvariantCulture, format, args)));
        }

        private static string GeneratePrefix()
        {
            string result = default(string);

            TimeSpan elapsed = DateTime.Now - Log.start;

            // In the event the game has been running for days, handle it especially - days is the highest increment of time handled.
            if(elapsed.TotalDays < 1)
            {
                result = string.Format(CultureInfo.InvariantCulture, "{0} [{1:00}:{2:00}:{3:00}.{4:000}]", Log.Prefix, elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
            }
            else
            {
                result = string.Format(CultureInfo.InvariantCulture, "{0} [{1:00}:{2:00}:{3:00}:{4:00}.{5:000}]", Log.Prefix, Convert.ToInt32(Math.Floor(elapsed.TotalDays)), elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
            }

            return result;
        }

        // TODO: PartWizardWindow doesn't seem to be saving/restoring window location properly.
        // TODO: Test to make sure windows appear in sane places when starting without a .cfg file.
        // TODO: Left justify status bar text.
        // TODO: Status bar text needs shorter/window needs wider because getting string truncating on most every message.
        // TODO: Clicking Break Symmetry button toggles the Symmetry Editor window; it should open it if hidden, or just switch parts if it is visible.
        // TODO: Test for highlighting problems when in AG mode.

        public static void WriteSymmetryReport(Part part)
        {
#if DEBUG
            Part r = PartWizard.FindSymmetryRoot(part);

            Log.Write("SYMMETRY REPORT FOR {0}", r.name);
            Log.Write("Root:");
            Log.Write("\tname = {0}", r.name);
            Log.Write("\tuid = {0}", r.uid);
            Log.Write("\tsymmetryMode = {0}", r.symmetryMode);
            Log.Write("Counterparts:");
            for(int index = 0; index < r.symmetryCounterparts.Count; index++)
            {
                Part c = r.symmetryCounterparts[index];

                Log.Write("\t{0} name = {1}", index, c.name);
                Log.Write("\t{0} uid = {1}", index, c.uid);
                Log.Write("\t{0} symmetryMode = {1}", index, c.symmetryMode);
                Log.Write("\t{0} children = {1}", index, c.children.Count);
            }
            Log.Write("END OF REPORT");
#endif
        }
    }
}

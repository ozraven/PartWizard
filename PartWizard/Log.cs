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
using System.Text;

using UnityEngine;

namespace PartWizard
{
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
#endif
#if TEST
                throw new Exception(message);
#endif
#if DEBUG && !TEST
                Log.Write(message);
#endif
#if DEBUG || TEST
            }
#endif
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Assert(bool condition, string format, params object[] args)
        {
#if DEBUG || TEST
            if(!condition)
            {
                string message = string.Format(CultureInfo.InvariantCulture, "Assertion failed in {0}: {1}", Log.GetCallingMethod(2), string.Format(CultureInfo.InvariantCulture, format, args));
#endif
#if TEST
                throw new Exception(message);
#endif
#if DEBUG && !TEST
                Log.Write(message);
#endif
#if DEBUG || TEST
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string Format(Rect rect)
        {
            return string.Format(CultureInfo.InvariantCulture, "[({0}, {1}) {2}x{3}]", (int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static string FormatInt32(Vector2 vector)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", (int)vector.x, (int)vector.y);
        }

        public static void Write(string format, params object[] args)
        {
            UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture, "{0} {1}", Log.Prefix, string.Format(CultureInfo.InvariantCulture, format, args)));
        }

        public static void WriteStyleReport(GUIStyle style, string description)
        {
#if DEBUG
            if(style != null)
            {
                Log.Write("STYLE REPORT FOR {0}:", description);
                Log.Write("\tname = {0}", style.name);
                Log.Write("\tnormal.textColor = {0}", Log.ColorToRGB(style.normal.textColor));
                Log.Write("\tonActive.textColor = {0}", Log.ColorToRGB(style.onActive.textColor));
                Log.Write("\tonNormal.textColor = {0}", Log.ColorToRGB(style.onNormal.textColor));
                Log.Write("\tonHover.textColor = {0}", Log.ColorToRGB(style.onHover.textColor));
                Log.Write("END OF STYLE REPORT");
            }
            else
            {
                Log.Write("STYLE REPORT FOR {0}: null", description);
            }
#endif
        }

        private static string ColorToRGB(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", (byte)(Mathf.Clamp01(color.r)), (byte)(Mathf.Clamp01(color.g)), (byte)(Mathf.Clamp01(color.b)));
        }
        
        public static void WriteSymmetryReport(Part part)
        {
#if DEBUG
            Part r = PartWizard.FindSymmetryRoot(part);

            Log.Write("SYMMETRY REPORT FOR {0}", r.name);
            Log.Write("Root:");
            Log.Write("\tname = {0}", r.name);
            Log.Write("\tsymMethod = {0}", r.symMethod);
            Log.Write("\tstackSymmetry = {0}", r.stackSymmetry);
            Log.Write("Counterparts:");
            for(int index = 0; index < r.symmetryCounterparts.Count; index++)
            {
                Part c = r.symmetryCounterparts[index];

                Log.Write("\t{0} name = {1}", index, c.name);
                Log.Write("\t{0} symMethod = {1}", index, c.symMethod);
                Log.Write("\t{0} stackSymmetry = {1}", index, c.stackSymmetry);
                Log.Write("\t{0} children = {1}", index, c.children.Count);
            }
            Log.Write("END OF SYMMETRY REPORT");
#endif // DEBUG
        }

        public static void WriteTransformReport(Part part)
        {
#if DEBUG
            Log.Write("TRANSFORM REPORT FOR {0}", part.name);
            Log.Write("\ttransform = {0}", part.transform != null ? part.transform.name : "<null>");
            Log.Write("\tpartTransform = {0}", part.partTransform != null ? part.partTransform.name : "<null>");
            Transform[] transforms = part.GetComponents<Transform>();
            if(transforms == null)
            {
                Log.Write("\tTransforms: <n/a>");
            }
            else
            {
                Log.Write("\tTransforms:");

                Log.WriteTransformReport(transforms, 2);
            }
            Log.Write("END OF TRANSFORM REPORT");
#endif // DEBUG
        }

        private static void WriteTransformReport(Transform[] transforms, int tabCount)
        {
#if DEBUG
            for(int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                StringBuilder reportLine = new StringBuilder();

                for(int tabIndex = 0; tabIndex < tabCount; tabIndex++)
                {
                    reportLine.Append("\t");    
                }

                Transform transform = transforms[transformIndex];

                reportLine.AppendFormat("{0} name = {1} ({2} children)", transformIndex, transform.name, transform.childCount);

                Log.Write(reportLine.ToString());
                
                if(transform.childCount > 0)
                {
                    Log.WriteTransformReport(transform.GetChildren(), tabCount + 1);
                }
            }
#endif // DEBUG
        }

        private static Transform[] GetChildren(this Transform transform)
        {
#if DEBUG
            Transform[] result = new Transform[transform.childCount];

            for(int index = 0; index < transform.childCount; index++)
            {
                result[index] = transform.GetChild(index);
            }

            return result;
#else
            return new Transform[0];
#endif // DEBUG
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PartWizard
{
    internal static class PartExtensions
    {
        public static string DisplayName(this Part part)
        {
            string result = string.Empty;

            if(part != null)
            {
                result = part.partInfo.title;
            }
            else
            {
                result = "DELETED!";
            }

            return result;
        }        
    }
}

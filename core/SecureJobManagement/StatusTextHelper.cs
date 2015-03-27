//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿namespace ManagementWeb
{
    using System.Collections.Generic;
    using System.Linq;

    public static class StatusTextHelper
    {
        public static IEnumerable<string> LastLines(this string statusText, int length)
        {
            IEnumerable<string> lines = statusText.Split('\n').Where(l => l.Length > 0).ToList();
            if (lines.Count() >= length)
            {
                lines = lines.Skip(lines.Count() - length);
            }

            return lines;
        }
    }
}
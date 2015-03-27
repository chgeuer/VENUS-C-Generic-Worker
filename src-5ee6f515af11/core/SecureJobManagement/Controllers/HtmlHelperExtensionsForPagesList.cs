//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System.Web;

namespace Site.JobManagement.Controllers
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Web.Mvc;

    public static class HtmlHelperExtensionsForPagesList
    {
        public static MvcHtmlString PagedListSizeSelection(this System.Web.Mvc.HtmlHelper html, Func<int, string> generatePageUrl)
        {
            var sizes = new int[] { 5, 10, 15, 20 };

            var listItemLinks = new StringBuilder();
            sizes.Select(size =>
            {
                var a = new TagBuilder("a");
                a.SetInnerText(size.ToString());
                a.Attributes["href"] = generatePageUrl(size);

                return new TagBuilder("li") { InnerHtml = a.ToString() };
            }).ToList().ForEach(x => listItemLinks.Append(x.ToString()));
            
            var ul = new TagBuilder("ul") { InnerHtml = listItemLinks.ToString() };

            var outerDiv = new TagBuilder("div");
            outerDiv.AddCssClass("PagedList-pager");
            outerDiv.InnerHtml = ul.ToString();

            var result = outerDiv.ToString();

            return new MvcHtmlString(result);
        }

        public static MvcHtmlString EscapedWithLinebreaks(this string text)
        {
            return MvcHtmlString.Create(text.Replace("\r\n", "<br/>"));
        }
    }
}
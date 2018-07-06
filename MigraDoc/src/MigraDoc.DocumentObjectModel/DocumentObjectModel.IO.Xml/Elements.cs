using System;
using System.Collections.Generic;
using System.Text;

namespace MigraDoc.DocumentObjectModel.IO.Xml
{
    internal class XmlKeyWords
    {
        static XmlKeyWords()
        {
            EnumToName.Add(XmlSymbol.True, "true");
            EnumToName.Add(XmlSymbol.False, "false");
            //EnumToName.Add(XmlSymbol.Null, "null");

            EnumToName.Add(XmlSymbol.Attributes, "attributes");
            EnumToName.Add(XmlSymbol.Styles, "styles");
            EnumToName.Add(XmlSymbol.Style, "style");
            EnumToName.Add(XmlSymbol.Document, "document");
            EnumToName.Add(XmlSymbol.Sections, "sections");
            EnumToName.Add(XmlSymbol.Section, "section");
            EnumToName.Add(XmlSymbol.Paragraph, "p"); // paragraph
            EnumToName.Add(XmlSymbol.Header, "header");
            EnumToName.Add(XmlSymbol.Footer, "footer");
            EnumToName.Add(XmlSymbol.PrimaryHeader, "primaryheader");
            EnumToName.Add(XmlSymbol.PrimaryFooter, "primaryfooter");
            EnumToName.Add(XmlSymbol.FirstPageHeader, "firstpageheader");
            EnumToName.Add(XmlSymbol.FirstPageFooter, "firstpagefooter");
            EnumToName.Add(XmlSymbol.EvenPageHeader, "evenpageheader");
            EnumToName.Add(XmlSymbol.EvenPageFooter, "evenpagefooter");
            EnumToName.Add(XmlSymbol.Table, "table");
            EnumToName.Add(XmlSymbol.Columns, "columns");
            EnumToName.Add(XmlSymbol.Column, "column");
            EnumToName.Add(XmlSymbol.Rows, "rows");
            EnumToName.Add(XmlSymbol.Row, "row");
            EnumToName.Add(XmlSymbol.Cell, "cell");
            EnumToName.Add(XmlSymbol.Image, "image");
           // EnumToName.Add(XmlSymbol.Text, "text");
            EnumToName.Add(XmlSymbol.TextFrame, "textframe");
            EnumToName.Add(XmlSymbol.PageBreak, "pagebreak");
            EnumToName.Add(XmlSymbol.Barcode, "barcode");
            EnumToName.Add(XmlSymbol.Chart, "chart");
            EnumToName.Add(XmlSymbol.HeaderArea, "headerarea");
            EnumToName.Add(XmlSymbol.FooterArea, "footerarea");
            EnumToName.Add(XmlSymbol.TopArea, "toparea");
            EnumToName.Add(XmlSymbol.BottomArea, "bottomarea");
            EnumToName.Add(XmlSymbol.LeftArea, "leftarea");
            EnumToName.Add(XmlSymbol.RightArea, "rightarea");
            EnumToName.Add(XmlSymbol.PlotArea, "plotarea");
            EnumToName.Add(XmlSymbol.Legend, "legend");
            EnumToName.Add(XmlSymbol.XAxis, "xaxis");
            EnumToName.Add(XmlSymbol.YAxis, "yaxis");
            EnumToName.Add(XmlSymbol.ZAxis, "zaxis");
            EnumToName.Add(XmlSymbol.Series, "series");
            EnumToName.Add(XmlSymbol.XSeries, "xseries");
			EnumToName.Add(XmlSymbol.XValue, "xvalue");
			EnumToName.Add(XmlSymbol.Point, "point");

			EnumToName.Add(XmlSymbol.TabStops, "tabstops");
			EnumToName.Add(XmlSymbol.TabStop, "tabstop");

			EnumToName.Add(XmlSymbol.Bold, "b");
            EnumToName.Add(XmlSymbol.Italic, "i");
            EnumToName.Add(XmlSymbol.Underline, "u");
            EnumToName.Add(XmlSymbol.FontSize, "fontsize");
            EnumToName.Add(XmlSymbol.FontColor, "fontcolor");
            EnumToName.Add(XmlSymbol.Font, "font");
            //
            EnumToName.Add(XmlSymbol.Field, "field");
            EnumToName.Add(XmlSymbol.Symbol, "symbol");
            EnumToName.Add(XmlSymbol.Chr, "chr");
            //
            EnumToName.Add(XmlSymbol.Footnote, "footnote");
            EnumToName.Add(XmlSymbol.Hyperlink, "hyperlink");
            //
            EnumToName.Add(XmlSymbol.SoftHyphen, "-");
            EnumToName.Add(XmlSymbol.Tab, "tab");
            EnumToName.Add(XmlSymbol.LineBreak, "br");
            EnumToName.Add(XmlSymbol.Space, "space");
            EnumToName.Add(XmlSymbol.NoSpace, "nospace");

            //
            //

            /*EnumToName.Add(XmlSymbol.BraceLeft, "{");
            EnumToName.Add(XmlSymbol.BraceRight, "}");
            EnumToName.Add(XmlSymbol.BracketLeft, "[");
            EnumToName.Add(XmlSymbol.BracketRight, "]");
            EnumToName.Add(XmlSymbol.ParenLeft, "(");
            EnumToName.Add(XmlSymbol.ParenRight, ")");
            EnumToName.Add(XmlSymbol.Colon, ":");
            EnumToName.Add(XmlSymbol.Semicolon, ";");  //??? id DDL?
            EnumToName.Add(XmlSymbol.Dot, ".");
            EnumToName.Add(XmlSymbol.Comma, ",");
            EnumToName.Add(XmlSymbol.Percent, "%");  //??? id DDL?
            EnumToName.Add(XmlSymbol.Dollar, "$");  //??? id DDL?
            //enumToName.Add(XmlSymbol.At,                "@");
            EnumToName.Add(XmlSymbol.Hash, "#");  //??? id DDL?
            //enumToName.Add(XmlSymbol.Question,          "?");  //??? id DDL?
            //enumToName.Add(XmlSymbol.Bar,               "|");  //??? id DDL?
            EnumToName.Add(XmlSymbol.Assign, "=");
            EnumToName.Add(XmlSymbol.Slash, "/");  //??? id DDL?
            EnumToName.Add(XmlSymbol.BackSlash, "\\");
            EnumToName.Add(XmlSymbol.Plus, "+");  //??? id DDL?
            EnumToName.Add(XmlSymbol.PlusAssign, "+=");
            EnumToName.Add(XmlSymbol.Minus, "-");  //??? id DDL?
            EnumToName.Add(XmlSymbol.MinusAssign, "-=");
            EnumToName.Add(XmlSymbol.Blank, " ");
            */
            //---------------------------------------------------------------
            //---------------------------------------------------------------
            //---------------------------------------------------------------

            NameToEnum.Add("true", XmlSymbol.True);
            NameToEnum.Add("false", XmlSymbol.False);
            //NameToEnum.Add("null", XmlSymbol.Null);
            //
            NameToEnum.Add("attributes", XmlSymbol.Attributes);
            NameToEnum.Add("styles", XmlSymbol.Styles);
            NameToEnum.Add("style", XmlSymbol.Style);
            NameToEnum.Add("document", XmlSymbol.Document);
            NameToEnum.Add("info", XmlSymbol.Info);
            NameToEnum.Add("sections", XmlSymbol.Sections);
            NameToEnum.Add("section", XmlSymbol.Section);
            NameToEnum.Add("p", XmlSymbol.Paragraph);
            NameToEnum.Add("header", XmlSymbol.Header);
            NameToEnum.Add("footer", XmlSymbol.Footer);
            NameToEnum.Add("primaryheader", XmlSymbol.PrimaryHeader);
            NameToEnum.Add("primaryfooter", XmlSymbol.PrimaryFooter);
            NameToEnum.Add("firstpageheader", XmlSymbol.FirstPageHeader);
            NameToEnum.Add("firstpagefooter", XmlSymbol.FirstPageFooter);
            NameToEnum.Add("evenpageheader", XmlSymbol.EvenPageHeader);
            NameToEnum.Add("evenpagefooter", XmlSymbol.EvenPageFooter);
            NameToEnum.Add("table", XmlSymbol.Table);
            NameToEnum.Add("columns", XmlSymbol.Columns);
            NameToEnum.Add("column", XmlSymbol.Column);
            NameToEnum.Add("rows", XmlSymbol.Rows);
            NameToEnum.Add("row", XmlSymbol.Row);
            NameToEnum.Add("cell", XmlSymbol.Cell);
            NameToEnum.Add("image", XmlSymbol.Image);
            //NameToEnum.Add("text", XmlSymbol.Text);
            NameToEnum.Add("textframe", XmlSymbol.TextFrame);
            NameToEnum.Add("pagebreak", XmlSymbol.PageBreak);
            NameToEnum.Add("barcode", XmlSymbol.Barcode);
            NameToEnum.Add("chart", XmlSymbol.Chart);
            NameToEnum.Add("headerarea", XmlSymbol.HeaderArea);
            NameToEnum.Add("footerarea", XmlSymbol.FooterArea);
            NameToEnum.Add("toparea", XmlSymbol.TopArea);
            NameToEnum.Add("bottomarea", XmlSymbol.BottomArea);
            NameToEnum.Add("leftarea", XmlSymbol.LeftArea);
            NameToEnum.Add("rightarea", XmlSymbol.RightArea);
            NameToEnum.Add("plotarea", XmlSymbol.PlotArea);
            NameToEnum.Add("legend", XmlSymbol.Legend);
            NameToEnum.Add("xaxis", XmlSymbol.XAxis);
            NameToEnum.Add("yaxis", XmlSymbol.YAxis);
            NameToEnum.Add("zaxis", XmlSymbol.ZAxis);
            NameToEnum.Add("series", XmlSymbol.Series);
            NameToEnum.Add("xseries", XmlSymbol.XSeries);
			NameToEnum.Add("xvalue", XmlSymbol.XValue);
			NameToEnum.Add("point", XmlSymbol.Point);

			NameToEnum.Add("tabstops", XmlSymbol.TabStops);
			NameToEnum.Add("tabstop", XmlSymbol.TabStop);

			NameToEnum.Add("b", XmlSymbol.Bold);
            NameToEnum.Add("i", XmlSymbol.Italic);
            NameToEnum.Add("u", XmlSymbol.Underline);
            NameToEnum.Add("fontsize", XmlSymbol.FontSize);
            NameToEnum.Add("fontcolor", XmlSymbol.FontColor);
            NameToEnum.Add("font", XmlSymbol.Font);
            //
            NameToEnum.Add("field", XmlSymbol.Field);
            NameToEnum.Add("symbol", XmlSymbol.Symbol);
            NameToEnum.Add("chr", XmlSymbol.Chr);
            //
            NameToEnum.Add("footnote", XmlSymbol.Footnote);
            NameToEnum.Add("hyperlink", XmlSymbol.Hyperlink);
            //
            NameToEnum.Add("-", XmlSymbol.SoftHyphen); //??? \( is another special case.
            NameToEnum.Add("tab", XmlSymbol.Tab);
            NameToEnum.Add("br", XmlSymbol.LineBreak);
            NameToEnum.Add("space", XmlSymbol.Space);
            NameToEnum.Add("nospace", XmlSymbol.NoSpace);
        }

        /// <summary>
        /// Returns XmlSymbol value from name, or XmlSymbol.None if no such XmlSymbol exists.
        /// </summary>
        internal static XmlSymbol SymbolFromName(string name)
        {
            XmlSymbol XmlSymbol;
            if (!NameToEnum.TryGetValue(name.ToLower(), out XmlSymbol))
            {
                // Check for case sensitive keywords. Allow first character upper case only.
                if (string.Compare(name, "True", StringComparison.OrdinalIgnoreCase) == 0)
                    XmlSymbol = XmlSymbol.True;
                else if (string.Compare(name, "False", StringComparison.OrdinalIgnoreCase) == 0)
                    XmlSymbol = XmlSymbol.False;
                /*else if (string.Compare(name, "Null", StringComparison.OrdinalIgnoreCase) == 0)
                    XmlSymbol = XmlSymbol.Null;*/
                else
                    XmlSymbol = XmlSymbol.None;
            }
            return XmlSymbol;
        }

        /// <summary>
        /// Returns string from XmlSymbol value.
        /// </summary>
        internal static string NameFromSymbol(XmlSymbol XmlSymbol)
        {
            return EnumToName[XmlSymbol];
        }

        static readonly Dictionary<XmlSymbol, string> EnumToName = new Dictionary<XmlSymbol, string>();
        static readonly Dictionary<string, XmlSymbol> NameToEnum = new Dictionary<string, XmlSymbol>();
    }

}

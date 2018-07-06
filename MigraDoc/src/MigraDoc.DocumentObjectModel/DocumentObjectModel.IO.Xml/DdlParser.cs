#region MigraDoc - Creating Documents on the Fly
//
// Authors:
//   Stefan Lange
//   Klaus Potzesny
//   David Stephensen
//
// Copyright (c) 2001-2017 empira Software GmbH, Cologne Area (Germany)
//
// http://www.pdfsharp.com
// http://www.migradoc.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using System.Globalization;
using MigraDoc.DocumentObjectModel.Internals;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Shapes.Charts;
using System.Xml;
using System.Collections.Generic;
using AttrDictionary = System.Collections.Generic.Dictionary<string, string>;
using AttributePair = System.Collections.Generic.KeyValuePair<string, string>;

namespace MigraDoc.DocumentObjectModel.IO.Xml
{
	/// <summary>
	/// An Xml parser for MigraDoc DDL.
	/// </summary>
	internal class DdlParser
	{
		/// <summary>
		/// Initializes a new instance of the DdlParser class.
		/// </summary>
		internal DdlParser(string ddl, DdlReaderErrors errors)
			: this(String.Empty, ddl, errors)
		{ }

		/// <summary>
		/// Initializes a new instance of the DdlParser class.
		/// </summary>
		internal DdlParser(string fileName, string ddl, DdlReaderErrors errors)
		{
			_errors = errors ?? new DdlReaderErrors();

			_fileName = fileName;

			var readerSettings = new XmlReaderSettings()
			{
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			if (_fileName == String.Empty)
				_reader = XmlReader.Create(new System.IO.StringReader(ddl), readerSettings);
			else
				_reader = XmlReader.Create(_fileName, readerSettings);
		}

		/// <summary>
		/// Parses the keyword «<document>».
		/// </summary>
		internal Document ParseDocument(Document document)
		{

			if (document == null)
				document = new Document();

			AssertSymbol(XmlSymbol.Document);

			ParseAttributes(document);

			// Styles come first
			if (XmlSymbol == XmlSymbol.Styles)
				ParseStyles(document.Styles);

			// A document with no sections is valid and has zero pages.
			if (XmlSymbol == XmlSymbol.Sections)
				ParseSections(document.Sections);

			MoveNext();
			AssertCondition(XmlSymbol == XmlSymbol.Eof, DomMsgID.EndOfFileExpected);

			return document;
		}

		/// <summary>
		/// Parses one of the keywords «\document», «\styles», «\section», «\table», «\textframe», «\chart»
		/// and «\paragraph» and returns the corresponding DocumentObject or DocumentObjectCollection.
		/// </summary>
		internal DocumentObject ParseDocumentObject()
		{
			DocumentObject obj = null;

			// MoveToCode();
			switch (XmlSymbol)
			{
				case XmlSymbol.Document:
					obj = ParseDocument(null);
					break;

				case XmlSymbol.Styles:
					obj = ParseStyles(new Styles());
					break;

				case XmlSymbol.Section:
					obj = ParseSection(new Sections());
					break;

				case XmlSymbol.Table:
					obj = new Table();
					ParseTable(null, (Table)obj);
					break;

				case XmlSymbol.TextFrame:
					DocumentElements elems = new DocumentElements();
					ParseTextFrame(elems);
					obj = elems[0];
					break;

				case XmlSymbol.Chart:
					throw new NotImplementedException();

				case XmlSymbol.Paragraph:
					obj = new DocumentElements();
					ParseParagraph((DocumentElements)obj);
					break;

				default:
					ThrowParserException(DomMsgID.UnexpectedSymbol);
					break;
			}

			MoveNext();
			AssertCondition(XmlSymbol == XmlSymbol.Eof, DomMsgID.EndOfFileExpected);

			return obj;
		}

		/// <summary>
		/// Parses the keyword «\styles».
		/// </summary>
		private Styles ParseStyles(Styles styles)
		{
			AssertSymbol(XmlSymbol.Styles);
			MoveNext();

			while (!IsEndElement(XmlSymbol.Styles))
				ParseStyleDefinition(styles);

			AssertSymbol(XmlSymbol.Styles, true);
			MoveNext();

			return styles;
		}

		/// <summary>
		/// Parses the keyword «\sections».
		/// </summary>
		private Sections ParseSections(Sections sections)
		{
			AssertSymbol(XmlSymbol.Sections);
			MoveNext();

			while (!IsEndElement(XmlSymbol.Sections))
				ParseSection(sections);

			AssertSymbol(XmlSymbol.Sections, true);
			MoveNext();

			return sections;
		}

		/// <summary>
		/// Parses a style definition block within the keyword «\styles».
		/// </summary>
		private Style ParseStyleDefinition(Styles styles)
		{
			//   <Style Name="TextBox" BaseStyle="Normal">
			//      <Font>...</Font>
			//      <ParagraphFormat>...</ParagraphFormat>
			//   ...
			//   </Style>

			AssertSymbol(XmlSymbol.Style);
			var attributes = ParseElementAttributes();

			Style style = null;
			try
			{
				var styleName = GetAttributeValue(attributes, "Name", true);
				var baseStyleName = GetAttributeValue(attributes, "BaseStyle", true);

				if (String.IsNullOrEmpty(styleName))
					ThrowParserException(DomMsgID.StyleNameExpected, styleName);

				if (!String.IsNullOrEmpty(baseStyleName))
				{
					if (styles.GetIndex(baseStyleName) == -1)
					{
						ReportParserInfo(DdlErrorLevel.Warning, DomMsgID.UseOfUndefinedBaseStyle, baseStyleName);
						baseStyleName = StyleNames.InvalidStyleName;
					}
				}

				// Get or create style.
				style = styles[styleName];
				if (style != null)
				{
					// Reset base style.
					if (baseStyleName != null)
						style.BaseStyle = baseStyleName;
				}
				else
				{
					// Style does not exist and no base style is given, choose InvalidStyleName by default.
					if (String.IsNullOrEmpty(baseStyleName))
					{
						baseStyleName = StyleNames.InvalidStyleName;
						ReportParserInfo(DdlErrorLevel.Warning, DomMsgID.UseOfUndefinedStyle, styleName);
					}

					style = styles.AddStyle(styleName, baseStyleName);
				}

				// Parse definition (if any).
				if (IsStartElement())
				{

					MoveNext();

					while (!IsEndElement(XmlSymbol.Style))
						ParseAttributeBlock(style);

					AssertSymbol(XmlSymbol.Style, true);
				}

				MoveNext();

			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.Style);
			}

			return style;
		}

		/// <summary>
		/// Determines if the current symbol is a header or footer.
		/// </summary>
		private bool IsHeaderFooter()
		{
			XmlSymbol sym = XmlSymbol;
			return (sym == XmlSymbol.Header || sym == XmlSymbol.Footer ||
			  sym == XmlSymbol.PrimaryHeader || sym == XmlSymbol.PrimaryFooter ||
			  sym == XmlSymbol.EvenPageHeader || sym == XmlSymbol.EvenPageFooter ||
			  sym == XmlSymbol.FirstPageHeader || sym == XmlSymbol.FirstPageFooter);
		}

		/// <summary>
		/// Parses the keyword «\section».
		/// </summary>
		private Section ParseSection(Sections sections)
		{
			Debug.Assert(sections != null);

			AssertSymbol(XmlSymbol.Section);

			Section section = null;
			try
			{
				section = sections.AddSection();

				ParseAttributes(section);

				// TODO
				// Consider the case that the keyword «\paragraph» can be omitted.

				// 1st parse headers and footers
				while (IsHeaderFooter())
					ParseHeaderFooter(section);

				// 2nd parse all other stuff
				ParseDocumentElements(section.Elements, XmlSymbol.Section);

				AssertSymbol(XmlSymbol.Section, true);
				MoveNext();

			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.Section);
			}
			return section;
		}

		/// <summary>
		/// Parses the keywords «\header».
		/// </summary>
		private void ParseHeaderFooter(Section section)
		{
			if (section == null)
				throw new ArgumentNullException("section");

			XmlSymbol hdrFtrSym = XmlSymbol;

			try
			{
				bool isHeader = hdrFtrSym == XmlSymbol.Header ||
				  hdrFtrSym == XmlSymbol.PrimaryHeader ||
				  hdrFtrSym == XmlSymbol.FirstPageHeader ||
				  hdrFtrSym == XmlSymbol.EvenPageHeader;

				// Recall that the styles "Header" resp. "Footer" are used as default if
				// no other style was given. But this belongs to the rendering process,
				// not to the DDL parser. Therefore no code here belongs to that.
				HeaderFooter headerFooter = new HeaderFooter();

				ParseAttributes(headerFooter);

				ParseDocumentElements(headerFooter.Elements, hdrFtrSym);

				HeadersFooters headersFooters = isHeader ? section.Headers : section.Footers;
				if (hdrFtrSym == XmlSymbol.Header || hdrFtrSym == XmlSymbol.Footer)
				{
					headersFooters.Primary = headerFooter.Clone();
					headersFooters.EvenPage = headerFooter.Clone();
					headersFooters.FirstPage = headerFooter.Clone();
				}
				else
				{
					switch (hdrFtrSym)
					{
						case XmlSymbol.PrimaryHeader:
						case XmlSymbol.PrimaryFooter:
							headersFooters.Primary = headerFooter;
							break;

						case XmlSymbol.EvenPageHeader:
						case XmlSymbol.EvenPageFooter:
							headersFooters.EvenPage = headerFooter;
							break;

						case XmlSymbol.FirstPageHeader:
						case XmlSymbol.FirstPageFooter:
							headersFooters.FirstPage = headerFooter;
							break;
					}
				}

				AssertSymbol(hdrFtrSym, true);
				MoveNext();

			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(hdrFtrSym);
			}
		}

		// TODO

		/// <summary>
		/// Determines whether the next text is paragraph content or document element.
		/// </summary>
		/*private bool IsParagraphContent()
		{
			
            if (MoveToParagraphContent())
            {
                if (_scanner.Char == Chars.BackSlash)
                {
                    XmlSymbol symbol = _scanner.PeekKeyword();
                    switch (symbol)
                    {
                        case XmlSymbol.Bold:
                        case XmlSymbol.Italic:
                        case XmlSymbol.Underline:
                        case XmlSymbol.Field:
                        case XmlSymbol.Font:
                        case XmlSymbol.FontColor:
                        case XmlSymbol.FontSize:
                        case XmlSymbol.Footnote:
                        case XmlSymbol.Hyperlink:
                        case XmlSymbol.Symbol:
                        case XmlSymbol.Chr:
                        case XmlSymbol.Tab:
                        case XmlSymbol.LineBreak:
                        case XmlSymbol.Space:
                        case XmlSymbol.SoftHyphen:
                            return true;
                    }
                    return false;
                }
                return true;
            }
            return false;
            


		}*/

		/// <summary>
		/// Parses the document elements of a «\paragraph», «\cell» or comparable.
		/// </summary>
		private DocumentElements ParseDocumentElements(DocumentElements elements, XmlSymbol context)
		{
			//
			// This is clear:
			//   \section { Hallo World! }
			// All section content will be treated as paragraph content.
			//
			// but this is ambiguous:
			//   \section { \image(...) }
			// It could be an image inside a paragraph or at the section level.
			// In this case it will be treated as an image on section level.
			//
			// If this is not your intention it must be like this:
			//   \section { \paragraph { \image(...) } }
			//

			while (!IsEndElement(context))
			{

				switch (XmlSymbol)
				{
					case XmlSymbol.Paragraph:
						ParseParagraph(elements);
						break;

					case XmlSymbol.PageBreak:
						ParsePageBreak(elements);
						break;

					case XmlSymbol.Table:
						ParseTable(elements, null);
						break;

					case XmlSymbol.TextFrame:
						ParseTextFrame(elements);
						break;

					case XmlSymbol.Image:
						ParseImage(elements.AddImage(""), false);
						break;

					case XmlSymbol.Chart:
						ParseChart(elements);
						break;

					case XmlSymbol.Barcode:
						ParseBarcode(elements);
						break;

					default:
						ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
						break;
				}

			}

			return elements;
		}

		/// <summary>
		/// Parses the keyword «\paragraph».
		/// </summary>
		private void ParseParagraph(DocumentElements elements)
		{
			AssertSymbol(XmlSymbol.Paragraph);

			Paragraph paragraph = elements.AddParagraph();

			try
			{

				var hasContent = IsStartElement();

				ParseAttributes(paragraph, null, false);

				if (hasContent)
				{
					ParseParagraphContent(elements, paragraph);

					AssertSymbol(XmlSymbol.Paragraph, true);
				}

				MoveNext();
			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.Paragraph);
			}

		}

		/// <summary>
		/// Parses the inner text of a paragraph, i.e. stops on BraceRight and treats empty
		/// line as paragraph separator.
		/// </summary>


		private void ParseParagraphContent(DocumentElements elements, Paragraph paragraph)
		{

			Paragraph para = paragraph ?? elements.AddParagraph();

			while (!IsEndElement(XmlSymbol.Paragraph))
				ParseFormattedText(para.Elements);

		}


		/// <summary>
		/// Removes the last blank from the text. Used before a tab, a linebreak or a space will be
		/// added to the text.
		/// </summary>
		private void RemoveTrailingBlank(ParagraphElements elements)
		{
			DocumentObject dom = elements.LastObject;
			Text text = dom as Text;
			if (text != null)
			{
				if (text.Content.EndsWith(" "))
					text.Content = text.Content.Remove(text.Content.Length - 1, 1);
			}
		}

		/// <summary>
		/// Parses the inner text of a paragraph. Parsing ends if '}' is reached or an empty
		/// line occurs on nesting level 0.
		/// </summary>

		private void ParseFormattedText(ParagraphElements elements)
		{
			string text;

			bool loop = true;
			while (loop)
			{
				switch (_reader.NodeType)
				{
					case XmlNodeType.Element:

						switch (XmlSymbol)
						{
							// TODO
							/*
							 case XmlSymbol.EmptyLine:
								 elements.AddCharacter(SymbolName.ParaBreak);
								 ReadText(rootLevel);
								 break;*/

							/*
							case XmlSymbol.Comment:
								// Ignore comments.
								ReadText(rootLevel);
								break;
								*/

							case XmlSymbol.Tab:
								RemoveTrailingBlank(elements);
								elements.AddTab();
								MoveNext(false);
								break;

							case XmlSymbol.LineBreak:
								RemoveTrailingBlank(elements);
								elements.AddLineBreak();
								MoveNext(false);
								break;

							case XmlSymbol.Bold:
								ParseBoldItalicEtc(elements.AddFormattedText(TextFormat.Bold), XmlSymbol.Bold);
								break;

							case XmlSymbol.Italic:
								ParseBoldItalicEtc(elements.AddFormattedText(TextFormat.Italic), XmlSymbol.Italic);
								break;

							case XmlSymbol.Underline:
								ParseBoldItalicEtc(elements.AddFormattedText(TextFormat.Underline), XmlSymbol.Underline);

								break;

							case XmlSymbol.Font:
								ParseFont(elements.AddFormattedText());

								break;

							case XmlSymbol.FontSize:
								ParseFontSize(elements.AddFormattedText());

								break;

							case XmlSymbol.FontColor:
								ParseFontColor(elements.AddFormattedText());

								break;

							case XmlSymbol.Image:
								ParseImage(elements.AddImage(""), true);

								break;

							case XmlSymbol.Field:
								ParseField(elements);

								break;

							case XmlSymbol.Footnote:
								ParseFootnote(elements);

								break;

							case XmlSymbol.Hyperlink:
								ParseHyperlink(elements);

								break;

							case XmlSymbol.Space:
								RemoveTrailingBlank(elements);
								ParseSpace(elements);

								break;

							case XmlSymbol.Symbol:
								ParseSymbol(elements);
								break;

							case XmlSymbol.Chr:
								ParseChr(elements);

								break;

							default:
								ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
								break;
						}
						break;
					case XmlNodeType.EndElement:

						loop = false;
						break;
					case XmlNodeType.Text:

						text = RemoveTrailingWhiteSpace(RemoveLeadingWhiteSpace(_reader.Value));

						if (text != String.Empty)
							elements.AddText(text);

						MoveNext();
						break;
					case XmlNodeType.CDATA:

						text = _reader.Value;

						if (text != String.Empty)
							elements.AddText(text);

						MoveNext();
						break;
					default:
						break;
				}
			}
		}

		/// <summary>
		/// Parses the keywords «\bold», «\italic», and «\underline».
		/// </summary>
		private void ParseBoldItalicEtc(FormattedText formattedText, XmlSymbol symbol)
		{

			AssertSymbol(symbol);

			if (IsStartElement())
			{				
				ParseFormattedText(formattedText.Elements);

				AssertSymbol(symbol, true);
			}

			MoveNext(false);

		}

		/// <summary>
		/// Parses the keyword «\font».
		/// </summary>
		private void ParseFont(FormattedText formattedText)
		{
			AssertSymbol(XmlSymbol.Font);

			var hasContent = IsStartElement();
			ParseAttributes(formattedText, null, false);

			if (hasContent)
			{				
				ParseFormattedText(formattedText.Elements);

				AssertSymbol(XmlSymbol.Font, true);
			}

			MoveNext(false);
		}
		
		/// <summary>
		/// Parses the keyword «\fontsize».
		/// </summary>
		private void ParseFontSize(FormattedText formattedText)
		{
			AssertSymbol(XmlSymbol.FontSize);

			var hasContent = IsStartElement();
			var attributes = ParseElementAttributes();

			var size = GetAttributeValue(attributes, "Size");
			if (size != null)
				//NYI: Check token for correct Unit format
				formattedText.Font.Size = size;

			if (hasContent)
			{
				MoveNext(false);

				ParseFormattedText(formattedText.Elements);
				AssertSymbol(XmlSymbol.FontSize, true);
			}

			MoveNext(false);
		}

		/// <summary>
		/// Parses the keyword «\fontcolor».
		/// </summary>
		private void ParseFontColor(FormattedText formattedText/*, string value*/)
		{

			AssertSymbol(XmlSymbol.FontColor);

			var hasContent = IsStartElement();
			var attributes = ParseElementAttributes();

			var color = GetAttributeValue(attributes, "Color");
			if (color != null)
			{
				Color c = ParseColor(color);
				formattedText.Font.Color = c;
			}

			if (hasContent)
			{
				MoveNext(false);

				ParseFormattedText(formattedText.Elements);
				AssertSymbol(XmlSymbol.FontColor, true);
			}

			MoveNext(false);
		}

		// TODO
		/// <summary>
		/// Parses the keyword «\symbol» resp. «\(».
		/// </summary>
		private void ParseSymbol(ParagraphElements elements)
		{
			throw new NotImplementedException();
		}

		// TODO
		/// <summary>
		/// Parses the keyword «\chr».
		/// </summary>
		private void ParseChr(ParagraphElements elements)
		{
			throw new NotImplementedException();

		}

		/// <summary>
		/// Parses the keyword «\field».
		/// </summary>
		private void ParseField(ParagraphElements elements)
		{

			AssertSymbol(XmlSymbol.Field);

			var attributes = ParseElementAttributes();

			string fieldType = GetAttributeValue(attributes, "Type", true);
			AssertCondition(fieldType != null, DomMsgID.MissingObligatoryProperty, "Type");

			DocumentObject field = null;
			switch (fieldType.ToLower())
			{
				case "date":
					field = elements.AddDateField();
					break;

				case "page":
					field = elements.AddPageField();
					break;

				case "numpages":
					field = elements.AddNumPagesField();
					break;

				case "info":
					field = elements.AddInfoField(0);
					break;

				case "sectionpages":
					field = elements.AddSectionPagesField();
					break;

				case "section":
					field = elements.AddSectionField();
					break;

				case "bookmark":
					field = elements.AddBookmark("");
					break;

				case "pageref":
					field = elements.AddPageRefField("");
					break;
			}

			AssertCondition(field != null, DomMsgID.InvalidFieldType, fieldType);

			ParseAttributes(field, attributes);

			MoveNext(false);
		}

		/// <summary>
		/// Parses the keyword «\footnote».
		/// </summary>
		private void ParseFootnote(ParagraphElements elements)
		{
			AssertSymbol(XmlSymbol.Footnote);

			Footnote footnote = elements.AddFootnote();

			ParseAttributes(footnote);
			ParseDocumentElements(footnote.Elements, XmlSymbol.Footnote);

			AssertSymbol(XmlSymbol.Footnote, true);
			MoveNext(false);

		}

		/// <summary>
		/// Parses the keyword «\hyperlink».
		/// </summary>
		private void ParseHyperlink(ParagraphElements elements)
		{
			AssertSymbol(XmlSymbol.Hyperlink);

			Hyperlink hyperlink = elements.AddHyperlink("");
			//NYI: Without name and type the hyperlink is senseless, so attributes need to be checked

			var hasContent = IsStartElement();
			ParseAttributes(hyperlink, null, false);

			if (hasContent)
			{
				while (!IsEndElement(XmlSymbol.Hyperlink))
					ParseFormattedText(hyperlink.Elements);

				AssertSymbol(XmlSymbol.Hyperlink, true);
			}

			MoveNext(false);
		}


		// TODO
		/// <summary>
		/// 
		/// Parses the keyword «\space».
		/// </summary>
		private void ParseSpace(ParagraphElements elements)
		{
			// Samples
			// <space/>
			// <space Count="5"/>
			// <space Type="em"/>
			// <space Type="em" Count="5"/>

			AssertSymbol(XmlSymbol.Space);

			Character space = elements.AddSpace(1);

			var attributes = ParseElementAttributes();

			var type = GetAttributeValue(attributes, "Type");
			if (type != null)
			{
				if (!IsSpaceType(type))
					ThrowParserException(DomMsgID.InvalidEnum, type);

				space.SymbolName = (SymbolName)Enum.Parse(typeof(SymbolName), type, true);
			}

			var count = GetAttributeValue(attributes, "Count");
			if (count != null)
				space.Count = Int32.Parse(count);

			MoveNext();
		}


		/// <summary>
		/// Parses a page break in a document elements container.
		/// </summary>
		private void ParsePageBreak(DocumentElements elements)
		{
			AssertSymbol(XmlSymbol.PageBreak);
			elements.AddPageBreak();

			AssertSymbol(XmlSymbol.PageBreak, true);
			MoveNext(false);
		}

		/// <summary>
		/// Parses the keyword «\table».
		/// </summary>
		private void ParseTable(DocumentElements elements, Table table)
		{
			Table tbl = table;
			try
			{
				if (tbl == null)
					tbl = elements.AddTable();

				AssertSymbol(XmlSymbol.Table);
				var attributes = ParseElementAttributes();

				ParseAttributes(tbl, attributes);

				// Table must start with «\columns»...
				AssertSymbol(XmlSymbol.Columns);
				ParseColumns(tbl);

				// ...followed by «\rows».
				AssertSymbol(XmlSymbol.Rows);
				ParseRows(tbl);

				AssertSymbol(XmlSymbol.Table, true);
				MoveNext();

			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.Table);
			}
		}

		/// <summary>
		/// Parses the keyword «\columns».
		/// </summary>
		private void ParseColumns(Table table)
		{
			Debug.Assert(table != null);
			Debug.Assert(XmlSymbol == XmlSymbol.Columns);

			ParseAttributes(table.Columns);

			while (!IsEndElement(XmlSymbol.Columns))
			{
				switch (XmlSymbol)
				{
					case XmlSymbol.Column:
						ParseColumn(table.AddColumn());
						break;

					default:
						ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
						break;

				}
			}

			AssertSymbol(XmlSymbol.Columns, true);
			MoveNext();
		}

		/// <summary>
		/// Parses the keyword «\column».
		/// </summary>
		private void ParseColumn(Column column)
		{
			Debug.Assert(column != null);
			Debug.Assert(XmlSymbol == XmlSymbol.Column);

			ParseAttributes(column);

			AssertSymbol(XmlSymbol.Column, true);
			MoveNext();
		}

		/// <summary>
		/// Parses the keyword «\rows».
		/// </summary>
		private void ParseRows(Table table)
		{
			Debug.Assert(table != null);
			Debug.Assert(XmlSymbol == XmlSymbol.Rows);

			ParseAttributes(table.Rows);

			while (!IsEndElement(XmlSymbol.Rows))
			{

				switch (XmlSymbol)
				{
					case XmlSymbol.Row:
						ParseRow(table.AddRow());
						break;

					default:
						ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
						break;
				}
			}

			AssertSymbol(XmlSymbol.Rows, true);
			MoveNext();
		}

		/// <summary>
		/// Parses the keyword «\row».
		/// </summary>
		private void ParseRow(Row row)
		{
			Debug.Assert(row != null);
			Debug.Assert(XmlSymbol == XmlSymbol.Row);

			ParseAttributes(row);

			int idx = 0;

			while (!IsEndElement(XmlSymbol.Row))
			{

				switch (XmlSymbol)
				{

					case XmlSymbol.Cell:
						ParseCell(row[idx]);
						idx++;
						break;

					default:
						ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
						break;
				}
			}

			AssertSymbol(XmlSymbol.Row, true);
			MoveNext();
		}

		/// <summary>
		/// Parses the keyword «\cell».
		/// </summary>
		private void ParseCell(Cell cell)
		{
			Debug.Assert(cell != null);
			Debug.Assert(XmlSymbol == XmlSymbol.Cell);

			var hasContent = IsStartElement();

			ParseAttributes(cell);

			if (hasContent)
			{
				ParseDocumentElements(cell.Elements, XmlSymbol.Cell);
				AssertSymbol(XmlSymbol.Cell, true);
			}

			MoveNext();

		}

		/// <summary>
		/// Parses the keyword «\image».
		/// </summary>
		private void ParseImage(Image image, bool paragraphContent)
		{
			// Future syntax by example
			//   \image("Name")
			//   \image("Name")[...]
			//   \image{base64...}       //NYI
			//   \image[...]{base64...}  //NYI
			Debug.Assert(image != null);

			try
			{

				AssertSymbol(XmlSymbol.Image);

				var attributes = ParseElementAttributes();
				image.Name = GetAttributeValue(attributes, "Name", true);

				ParseAttributes(image, attributes);

				AssertSymbol(XmlSymbol.Image, true);
				MoveNext(false);
			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.Image);
			}
		}

		/// <summary>
		/// Parses the keyword «\textframe».
		/// </summary>
		private void ParseTextFrame(DocumentElements elements)
		{
			Debug.Assert(elements != null);

			TextFrame textFrame = elements.AddTextFrame();
			try
			{
				var hasContent = IsStartElement();

				ParseAttributes(textFrame);

				if (hasContent)
				{
					ParseDocumentElements(textFrame.Elements, XmlSymbol.TextFrame);
					AssertSymbol(XmlSymbol.TextFrame, true);
				}

				MoveNext();

			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				AdjustToNextBlock(XmlSymbol.TextFrame);
			}
		}

		// TODO
		private void ParseBarcode(DocumentElements elements)
		{
			// Syntax:
			// 1.  \barcode(Code)
			// 2.  \barcode(Code)[...]
			// 3.  \barcode(Code, Type)
			// 4.  \barcode(Code, Type)[...]

			try
			{
				AssertSymbol(XmlSymbol.Barcode);

				Barcode barcode = elements.AddBarcode();

				ParseAttributes(barcode);

				AssertSymbol(XmlSymbol.Barcode, true);
				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.Barcode);
			}
		}


		// TODO
		/// <summary>
		/// Parses the keyword «\chart».
		/// </summary>
		private void ParseChart(DocumentElements elements)
		{
			// Syntax:
			// 1.  \chartarea(Type){...}
			// 2.  \chartarea(Type)[...]{...}
			//
			// Usage of header-, bottom-, footer-, left- and rightarea are similar.

			ChartType chartType = 0;
			try
			{
				AssertSymbol(XmlSymbol.Chart);

				var attributes = ParseElementAttributes();

				string chartTypeName = GetAttributeValue(attributes, "Type", true);

				try
				{
					chartType = (ChartType)Enum.Parse(typeof(ChartType), chartTypeName, true);
				}
				catch (Exception ex)
				{
					ThrowParserException(ex, DomMsgID.UnknownChartType, chartTypeName);
				}

				Chart chart = elements.AddChart(chartType);

				ParseAttributes(chart, attributes);

				while (!IsEndElement(XmlSymbol.Chart))
				{
					switch (XmlSymbol)
					{
						case XmlSymbol.PlotArea:
							ParseArea(chart.PlotArea);
							break;

						case XmlSymbol.HeaderArea:
							ParseArea(chart.HeaderArea);
							break;

						case XmlSymbol.FooterArea:
							ParseArea(chart.FooterArea);
							break;

						case XmlSymbol.TopArea:
							ParseArea(chart.TopArea);
							break;

						case XmlSymbol.BottomArea:
							ParseArea(chart.BottomArea);
							break;

						case XmlSymbol.LeftArea:
							ParseArea(chart.LeftArea);
							break;

						case XmlSymbol.RightArea:
							ParseArea(chart.RightArea);
							break;

						case XmlSymbol.XAxis:
							ParseAxes(chart.XAxis, XmlSymbol);
							break;

						case XmlSymbol.YAxis:
							ParseAxes(chart.YAxis, XmlSymbol);
							break;

						case XmlSymbol.ZAxis:
							ParseAxes(chart.ZAxis, XmlSymbol);
							break;

						case XmlSymbol.Series:
							ParseSeries(chart.SeriesCollection.AddSeries());
							break;

						case XmlSymbol.XSeries:
							ParseSeries(chart.XValues.AddXSeries());
							break;

						default:
							ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
							break;
					}
				}

				AssertSymbol(XmlSymbol.Chart, true);
				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.Chart);
			}
		}

		/// <summary>
		/// Parses the keyword «\plotarea» inside a chart.
		/// </summary>
		private void ParseArea(PlotArea area)
		{
			// Syntax:
			// 1.  \plotarea{...}
			// 2.  \plotarea[...]{...} //???

			try
			{
				ParseAttributes(area);
				MoveNext();
			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.PlotArea);
			}
		}

		/// <summary>
		/// Parses the keywords «\headerarea», «\toparea», «\bottomarea», «\footerarea»,
		/// «\leftarea» or «\rightarea» inside a chart.
		/// </summary>
		private void ParseArea(TextArea area)
		{
			// Syntax:
			// 1.  \toparea{...}
			// 2.  \toparea[...]{...}
			//
			// Usage of header-, bottom-, footer-, left- and rightarea are similar.

			string name = _reader.Name;

			try
			{

				ParseAttributes(area);

				switch (XmlSymbol)
				{
					case XmlSymbol.Legend:
						ParseLegend(area.AddLegend());
						break;

					case XmlSymbol.Paragraph:
						ParseParagraph(area.Elements);
						break;

					case XmlSymbol.Table:
						ParseTable(null, area.AddTable());
						break;

					case XmlSymbol.TextFrame:
						ParseTextFrame(area.Elements);
						break;

					case XmlSymbol.Image:
						Image image = new Image();
						ParseImage(image, false);
						area.Elements.Add(image);
						break;

					default:
						ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
						break;
				}

				MoveNext();
			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(name);
			}
		}

		/// <summary>
		/// Parses the keywords «\xaxis», «\yaxis» or «\zaxis» inside a chart.
		/// </summary>
		private void ParseAxes(Axis axis, XmlSymbol symbolAxis)
		{
			// Syntax:
			// 1.  \xaxis[...]
			// 2.  \xaxis[...]{...} //???
			//
			// Usage of yaxis and zaxis are similar.

			string name = _reader.Name;

			try
			{
				ParseAttributes(axis);

				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(name);
			}
		}

		// TODO

		/// <summary>
		/// Parses the keyword «\series» inside a chart.
		/// </summary>
		private void ParseSeries(Series series)
		{
			// Syntax:
			// 1.  \series{...}
			// 2.  \series[...]{...}

			try
			{
				AssertSymbol(XmlSymbol.Series);

				ParseAttributes(series);

				while (!IsEndElement(XmlSymbol.Series))
				{
					switch (XmlSymbol)
					{
						case XmlSymbol.Point:
							ParsePoint(series.Add(0.0));
							break;

						default:
							ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
							break;
					}
				}

				AssertSymbol(XmlSymbol.Series, true);
				MoveNext();
			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.Series);
			}
		}

		// TODO

		/// <summary>
		/// Parses the keyword «\xvalues» inside a chart.
		/// </summary>
		private void ParseSeries(XSeries series)
		{
			// Syntax:
			// 1.  \xvalues{...}

			try
			{

				AssertSymbol(XmlSymbol.XSeries);

				ParseAttributes(series);

				while (!IsEndElement(XmlSymbol.XSeries))
				{
					switch (XmlSymbol)
					{
						case XmlSymbol.XValue:
							ParseXValue(series.Add(String.Empty));
							break;
						default:
							ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
							break;
					}
				}

				AssertSymbol(XmlSymbol.XSeries, true);
				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.XSeries);
			}
		}

		// TODO

		/// <summary>
		/// Parses the keyword «\point» inside a series.
		/// </summary>
		private void ParsePoint(Point point)
		{
			// Syntax:
			// 1.  \point{...}
			// 2.  \point[...]{...}

			try
			{
				AssertSymbol(XmlSymbol.Point);

				var hasContent = IsStartElement();
				ParseAttributes(point, null, false);

				if (hasContent)
				{
					point.Value = Double.Parse(ReadText());
					AssertSymbol(XmlSymbol.Point, true);
				}

				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.Point);
			}
		}


		private void ParseXValue(XValue xValue)
		{
			try
			{
				AssertSymbol(XmlSymbol.XValue);

				var hasContent = IsStartElement();
				ParseAttributes(xValue, null, false);

				if (hasContent)
				{
					xValue.Value = ReadText();

					AssertSymbol(XmlSymbol.XValue, true);
				}

				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.XValue);
			}
		}


		/// <summary>
		/// Parses the keyword «\legend» inside a textarea.
		/// </summary>
		private void ParseLegend(Legend legend)
		{
			// Syntax:
			// 1.  \legend
			// 2.  \legend[...]
			// 3.  \legend[...]{...}

			try
			{
				ParseAttributes(legend);
				MoveNext();
			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.Legend);
			}
		}

		/// <summary>
		/// Parses an attribute declaration block enclosed in brackets «[…]».
		/// </summary>
		private void ParseAttributes(DocumentObject element, AttrDictionary attributes = null, bool skipText = true)
		{

			if (attributes == null)
				attributes = ParseElementAttributes();

			foreach (var attribute in attributes)
				ParseAttributeStatement(element, attribute);

			if (IsStartElement())
			{
				//bool hasAttributes = ;
				MoveNext(skipText);

				while (IsStartElement(XmlSymbol.Attributes) || IsEmptyElement(XmlSymbol.Attributes))
				{
					if (IsStartElement(XmlSymbol.Attributes))
					{
						MoveNext();
						while (!IsEndElement(XmlSymbol.Attributes))
							ParseAttributeBlock(element);

						AssertSymbol(XmlSymbol.Attributes, true);
						MoveNext(skipText);
					}
					else if (IsEmptyElement(XmlSymbol.Attributes))
						MoveNext(skipText);
				}

			}

		}

		/// <summary>
		/// Parses a single statement in an attribute declaration block.
		/// </summary>
		private void ParseAttributeStatement(DocumentObject doc, AttributePair attribute)
		{

			// Syntax is easy
			//   identifier: xxxxx
			// or 
			//   sequence of identifiers: xxx.yyy.zzz
			//
			// followed by: «=», «+=», «-=», or «{»
			//
			// Parser of rhs depends on the type of the l-value.

			if (doc == null)
				throw new ArgumentNullException("doc");

			string valueName = "";
			try
			{
				valueName = attribute.Key;

				// Assign
				//DomValueDescriptor is needed from assignment routine.               

				// TODO
				// Assert doc.Meta[valueName]

				ValueDescriptor pvd = null;

				try
				{
					pvd = doc.Meta[valueName];
				}
				catch
				{
				}

				AssertCondition(pvd != null, DomMsgID.InvalidValueName, valueName);
				ParseAssign(doc, pvd, attribute);
			}
			catch (DdlParserException ex)
			{
				ReportParserException(ex);
				//AdjustToNextBlock();
			}
			catch (ArgumentException e)
			{
				ReportParserException(e, DomMsgID.InvalidAssignment, valueName);
			}
		}

		/// <summary>
		/// Parses an attribute declaration block enclosed in braces «{…}».
		/// </summary>
		private void ParseAttributeBlock(DocumentObject element)
		{
			var valueName = _reader.Name;

			//hack
			if (String.Compare(valueName, "TabStops", StringComparison.OrdinalIgnoreCase) == 0)
			{

				if (!(element is ParagraphFormat))
					ThrowParserException(DomMsgID.SymbolNotAllowed, valueName);

				ParseTabStops((element as ParagraphFormat).TabStops);

			}
			else
			{
				try
				{
					object val = null;
					try
					{
						val = element.GetValue(valueName);
					}
					catch
					{
					}

					AssertCondition(val != null, DomMsgID.InvalidValueName, valueName);

					DocumentObject child = val as DocumentObject;
					if (child != null)
					{

						AttrDictionary attributes = ParseElementAttributes();

						foreach (var attribute in attributes)
							ParseAttributeStatement(child, attribute);

						// parse child elements if any
						if (IsStartElement())
						{
							string name = _reader.Name;
							MoveNext();
							while (!IsEndElement(name))
								ParseAttributeBlock(child);
						}

						MoveNext();

					}
					else
						ThrowParserException(DomMsgID.SymbolIsNotAnObject, valueName);
				}
				catch (DdlParserException ex)
				{
					ReportParserException(ex);

					AdjustToNextBlock(valueName);
				}
				catch (ArgumentException e)
				{
					ReportParserException(e, DomMsgID.InvalidAssignment, valueName);
				}

			}

		}

		/// <summary>
		/// Parses the keyword «\tabstops» inside a paragraph.
		/// </summary>
		private void ParseTabStops(TabStops tabStops)
		{
			try
			{
				AssertSymbol(XmlSymbol.TabStops);
				MoveNext();

				while (!IsEndElement(XmlSymbol.TabStops))
				{
					switch (XmlSymbol)
					{
						case XmlSymbol.TabStop:
							ParseTabStop(tabStops);
							break;
						default:
							ThrowParserException(DomMsgID.UnexpectedSymbol, _reader.Name);
							break;
					}
				}

				AssertSymbol(XmlSymbol.TabStops, true);
				MoveNext();

			}
			catch (DdlParserException pe)
			{
				ReportParserException(pe);
				AdjustToNextBlock(XmlSymbol.TabStops);
			}
		}

		private void ParseTabStop(TabStops tabStops)
		{
			AssertSymbol(XmlSymbol.TabStop);

			var attributes = ParseElementAttributes();
			bool fAddItem = GetAttributeValueAsBool(attributes, "Add", true);

			TabStop tabStop = new TabStop();

			ParseAttributes(tabStop, attributes);

			if (fAddItem)
				tabStops.AddTabStop(tabStop);
			else
				tabStops.RemoveTabStop(tabStop.Position);

			MoveNext();

			// TODO
			// Special hack for tab stops...
			//Unit unit = Token;
			//tabStop.SetValue("Position", unit);

		}


		/// <summary>
		/// Parses an assign statement in an attribute declaration block.
		/// </summary>
		private void ParseAssign(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			if (dom == null)
				throw new ArgumentNullException("dom");
			if (vd == null)
				throw new ArgumentNullException("vd");

			Type valType = vd.ValueType;
			try
			{
				if (valType == typeof(string))
					ParseStringAssignment(dom, vd, attribute);
				else if (valType == typeof(int))
					ParseIntegerAssignment(dom, vd, attribute);
				else if (valType == typeof(Unit))
					ParseUnitAssignment(dom, vd, attribute);
				else if (valType == typeof(double) || valType == typeof(float))
					ParseRealAssignment(dom, vd, attribute);
				else if (valType == typeof(bool))
					ParseBoolAssignment(dom, vd, attribute);
#if !NETFX_CORE
				else if (typeof(Enum).IsAssignableFrom(valType))
#else
                else if (typeof(Enum).GetTypeInfo().IsAssignableFrom(valType.GetTypeInfo()))
#endif
					ParseEnumAssignment(dom, vd, attribute);
				else if (valType == typeof(Color))
					ParseColorAssignment(dom, vd, attribute);
#if !NETFX_CORE
				else if (typeof(ValueType).IsAssignableFrom(valType))
#else
                else if (typeof(ValueType).GetTypeInfo().IsAssignableFrom(valType.GetTypeInfo()))
#endif
				{
					ParseValueTypeAssignment(dom, vd, attribute);
				}
#if !NETFX_CORE
				else if (typeof(DocumentObject).IsAssignableFrom(valType))
#else
                else if (typeof(DocumentObject).GetTypeInfo().IsAssignableFrom(valType.GetTypeInfo()))
#endif
				{
					ParseDocumentObjectAssignment(dom, vd, attribute);
				}
				else
				{
					//AdjustToNextStatement();
					ThrowParserException(DomMsgID.InvalidType, vd.ValueType.Name, vd.ValueName);
				}
			}
			catch (Exception ex)
			{
				ReportParserException(ex, DomMsgID.InvalidAssignment, vd.ValueName);
			}
		}


		// TODO parser attributi numerici check tipo

		/// <summary>
		/// Parses the assignment to a boolean l-value.
		/// </summary>
		private void ParseBoolAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			var symbol = XmlKeyWords.SymbolFromName(attribute.Value);

			AssertCondition(symbol == XmlSymbol.True || symbol == XmlSymbol.False, DomMsgID.BoolExpected,
			  attribute.Value);

			dom.SetValue(vd.ValueName, symbol == XmlSymbol.True);
			// ReadCode();
		}

		/// <summary>
		/// Parses the assignment to an integer l-value.
		/// </summary>
		private void ParseIntegerAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			//AssertCondition(XmlSymbol == XmlSymbol.IntegerLiteral || XmlSymbol == XmlSymbol.HexIntegerLiteral || XmlSymbol == XmlSymbol.StringLiteral,
			//  DomMsgID.IntegerExpected, _reader.Name);

			int n = Int32.Parse(attribute.Value, CultureInfo.InvariantCulture);
			dom.SetValue(vd.ValueName, n);

			// ReadCode();
		}

		/// <summary>
		/// Parses the assignment to a floating point l-value.
		/// </summary>
		private void ParseRealAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			double r = double.Parse(attribute.Value, CultureInfo.InvariantCulture);
			dom.SetValue(vd.ValueName, r);

		}

		/// <summary>
		/// Parses the assignment to a Unit l-value.
		/// </summary>
		private void ParseUnitAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			Unit unit = attribute.Value;
			dom.SetValue(vd.ValueName, unit);

		}

		/// <summary>
		/// Parses the assignment to a string l-value.
		/// </summary>
		private void ParseStringAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			//AssertCondition(XmlSymbol == XmlSymbol.StringLiteral, DomMsgID.StringExpected, _scanner.Token);

			vd.SetValue(dom, attribute.Value);  //dom.SetValue(vd.ValueName, scanner.Token);

			// ReadCode();  // read next token
		}

		/// <summary>
		/// Parses the assignment to an enum l-value.
		/// </summary>
		private void ParseEnumAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{

			try
			{
				object val = Enum.Parse(vd.ValueType, attribute.Value, true);
				dom.SetValue(vd.ValueName, val);
			}
			catch (Exception ex)
			{
				ThrowParserException(ex, DomMsgID.InvalidEnum, attribute.Value, vd.ValueName);
			}

		}

		/// <summary>
		/// Parses the assignment to a struct (i.e. LeftPosition) l-value.
		/// </summary>
		private void ParseValueTypeAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			object val = vd.GetValue(dom, GV.ReadWrite);
			try
			{
				INullableValue ival = (INullableValue)val;
				ival.SetValue(attribute.Value);
				dom.SetValue(vd.ValueName, val);
				// ReadCode();
			}
			catch (Exception ex)
			{
				ReportParserException(ex, DomMsgID.InvalidAssignment, vd.ValueName);
			}
		}


		// TODO

		/// <summary>
		/// Parses the assignment to a DocumentObject l-value.
		/// </summary>
		private void ParseDocumentObjectAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			object val = vd.GetValue(dom, GV.ReadWrite);

			try
			{
				//if (XmlSymbol == XmlSymbol.Null)
				//{
				//string name = vd.ValueName;
				Type type = vd.ValueType;
				if (typeof(Border) == type)
					((Border)val).Clear();
				else if (typeof(Borders) == type)
					((Borders)val).ClearAll();
				else if (typeof(Shading) == type)
					((Shading)val).Clear();
				else if (typeof(TabStops) == type)
				{
					TabStops tabStops = (TabStops)vd.GetValue(dom, GV.ReadWrite);
					tabStops.ClearAll();
				}
				else
					ThrowParserException(DomMsgID.NullAssignmentNotSupported, vd.ValueName);

			}
			catch (Exception ex)
			{
				ReportParserException(ex, DomMsgID.InvalidAssignment, vd.ValueName);
			}
		}


		/// <summary>
		/// Parses the assignment to a Color l-value.
		/// </summary>
		private void ParseColorAssignment(DocumentObject dom, ValueDescriptor vd, AttributePair attribute)
		{
			object val = vd.GetValue(dom, GV.ReadWrite);
			Color color = ParseColor(attribute.Value);
			dom.SetValue(vd.ValueName, color);
		}

		// TODO literal, hexliteral, cmyk

		/// <summary>
		/// Parses a color. It can be «green», «123456», «0xFFABCDEF», 
		/// «RGB(r, g, b)», «CMYK(c, m, y, k)», «CMYK(a, c, m, y, k)», «GRAY(g)», or «"MyColor"».
		/// </summary>
		private Color ParseColor(String value)
		{
			Color color = Color.Empty;

			var scanner = new AttributeScanner(value);

			scanner.Scan('(');

			switch (scanner.Token.ToUpper())
			{
				case "RGB":
					color = ParseRGB(scanner);
					break;
				case "CMYK":
					color = ParseCMYK();
					break;

				case "HSB":
					throw new NotImplementedException("ParseColor(HSB)");

				case "Lab":
					throw new NotImplementedException("ParseColor(Lab)");

				case "GRAY":
					color = ParseGray();
					break;

				default: // Must be color enum
					try
					{
						color = Color.Parse(scanner.Token);
					}
					catch (Exception ex)
					{
						ThrowParserException(ex, DomMsgID.InvalidColor, scanner.Token);
					}
					break;
			}

			return color;
		}

		/// <summary>
		/// Parses «RGB(r, g, b)».
		/// </summary>
		private Color ParseRGB(AttributeScanner scanner)
		{
			uint r, g, b;

			// read red value
			scanner.Scan(',');

			AssertCondition(AttributeScanner.IsHexIntegerLiteral(scanner.Token) || AttributeScanner.IsIntegerLiteral(scanner.Token),
				DomMsgID.IntegerExpected, scanner.Token);

			r = scanner.GetTokenValueAsUInt();
			AssertCondition(r >= 0 && r <= 255, DomMsgID.InvalidRange, "0 - 255");

			AssertCondition(scanner.PeekChar(','), DomMsgID.MissingComma, scanner.Token);

			// read green value
			scanner.Scan(',');

			AssertCondition(AttributeScanner.IsHexIntegerLiteral(scanner.Token) || AttributeScanner.IsIntegerLiteral(scanner.Token),
				DomMsgID.IntegerExpected, scanner.Token);

			g = scanner.GetTokenValueAsUInt();
			AssertCondition(g >= 0 && g <= 255, DomMsgID.InvalidRange, "0 - 255");

			scanner.Scan(')');

			// read blue value

			AssertCondition(AttributeScanner.IsHexIntegerLiteral(scanner.Token) || AttributeScanner.IsIntegerLiteral(scanner.Token),
				DomMsgID.IntegerExpected, scanner.Token);

			b = scanner.GetTokenValueAsUInt();
			AssertCondition(b >= 0 && b <= 255, DomMsgID.InvalidRange, "0 - 255");

			return new Color(0xFF000000 | (r << 16) | (g << 8) | b);
		}


		/// <summary>
		/// Parses «CMYK(c, m, y, k)» or «CMYK(a, c, m, y, k)».
		/// </summary>
		private Color ParseCMYK()
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Parses «GRAY(g)».
		/// </summary>
		private Color ParseGray()
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// Determines the name/text of the given symbol.
		/// </summary>
		private string GetSymbolText(XmlSymbol docSym)
		{
			return XmlKeyWords.NameFromSymbol(docSym);
		}

		/// <summary>
		/// Returns whether the specified type is a valid SpaceType.
		/// </summary>
		private bool IsSpaceType(string type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (type == "")
				throw new ArgumentException("type");

			if (Enum.IsDefined(typeof(SymbolName), type))
			{
				SymbolName symbolName = (SymbolName)Enum.Parse(typeof(SymbolName), type, false); // symbols are case sensitive
				switch (symbolName)
				{
					case SymbolName.Blank:
					case SymbolName.Em:
					//case SymbolName.Em4: // same as SymbolName.EmQuarter
					case SymbolName.EmQuarter:
					case SymbolName.En:
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Returns whether the specified type is a valid enum for \symbol.
		/// </summary>
		private bool IsSymbolType(string type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (type == "")
				throw new ArgumentException("type");

			if (Enum.IsDefined(typeof(SymbolName), type))
			{
				SymbolName symbolName = (SymbolName)Enum.Parse(typeof(SymbolName), type, false); // symbols are case sensitive
				switch (symbolName)
				{
					case SymbolName.Euro:
					case SymbolName.Copyright:
					case SymbolName.Trademark:
					case SymbolName.RegisteredTrademark:
					case SymbolName.Bullet:
					case SymbolName.Not:
					case SymbolName.EmDash:
					case SymbolName.EnDash:
					case SymbolName.NonBreakableBlank:
						//case SymbolName.HardBlank: //same as SymbolName.NonBreakableBlank:
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// If cond is evaluated to false, a DdlParserException with the specified error will be thrown.
		/// </summary>
		private void AssertCondition(bool cond, DomMsgID error, params object[] args)
		{
			if (!cond)
				ThrowParserException(error, args);
		}


		/// <summary>
		/// If current symbol is not equal symbol a DdlParserException will be thrown.
		/// </summary>
		private void AssertSymbol(XmlSymbol symbol, bool endElement = false)
		{

			bool assert;
			if (endElement)
				assert = IsEndElement(symbol) || IsEmptyElement(symbol);
			else
				assert = IsStartElement(symbol) || IsEmptyElement(symbol);

			if (!assert)
				ThrowParserException(DomMsgID.SymbolExpected, endElement ? "/" : "" + XmlKeyWords.NameFromSymbol(symbol), _reader.Name);
		}

		/// <summary>
		/// If current symbol is not equal symbol a DdlParserException with the specified message id
		/// will be thrown.
		/// </summary>
		private void AssertSymbol(XmlSymbol symbol, DomMsgID err, bool endElement = false)
		{
			bool assert;
			if (endElement)
				assert = IsEndElement(symbol) || IsEmptyElement(symbol);
			else
				assert = IsStartElement(symbol) || IsEmptyElement(symbol);

			if (!assert)
				ThrowParserException(err, XmlKeyWords.NameFromSymbol(symbol), _reader.Name);
		}

		/// <summary>
		/// If current symbol is not equal symbol a DdlParserException with the specified message id
		/// will be thrown.
		/// </summary>
		private void AssertSymbol(XmlSymbol symbol, DomMsgID err, bool endElement = false, params object[] parms)
		{
			bool assert;
			if (endElement)
				assert = IsEndElement(symbol) || IsEmptyElement(symbol);
			else
				assert = IsStartElement(symbol) || IsEmptyElement(symbol);

			if (!assert)
				ThrowParserException(err, XmlKeyWords.NameFromSymbol(symbol), parms);
		}


		/// <summary>
		/// Creates an ErrorInfo based on the given errorlevel, error and parms and adds it to the ErrorManager2.
		/// </summary>
		private void ReportParserInfo(DdlErrorLevel level, DomMsgID errorCode, params string[] parms)
		{
			string message = DomSR.FormatMessage(errorCode, parms);

			int currentLine = 0;
			int currentLinePos = 0;
			if (_reader is IXmlLineInfo)
			{
				var lineInfo = _reader as IXmlLineInfo;
				currentLine = lineInfo.LineNumber;
				currentLinePos = lineInfo.LinePosition;
			}

			DdlReaderError error = new DdlReaderError(level, message, (int)errorCode,
				_fileName, currentLine, currentLinePos);

			_errors.AddError(error);
		}

		/// <summary>
		/// Creates an ErrorInfo based on the given error and parms and adds it to the ErrorManager2.
		/// </summary>
		private void ReportParserException(DomMsgID error, params string[] parms)
		{
			ReportParserException(null, error, parms);
		}

		/// <summary>
		/// Adds the ErrorInfo from the ErrorInfoException2 to the ErrorManager2.
		/// </summary>
		private void ReportParserException(DdlParserException ex)
		{
			_errors.AddError(ex.Error);
		}

		/// <summary>
		/// Creates an ErrorInfo based on the given inner exception, error and parms and adds it to the ErrorManager2.
		/// </summary>
		private void ReportParserException(Exception innerException, DomMsgID errorCode, params string[] parms)
		{
			string message = "";
			if (innerException != null)
				message = ": " + innerException;

			message += DomSR.FormatMessage(errorCode, parms);

			int currentLine = 0;
			int currentLinePos = 0;
			if (_reader is IXmlLineInfo)
			{
				var lineInfo = _reader as IXmlLineInfo;
				currentLine = lineInfo.LineNumber;
				currentLinePos = lineInfo.LinePosition;
			}

			DdlReaderError error = new DdlReaderError(DdlErrorLevel.Error, message, (int)errorCode,
			   _fileName, currentLine, currentLinePos);

			_errors.AddError(error);
		}

		/// <summary>
		/// Creates an ErrorInfo based on the DomMsgID and the specified parameters.
		/// Throws a DdlParserException with that ErrorInfo.
		/// </summary>
		private void ThrowParserException(DomMsgID errorCode, params object[] parms)
		{
			string message = DomSR.FormatMessage(errorCode, parms);

			int currentLine = 0;
			int currentLinePos = 0;
			if (_reader is IXmlLineInfo)
			{
				var lineInfo = _reader as IXmlLineInfo;
				currentLine = lineInfo.LineNumber;
				currentLinePos = lineInfo.LinePosition;
			}

			DdlReaderError error = new DdlReaderError(DdlErrorLevel.Error, message, (int)errorCode,
			   _fileName, currentLine, currentLinePos
				);

			throw new DdlParserException(error);
		}

		/// <summary>
		/// Determines the error message based on the DomMsgID and the parameters.
		/// Throws a DdlParserException with that error message and the Exception as the inner exception.
		/// </summary>
		private void ThrowParserException(Exception innerException, DomMsgID errorCode, params object[] parms)
		{
			string message = DomSR.FormatMessage(errorCode, parms);
			throw new DdlParserException(message, innerException);
		}

		/// <summary>
		/// Used for exception handling. Sets the DDL stream to the next valid position behind
		/// the current block.
		/// </summary>
		private void AdjustToNextBlock(XmlSymbol symbol)
		{

			if (!IsEmptyElement())
				MoveToEndElement(symbol);

			MoveNext();

			if (_reader.EOF)
				ThrowParserException(DomMsgID.UnexpectedEndOfFile);

		}

		private void AdjustToNextBlock(String name)
		{
			if (!IsEmptyElement())
				MoveToEndElement(name);

			MoveNext();

			if (_reader.EOF)
				ThrowParserException(DomMsgID.UnexpectedEndOfFile);
		}

		/// <summary>
		/// Shortcut for scanner.ReadText().
		/// Reads either text or \keyword from current position.
		/// </summary>
		private string ReadText()
		{

			string text = String.Empty;

			_reader.MoveToContent();

			bool loop = true;
			while (loop)
			{

				if (_reader.NodeType == XmlNodeType.CDATA || _reader.NodeType == XmlNodeType.Text)
				{
					text += _reader.Value;
					MoveNext(true);
				}
				else
					if (_reader.NodeType == XmlNodeType.EndElement)
					loop = false;
			}

			return text;

		}




		/// <summary>
		/// Gets the current symbol from the scanner.
		/// </summary>
		private XmlSymbol XmlSymbol
		{
			get
			{
				XmlSymbol symbol;

				if (_reader.EOF)
					symbol = XmlSymbol.Eof;
				else
				{
					_reader.MoveToContent();

					if (_reader.NodeType == XmlNodeType.Element || _reader.NodeType == XmlNodeType.EndElement)
						symbol = XmlKeyWords.SymbolFromName(_reader.Name);
					else
						symbol = XmlSymbol.None;
				}
				return symbol;
			}
		}


		private readonly XmlReader _reader;
		private readonly DdlReaderErrors _errors;

		private readonly string _fileName;

		/* Xml functionality, might be moved to an helper class*/


		private bool MoveToEndElement(XmlSymbol symbol)
		{
			_reader.MoveToContent();

			do
			{
				if ((_reader.NodeType == XmlNodeType.EndElement || (_reader.NodeType == XmlNodeType.Element
					&& _reader.IsEmptyElement))
					&& (symbol == XmlKeyWords.SymbolFromName(_reader.Name)))
					return true;

				MoveNext();

			} while (!_reader.EOF);

			return false;
		}


		private bool MoveToEndElement(string name)
		{
			_reader.MoveToContent();

			do
			{
				if ((_reader.NodeType == XmlNodeType.EndElement ||
					(_reader.NodeType == XmlNodeType.Element && _reader.IsEmptyElement))
					&& (_reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
					return true;

				MoveNext();

			} while (!_reader.EOF);



			return false;
		}



		private bool MoveNext(bool skipText = true)
		{
			_reader.MoveToContent();

			while (_reader.Read())
			{
				if (_reader.NodeType == XmlNodeType.EndElement
					|| _reader.NodeType == XmlNodeType.Element
					|| (!skipText
						&& (_reader.NodeType == XmlNodeType.Text
						|| _reader.NodeType == XmlNodeType.CDATA)))
					return true;
			}

			return false;
		}

		private bool IsStartElement(XmlSymbol symbol = XmlSymbol.None)
		{
			_reader.MoveToContent();
			return ((_reader.NodeType == XmlNodeType.Element && !_reader.IsEmptyElement)
				&& (symbol == XmlSymbol.None ? true : XmlKeyWords.SymbolFromName(_reader.Name) == symbol));
		}

		private bool IsTextElement()
		{
			_reader.MoveToContent();
			return (_reader.NodeType == XmlNodeType.Text || _reader.NodeType == XmlNodeType.CDATA);
		}

		private bool IsEmptyElement(XmlSymbol symbol = XmlSymbol.None)
		{
			_reader.MoveToContent();
			return ((_reader.NodeType == XmlNodeType.Element && _reader.IsEmptyElement)
				&& (symbol == XmlSymbol.None ? true : XmlKeyWords.SymbolFromName(_reader.Name) == symbol));
		}


		private bool IsEndElement(XmlSymbol symbol = XmlSymbol.None)
		{
			_reader.MoveToContent();
			return (_reader.NodeType == XmlNodeType.EndElement
				&& (symbol == XmlSymbol.None ? true : XmlKeyWords.SymbolFromName(_reader.Name) == symbol));
		}

		private bool IsEndElement(string name)
		{
			_reader.MoveToContent();
			return (_reader.NodeType == XmlNodeType.EndElement && _reader.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

		}

		private AttrDictionary ParseElementAttributes()
		{

			AttrDictionary attributes = new AttrDictionary(StringComparer.OrdinalIgnoreCase);

			_reader.MoveToContent();
			if (_reader.HasAttributes)
			{
				while (_reader.MoveToNextAttribute())

					attributes[_reader.Name] = _reader.Value;


			}

			return attributes;
		}

		private static string GetAttributeValue(AttrDictionary attributes, string key, bool remove = false)
		{
			string value = null;
			if (attributes.ContainsKey(key))
			{
				value = attributes[key];

				if (remove)
					attributes.Remove(key);
			}

			return value;
		}

		private static bool GetAttributeValueAsBool(AttrDictionary attributes, string key, bool remove = false)
		{
			var value = GetAttributeValue(attributes, key, remove);
			if (value == null)
				return false;
			else
				return value.ToLower() == "true";
		}


		private string RemoveLeadingWhiteSpace(string text)
		{
			if (text != null)
			{
				int idx = 0;
				while (idx < text.Length)
				{
					var ch = text[idx];
					if (!Char.IsWhiteSpace(ch))
						break;
					idx++;
				}

				if (idx > 0)
					text = text.Remove(0, idx);

			}

			return text;
		}

		private string RemoveTrailingWhiteSpace(string text)
		{
			if (text != null)
			{
				int idx = text.Length - 1;
				while (idx > 0)
				{
					var ch = text[idx];
					if (!Char.IsWhiteSpace(ch))
						break;
					idx--;
				}

				if (idx + 1 < text.Length)
					text = text.Remove(idx + 1);

			}

			return text;
		}
	}
}
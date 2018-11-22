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
using System.IO;
using System.Text;
using System.Xml;
using MigraDoc.DocumentObjectModel.Internals;

namespace MigraDoc.DocumentObjectModel
{
    /// <summary>
    /// Object to be passed to the Serialize function of a DocumentObject to convert
    /// it into DDL.
    /// </summary>
    internal sealed class XmlSerializer
    {
        /// <summary>
        /// A Serializer object for converting MDDOM into DDL.
        /// </summary>
        /// <param name="textWriter">A TextWriter to write DDL in.</param>
        /// <param name="indent">Indent of a new block. Default is 2.</param>
        /// <param name="initialIndent">Initial indent to start with.</param>
        internal XmlSerializer(TextWriter textWriter, int indent, int initialIndent)
        {
            if (textWriter == null)
                throw new ArgumentNullException("textWriter");

            _textWriter = textWriter;
            _indent = indent;
            _writeIndent = initialIndent;

            _xmlWriter = XmlWriter.Create(_textWriter, new XmlWriterSettings() { Encoding = Encoding.UTF8 });

            // todo
            //if (textWriter is StreamWriter)
            //    WriteStamp();
        }

        /// <summary>
        /// Initializes a new instance of the Serializer class with the specified TextWriter.
        /// </summary>
        internal XmlSerializer(TextWriter textWriter) : this(textWriter, 2, 0) { }

        /// <summary>
        /// Initializes a new instance of the Serializer class with the specified TextWriter and indentation.
        /// </summary>
        internal XmlSerializer(TextWriter textWriter, int indent) : this(textWriter, indent, 0) { }

        readonly TextWriter _textWriter;

        /// <summary>
        /// Gets or sets the indentation for a new indentation level.
        /// </summary>
        internal int Indent
        {
            get { return _indent; }
            set { _indent = value; }
        }
        int _indent = 2;

        /// <summary>
        /// Gets or sets the initial indentation which precede every line.
        /// </summary>
        internal int InitialIndent
        {
            get { return _writeIndent; }
            set { _writeIndent = value; }
        }
        int _writeIndent;

        /// <summary>
        /// Increases indent of DDL code.
        /// </summary>
        void IncreaseIndent()
        {
            _writeIndent += _indent;
        }

        /// <summary>
        /// Decreases indent of DDL code.
        /// </summary>
        void DecreaseIndent()
        {
            _writeIndent -= _indent;
        }

        /// <summary>
        /// Writes the header for a DDL file containing copyright and creation time information.
        /// </summary>
        internal void WriteStamp()
        {
            if (_fWriteStamp)
            {
                WriteComment("Created by empira MigraDoc Document Object Model");
                WriteComment(String.Format("generated file created {0:d} at {0:t}", DateTime.Now));
            }
        }


        internal void WriteComment(string comment)
        {
            if (String.IsNullOrEmpty(comment))
                return;

            WriteSimpleAttribute("Comment", comment);
        }



        /// <summary>
        /// Write attribute of type Unit, Color, int, float, double, bool, string or enum.
        /// </summary>
        internal void WriteSimpleAttribute(string valueName, object value)
        {
            INullableValue ival = value as INullableValue;
            if (ival != null)
                value = ival.GetValue();

            Type type = value.GetType();

            if (type == typeof(Unit))
            {
                string strUnit = value.ToString();
                if (((Unit)value).Type == UnitType.Point)
                    //WriteLine(valueName + " = " + strUnit);
                    _xmlWriter.WriteAttributeString(valueName, strUnit);
                else
                    //WriteLine(valueName + " = \"" + strUnit + "\"");
                    _xmlWriter.WriteAttributeString(valueName, strUnit);
            }
            else if (type == typeof(float))
            {
                //WriteLine(valueName + " = " + ((float)value).ToString(CultureInfo.InvariantCulture));
                _xmlWriter.WriteAttributeString(valueName, ((float)value).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == typeof(double))
            {
                //WriteLine(valueName + " = " + ((double)value).ToString(CultureInfo.InvariantCulture));
                _xmlWriter.WriteAttributeString(valueName, ((double)value).ToString(CultureInfo.InvariantCulture));
            }
            else if (type == typeof(bool))
            {
                //WriteLine(valueName + " = " + value.ToString().ToLower());
                _xmlWriter.WriteAttributeString(valueName, value.ToString().ToLower());
            }
            else if (type == typeof(string))
            {
                //StringBuilder sb = new StringBuilder(value.ToString());

                //sb.Replace("\\", "\\\\");
                //sb.Replace("\"", "\\\"");
                //WriteLine(valueName + " = \"" + sb + "\"");
                _xmlWriter.WriteAttributeString(valueName, value.ToString());
            }
#if !NETFX_CORE
            else if (type == typeof(int) || type.BaseType == typeof(Enum) || type == typeof(Color))
#else
            else if (type == typeof(int) || type.GetTypeInfo().BaseType == typeof(Enum) || type == typeof(Color))
#endif
            {
                //WriteLine(valueName + " = " + value);
                _xmlWriter.WriteAttributeString(valueName, value.ToString());
            }
            else
            {
                string message = String.Format("Type '{0}' of value '{1}' not supported", type, valueName);
                Debug.Assert(false, message);
            }
        }


        /// <summary>
        /// Flushes the buffers of the underlying text writer.
        /// </summary>
        internal void Flush()
        {
            //_textWriter.Flush();
            _xmlWriter.Flush();
        }

        /// <summary>
        /// Returns an indent string of blanks.
        /// </summary>
        static string Ind(int indent)
        {
            return new String(' ', indent);
        }


        bool _fWriteStamp = false;

        /* XML */

        readonly XmlWriter _xmlWriter;

		internal void WriteStartDocument()
        {
            _xmlWriter.WriteStartDocument();
        }

        internal void WriteEndDocument()
        {
            _xmlWriter.WriteEndDocument();

            // todo force close
            _xmlWriter.Close();
        }

        internal void WriteStartElement(string name)
        {
            _xmlWriter.WriteStartElement(name);
        }

        internal void WriteEndElement()
        {
            _xmlWriter.WriteEndElement();
        }

        internal void WriteElement(string name, string value)
        {
            _xmlWriter.WriteStartElement(name);
            _xmlWriter.WriteValue(value);
            _xmlWriter.WriteEndElement();
        }

        internal void WriteElement(string name)
        {
            _xmlWriter.WriteStartElement(name);
            _xmlWriter.WriteEndElement();
        }

        internal void WriteValue(string value)
        {
            _xmlWriter.WriteValue(value);
        }

        internal void WriteContent(string value)
        {
			// TODO 
			//_xmlWriter.WriteCData(value);
			_xmlWriter.WriteString(value);

		}

        internal void BeginAttributes()
        {
            _xmlWriter.WriteStartElement("Attributes");
        }
        internal void EndAttributes()
        {
            _xmlWriter.WriteEndElement();
        }

    }
}

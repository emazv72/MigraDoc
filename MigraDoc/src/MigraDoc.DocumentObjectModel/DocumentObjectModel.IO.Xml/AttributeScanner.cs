

using System;
using System.Globalization;

namespace MigraDoc.DocumentObjectModel.IO.Xml
{
	internal class AttributeScanner
	{
		public string Token { get; private set; }

		private bool ignoreBlank;
		private string text;
		private int idx;

		public AttributeScanner(string text, bool ignoreBlank = true)
		{
			this.text = text;
			this.Token = "";
			this.idx = 0;
			this.ignoreBlank = ignoreBlank;
		}

		internal void Scan(char separator)
		{
			this.Token = "";

			while (idx < text.Length)
			{
				if (text[idx] == separator)
				{
					idx++;
					break;
				}
				else
				{
					if (ignoreBlank)
					{
						if (!IsWhiteSpace(text[idx]))
							this.Token += text[idx];
					}
					else
						this.Token += text[idx];

					idx++;
				}
			}

			//Text.Remove(0, i);
		}

		internal bool PeekChar(char ch)
		{
			var i = idx;

			while (i < text.Length)
			{
				if (ignoreBlank)
				{
					if (!IsWhiteSpace(text[i]))
						if (text[i] == ch)
							return true;

				}
				else
					if (text[i] == ch)
					return true;

				i++;

			}

			return false;

		}




		internal uint GetTokenValueAsUInt()
		{
			if (Token.Length > 2 && Token.ToLower().StartsWith("0x"))
			{
				string number = Token.Substring(2);
				return UInt32.Parse(number, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
			}

			if (IsIntegerLiteral(Token))
				return UInt32.Parse(Token, CultureInfo.InvariantCulture);

			return 0;
		}

		internal static bool IsWhiteSpace(char ch)
		{
			return Char.IsWhiteSpace(ch);
		}

		internal static bool IsIntegerLiteral(string value)
		{
			UInt32 i;
			return UInt32.TryParse(value, out i);
		}

		internal static bool IsHexIntegerLiteral(string value)
		{
			if (value.Length > 2 && value.ToLower().StartsWith("0x"))
			{
				string number = value.Substring(2);
				UInt32 i;
				return UInt32.TryParse(number, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out i);
			}
			else
				return false;
		}
	}
}

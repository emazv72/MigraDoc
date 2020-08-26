#region MigraDoc - Creating Documents on the Fly
//
// Authors:
//   Klaus Potzesny
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
using System.IO;
using System.Diagnostics;
using PdfSharp.Drawing;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering.Resources;

#if CORE_WITH_GDI || GDI
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
#endif

namespace MigraDoc.Rendering
{
    /// <summary>
    /// Renders images.
    /// </summary>
    internal class BarcodeRenderer : ShapeRenderer
    {
        internal BarcodeRenderer(XGraphics gfx, Barcode barcode, FieldInfos fieldInfos)
            : base(gfx, barcode, fieldInfos)
        {
            _barcode = barcode;
            ImageRenderInfo renderInfo = new ImageRenderInfo();
            renderInfo.DocumentObject = _shape;
            _renderInfo = renderInfo;
        }

        internal BarcodeRenderer(XGraphics gfx, RenderInfo renderInfo, FieldInfos fieldInfos)
            : base(gfx, renderInfo, fieldInfos)
        {
            _barcode = (Barcode)renderInfo.DocumentObject;
        }

        internal override void Format(Area area, FormatInfo previousFormatInfo)
        {

            ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;

            if (!_barcode._height.IsNull)
                formatInfo.Height = _barcode.Height.Point;
            else
                formatInfo.Height = XUnit.FromCentimeter(2.5);

            if (!_barcode._width.IsNull)
                formatInfo.Width = _barcode.Width.Point;
            else
                formatInfo.Width = XUnit.FromCentimeter(2.5);

            if (!_barcode._resolution.IsNull)
                formatInfo.Resolution = _barcode.Resolution;
            else
                formatInfo.Resolution = 72;

            base.Format(area, previousFormatInfo);

        }

        protected override XUnit ShapeHeight
        {
            get
            {
                ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;
                return formatInfo.Height + _lineFormatRenderer.GetWidth();
            }
        }

        protected override XUnit ShapeWidth
        {
            get
            {
                ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;
                return formatInfo.Width + _lineFormatRenderer.GetWidth();
            }
        }

        internal override void Render()
        {
            RenderFilling();

            ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;
            Area contentArea = _renderInfo.LayoutInfo.ContentArea;
            XRect destRect = new XRect(contentArea.X, contentArea.Y, formatInfo.Width, formatInfo.Height);

            if (formatInfo.Failure == ImageFailure.None)
            {
                XImage xImage = null;
                try
                {
                    XRect srcRect = new XRect(formatInfo.CropX, formatInfo.CropY, formatInfo.CropWidth, formatInfo.CropHeight);
                    xImage = CreateBarcode();
                    _gfx.DrawImage(xImage, destRect, srcRect, XGraphicsUnit.Point); //Pixel.
                }
                catch (Exception)
                {
                    if (_barcode._renderOnFailure.IsNull || _barcode._renderOnFailure.Value)
                        RenderFailureImage(destRect);
                }
                finally
                {
                    if (xImage != null)
                        xImage.Dispose();
                }
            }
            else
            {
                //if (_image._renderOnFailure.IsNull || _image._renderOnFailure.Value)
                //  RenderFailureImage(destRect);
            }

            RenderLine();
        }

        void RenderFailureImage(XRect destRect)
        {
            _gfx.DrawRectangle(XBrushes.LightGray, destRect);
            string failureString;
            ImageFormatInfo formatInfo = (ImageFormatInfo)RenderInfo.FormatInfo;

            failureString = Messages2.DisplayInvalidImageType;

            // Create stub font
            XFont font = new XFont("Courier New", 8);
            _gfx.DrawString(failureString, font, XBrushes.Red, destRect, XStringFormats.Center);
        }


        XImage CreateBarcode()
        {

            XImage image = null;

            ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;

#if CORE_WITH_GDI || GDI

            BarcodeFormat format = BarcodeFormat.All_1D;

            switch (_barcode.Type)
            {
                case BarcodeType.Barcode25i:
                    format = BarcodeFormat.ITF;
                    break;
                case BarcodeType.Barcode39:
                    format = BarcodeFormat.CODE_39;
                    break;
                case BarcodeType.Barcode128:
                    format = BarcodeFormat.CODE_128;
                    break;
                case BarcodeType.QRCode:
                    format = BarcodeFormat.QR_CODE;
                    break;
                default:
                    break;
            }

            if (String.IsNullOrEmpty(_barcode.Code))
                throw new ArgumentNullException("Code");

            var options = new EncodingOptions();

            int horzPixels = (int)(formatInfo.Width.Inch * formatInfo.Resolution);
            int vertPixels = (int)(formatInfo.Height.Inch * formatInfo.Resolution);

            options.Width = horzPixels;
            options.Height = vertPixels;

            var writer = new BarcodeWriter
            {
                Format = format,
                Options = options
            };

            var bmp = writer.Write(_barcode.Code);

            string fileName = Path.GetTempFileName();

            bmp.SetResolution((float)formatInfo.Resolution, (float)formatInfo.Resolution);
            bmp.Save(fileName, ImageFormat.Png);

            image = XImage.FromFile(fileName);

            /*
            if (File.Exists(fileName))
                File.Delete(fileName);
                */
#endif
            return image;

        }

        readonly Barcode _barcode;
        //string _imageFilePath;
        //ImageFailure _failure;
    }
}

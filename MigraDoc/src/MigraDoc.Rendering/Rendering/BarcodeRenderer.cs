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

            //_imageFilePath = _image.GetFilePath(_documentRenderer.WorkingDirectory);

            // The Image is stored in the string if path starts with "base64:", otherwise we check whether the file exists.
            /*if (!_imageFilePath.StartsWith("base64:") &&
                !XImage.ExistsFile(_imageFilePath))
            {
                _failure = ImageFailure.FileNotFound;
                Debug.WriteLine(Messages2.ImageNotFound(_image.Name), "warning");
            }*/

            //ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;

            // formatInfo.Failure = _failure;
            // formatInfo.ImagePath = _imageFilePath;

            CalculateImageDimensions();
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
                    //xImage = XImage.FromFile(formatInfo.ImagePath);
                    xImage = CreateXImage(/*formatInfo.ImagePath*/);
                    _gfx.DrawImage(xImage, destRect, srcRect, XGraphicsUnit.Point); //Pixel.
                }
                catch (Exception)
                {
                    //if (_image._renderOnFailure.IsNull || _image._renderOnFailure.Value)
                    //  RenderFailureImage(destRect);
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

        private void CalculateImageDimensions()
        {
            ImageFormatInfo formatInfo = (ImageFormatInfo)_renderInfo.FormatInfo;

            if (formatInfo.Failure == ImageFailure.None)
            {
                XImage xImage = null;

                try
                {
                    xImage = CreateXImage();

                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine(Messages2.InvalidImageType(ex.Message));
                    formatInfo.Failure = ImageFailure.InvalidType;
                }

                if (formatInfo.Failure == ImageFailure.None)
                {
                    try
                    {
                        XUnit usrWidth = _barcode.Width.Point;
                        XUnit usrHeight = _barcode.Height.Point;
                        bool usrWidthSet = !_barcode._width.IsNull;
                        bool usrHeightSet = !_barcode._height.IsNull;

                        XUnit resultWidth = usrWidth;
                        XUnit resultHeight = usrHeight;

                        Debug.Assert(xImage != null);
                        double xPixels = xImage.PixelWidth;
                        bool usrResolutionSet = !_barcode._resolution.IsNull;

                        double horzRes = usrResolutionSet ? _barcode.Resolution : xImage.HorizontalResolution;
                        double vertRes = usrResolutionSet ? _barcode.Resolution : xImage.VerticalResolution;

                        // ReSharper disable CompareOfFloatsByEqualityOperator
                        if (horzRes == 0 && vertRes == 0)
                        {
                            horzRes = 72;
                            vertRes = 72;
                        }
                        else if (horzRes == 0)
                        {
                            Debug.Assert(false, "How can this be?");
                            horzRes = 72;
                        }
                        else if (vertRes == 0)
                        {
                            Debug.Assert(false, "How can this be?");
                            vertRes = 72;
                        }
                        // ReSharper restore CompareOfFloatsByEqualityOperator

                        XUnit inherentWidth = XUnit.FromInch(xPixels / horzRes);
                        double yPixels = xImage.PixelHeight;
                        XUnit inherentHeight = XUnit.FromInch(yPixels / vertRes);

                        //bool lockRatio = _image.IsNull("LockAspectRatio") ? true : _image.LockAspectRatio;
                        bool lockRatio = _barcode._lockAspectRatio.IsNull || _barcode.LockAspectRatio;

                        double scaleHeight = _barcode.ScaleHeight;
                        double scaleWidth = _barcode.ScaleWidth;
                        //bool scaleHeightSet = !_image.IsNull("ScaleHeight");
                        //bool scaleWidthSet = !_image.IsNull("ScaleWidth");
                        bool scaleHeightSet = !_barcode._scaleHeight.IsNull;
                        bool scaleWidthSet = !_barcode._scaleWidth.IsNull;

                        if (lockRatio && !(scaleHeightSet && scaleWidthSet))
                        {
                            if (usrWidthSet && !usrHeightSet)
                            {
                                resultHeight = inherentHeight / inherentWidth * usrWidth;
                            }
                            else if (usrHeightSet && !usrWidthSet)
                            {
                                resultWidth = inherentWidth / inherentHeight * usrHeight;
                            }
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            else if (!usrHeightSet && !usrWidthSet)
                            {
                                resultHeight = inherentHeight;
                                resultWidth = inherentWidth;
                            }

                            if (scaleHeightSet)
                            {
                                resultHeight = resultHeight * scaleHeight;
                                resultWidth = resultWidth * scaleHeight;
                            }
                            if (scaleWidthSet)
                            {
                                resultHeight = resultHeight * scaleWidth;
                                resultWidth = resultWidth * scaleWidth;
                            }
                        }
                        else
                        {
                            if (!usrHeightSet)
                                resultHeight = inherentHeight;

                            if (!usrWidthSet)
                                resultWidth = inherentWidth;

                            if (scaleHeightSet)
                                resultHeight = resultHeight * scaleHeight;
                            if (scaleWidthSet)
                                resultWidth = resultWidth * scaleWidth;
                        }

                        formatInfo.CropWidth = (int)xPixels;
                        formatInfo.CropHeight = (int)yPixels;
                        if (_barcode._pictureFormat != null && !_barcode._pictureFormat.IsNull())
                        {
                            PictureFormat picFormat = _barcode.PictureFormat;
                            //Cropping in pixels.
                            XUnit cropLeft = picFormat.CropLeft.Point;
                            XUnit cropRight = picFormat.CropRight.Point;
                            XUnit cropTop = picFormat.CropTop.Point;
                            XUnit cropBottom = picFormat.CropBottom.Point;
                            formatInfo.CropX = (int)(horzRes * cropLeft.Inch);
                            formatInfo.CropY = (int)(vertRes * cropTop.Inch);
                            formatInfo.CropWidth -= (int)(horzRes * ((XUnit)(cropLeft + cropRight)).Inch);
                            formatInfo.CropHeight -= (int)(vertRes * ((XUnit)(cropTop + cropBottom)).Inch);

                            //Scaled cropping of the height and width.
                            double xScale = resultWidth / inherentWidth;
                            double yScale = resultHeight / inherentHeight;

                            cropLeft = xScale * cropLeft;
                            cropRight = xScale * cropRight;
                            cropTop = yScale * cropTop;
                            cropBottom = yScale * cropBottom;

                            resultHeight = resultHeight - cropTop - cropBottom;
                            resultWidth = resultWidth - cropLeft - cropRight;
                        }
                        if (resultHeight <= 0 || resultWidth <= 0)
                        {
                            formatInfo.Width = XUnit.FromCentimeter(2.5);
                            formatInfo.Height = XUnit.FromCentimeter(2.5);
                            Debug.WriteLine(Messages2.EmptyImageSize);
                            //_failure = ImageFailure.EmptySize;
                        }
                        else
                        {
                            formatInfo.Width = resultWidth;
                            formatInfo.Height = resultHeight;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(Messages2.ImageNotReadable(_barcode.Code, ex.Message));
                        formatInfo.Failure = ImageFailure.NotRead;
                    }
                    finally
                    {
                        if (xImage != null)
                            xImage.Dispose();
                    }
                }
            }
            if (formatInfo.Failure != ImageFailure.None)
            {
                if (!_barcode._width.IsNull)
                    formatInfo.Width = _barcode.Width.Point;
                else
                    formatInfo.Width = XUnit.FromCentimeter(2.5);

                if (!_barcode._height.IsNull)
                    formatInfo.Height = _barcode.Height.Point;
                else
                    formatInfo.Height = XUnit.FromCentimeter(2.5);
            }
        }

        XImage CreateXImage()
        {

            XImage image = null;

#if CORE_WITH_GDI || GDI

            if (String.IsNullOrEmpty(_barcode.Code))
                throw new ArgumentNullException("Code");

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

            var options = new EncodingOptions();

            // hack
            const float resolution = 200;
            int horzPixels = (int)(_barcode.Width.Inch * resolution);
            int vertPixels = (int)(_barcode.Height.Inch * resolution);

            options.Width = horzPixels;
            options.Height = vertPixels;

            var writer = new BarcodeWriter
            {
                Format = format,
                Options = options
            };

            var bmp = writer.Write(_barcode.Code);

            string fileName = Path.GetTempFileName();

            bmp.SetResolution(resolution, resolution);
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

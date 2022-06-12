using BarcodeGenerator.Helper;
using BarcodeGenerator.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace BarcodeGenerator
{
    public static class BarcodeGenerator
    {
        [FunctionName("BarcodeGenerator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            BarcodeModel mBarcodeModel = JsonConvert.DeserializeObject<BarcodeModel>(requestBody);

            BarcodeFormat symbology = BarcodeFormat.AZTEC;

            Response<string> response = Validations(mBarcodeModel, ref symbology);
            if (!response.IsSuccess)
            {
                return new BadRequestObjectResult(response.Message);
            }
            
            return WriterPngData(mBarcodeModel, symbology);
        }
        
        private static Response<string> Validations(BarcodeModel mBarcodeModel, ref BarcodeFormat symbology)
        {
            mBarcodeModel.OutputFormat = "png";

            // Validate OutputFormat
            if (mBarcodeModel.OutputFormat != "png" && mBarcodeModel.OutputFormat != "svg")
            {
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"Invalid output file format, Value must be 'png' or 'svg'."
                };
            }

            symbology = (BarcodeFormat)Enum.Parse(typeof(BarcodeFormat), mBarcodeModel.Symbology);
            if (!Enum.IsDefined(typeof(BarcodeFormat), symbology) && !symbology.ToString().Contains(","))
            {
                throw new InvalidOperationException($"{symbology} is not an underlying value of the BarcodeFormat enumeration.");
            }

            // Validate Length, ImageHeight, ImageWidth and margin.
            if (mBarcodeModel.ImageHeight < Constants.MinImageHeight || mBarcodeModel.ImageHeight > Constants.MaxImageHeight)
            {
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"Height must be between {Constants.MinImageHeight} and {Constants.MaxImageHeight}."
                };
            }
            if (mBarcodeModel.ImageWidth < Constants.MinImageWidth || mBarcodeModel.ImageWidth > Constants.MaxImageWidth)
            {
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"Width must be between {Constants.MinImageWidth} and {Constants.MaxImageWidth}."
                };
            }
            if (mBarcodeModel.Margin < 0 || mBarcodeModel.Margin > Constants.MaxMargin)
            {
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"Margin must be between 0 and {Constants.MaxMargin}."
                };
            }
            if (mBarcodeModel.Value.Length > Constants.MaxValueLength)
            {
                return new Response<string>
                {
                    IsSuccess = false,
                    Message = $"Invalid length for value. This API will not render a barcode longer than " +
                    $"{Constants.MaxValueLength} characters."
                };
            }

            return new Response<string>
            {
                IsSuccess = true
            };

        }

        private static IActionResult WriterPngData(BarcodeModel mBarcodeModel, BarcodeFormat symbology)
        {
            EncodingOptions options = new EncodingOptions
            {
                Height = mBarcodeModel.ImageHeight,
                Width = mBarcodeModel.ImageWidth,
                Margin = mBarcodeModel.Margin,
            };

            // Branch depending on whether we're outputting a PNG graphics file or an SVG.
            try
            {
                BarcodeWriterPixelData WriterPngData = new BarcodeWriterPixelData
                {
                    Format = symbology,
                    Options = options,
                };

                PixelData pixelData = WriterPngData.Write(mBarcodeModel.Value);

                using (Image<Rgba32> img = Image.LoadPixelData<Rgba32>(pixelData.Pixels, pixelData.Width, pixelData.Height))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.SaveAsPng(ms);
                        return new FileContentResult(ms.ToArray(), "image/png");
                    }
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult($"Exception: {ex.Message}.");
            }
        }

        //private static IActionResult WriterSvgData(BarcodeModel mBarcodeModel, BarcodeFormat symbology)
        //{
        //    BarcodeWriterSvg WriterSvgData = new BarcodeWriterSvg()
        //    {
        //        Format = symbology,
        //    };
        //    SvgRenderer.SvgImage svg = WriterSvgData.Write(mBarcodeModel.Value);
        //    byte[] bytes = Encoding.UTF8.GetBytes(svg.ToString());
        //    return new FileContentResult(bytes, "image/svg+xml");
        //}
    }
}
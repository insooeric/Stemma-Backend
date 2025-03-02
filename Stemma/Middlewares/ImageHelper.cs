using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;

namespace Stemma.Middlewares
{
    public static class ImageHelper
    {
        // !!NOTE!!
        // !!NOTE!!
        // fileName is unique
        // !!NOTE!!
        // !!NOTE!!
        public static string ConvertToSVG(IFormFile file, string fileName)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            string extension = Path.GetExtension(file.FileName).ToLower();
            string[] convertExtensions = { ".png", ".jpg", ".jpeg" };
            string newSvgContent = string.Empty;

            if (convertExtensions.Contains(extension))
            {
                string mimeType = (extension == ".jpg" || extension == ".jpeg") ? "image/jpeg" : "image/png";
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                int newHeight = 100;
                int newWidth = GetWidthByHeight(newHeight, fileBytes);

                string base64Image = Convert.ToBase64String(fileBytes);

                newSvgContent = $@"
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""{newWidth}px"" height=""{newHeight}px"" x=""0"" y=""0"">
                  <defs>
                    <clipPath id=""clip-{fileName}"">
                      <rect width=""100%"" height=""100%"" rx=""8"" />
                    </clipPath>
                  </defs>
                  <image href=""data:{mimeType};base64,{base64Image}"" width=""100%"" height=""100%"" 
                         clip-path=""url(#clip-{fileName})"" preserveAspectRatio=""xMidYMid meet"" />
                </svg>";
            }
            else if (extension.Equals(".svg"))
            {
                /*
                 * FUCK WE DO HAVE PROBLEM HERE
                 */
                //Console.WriteLine("Parsing svg to png");

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }
                string originalSvg = Encoding.UTF8.GetString(fileBytes);
                // Console.WriteLine($"-------------\nOriginal SVG:\n{originalSvg}\n------------\n");
                // OK. nothing WRONG till this point. WE ARE GETTING ORIGINAL SVG

                // let's try to get the viewbox attribute.
                string outerViewBox = "";
                double origWidth = 0, origHeight = 0;
                Match viewBoxMatch = Regex.Match(originalSvg, @"viewBox\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (viewBoxMatch.Success)
                {
                    outerViewBox = viewBoxMatch.Groups[1].Value.Trim();
                    string[] parts = outerViewBox.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4 &&
                        double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out origWidth) &&
                        double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out origHeight))
                    {
                        // Console.WriteLine("yeet");
                    }
                }
                if (origWidth <= 0 || origHeight <= 0)
                {
                    // if we can't find the viewbox, fallback; extract width and height
                    Match widthMatch = Regex.Match(originalSvg, @"width\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    Match heightMatch = Regex.Match(originalSvg, @"height\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    if (widthMatch.Success && heightMatch.Success)
                    {
                        string wStr = Regex.Replace(widthMatch.Groups[1].Value, "[^0-9.]", "");
                        string hStr = Regex.Replace(heightMatch.Groups[1].Value, "[^0-9.]", "");
                        if (!double.TryParse(wStr, NumberStyles.Any, CultureInfo.InvariantCulture, out origWidth))
                            origWidth = 160;
                        if (!double.TryParse(hStr, NumberStyles.Any, CultureInfo.InvariantCulture, out origHeight))
                            origHeight = 160;
                        outerViewBox = $"0 0 {origWidth} {origHeight}";
                    }
                    else
                    {
                        // in case we still can't define just use magic numbers
                        origWidth = 160;
                        origHeight = 160;
                        outerViewBox = "0 0 160 160";
                    }
                }
                // Console.WriteLine($"Extracted viewBox: {outerViewBox} (w={origWidth}, h={origHeight})");
                // OK. nothing WRONG till this point. WE ARE GETTING DA VIEWBOX

                // configure new dimension
                int newHeight = 100;
                int newWidth = (int)Math.Round(newHeight * (origWidth / origHeight));
                // Console.WriteLine($"Computed dimensions: Height: {newHeight}, Width: {newWidth}");
                // OK. nothing WRONG till this point. WE ARE GETTING WIDTH

                // let's try to get the inner content for original svg
                int firstTagClose = originalSvg.IndexOf('>');
                int lastTagOpen = originalSvg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
                if (firstTagClose < 0 || lastTagOpen < 0 || lastTagOpen <= firstTagClose)
                    throw new ArgumentException("Invalid SVG content provided.");
                string innerContent = originalSvg.Substring(firstTagClose + 1, lastTagOpen - firstTagClose - 1).Trim();
                // Console.WriteLine($"-------------\nInner content:\n{innerContent}\n------------\n");
                // OK. nothing WRONG till this point. WE ARE GETTING INNER CONTENT FOR ORIGINAL SVG

                // if def exists in original svg, we're gonna extract it
                string originalDefsInner = "";
                Match defsMatch = Regex.Match(innerContent, @"<defs\b[^>]*>(.*?)</defs>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (defsMatch.Success)
                {
                    originalDefsInner = defsMatch.Groups[1].Value.Trim();
                    innerContent = Regex.Replace(innerContent, @"<defs\b[^>]*>.*?</defs>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                }
                // Console.WriteLine($"Extracted original defs inner content:\n{originalDefsInner}\n");
                // OK. nothing WRONG till this point. WE ARE GETTING DEFINITIONS FOR ORIGINAL SVG

                // let's create finalized defs
                string newDefs = $@"
  <defs>
    <clipPath id=""clip-{fileName}"">
      <rect x=""0"" y=""0"" width=""{origWidth}"" height=""{origHeight}"" rx=""8"" />
    </clipPath>
    {originalDefsInner}
  </defs>";
                // Console.WriteLine($"New defs block:\n{newDefs}\n");
                // OK. nothing WRONG till this point. WE ARE GETTING FINALIZED DEFINITION

                // let's assemble everything
                newSvgContent = $@"<svg xmlns=""http://www.w3.org/2000/svg"" 
       xmlns:xlink=""http://www.w3.org/1999/xlink"" 
       width=""{newWidth}px"" height=""{newHeight}px"" 
       viewBox=""{outerViewBox}"">
{newDefs}
  <g clip-path=""url(#clip-{fileName})"">
    {innerContent}
  </g>
</svg>";

                // Console.WriteLine($"-------------\nFinal SVG:\n{newSvgContent}\n------------\n");
                // OK. nothing WRONG till this point. WE ARE GETTING FINALIZED SVG
            }

            // testing ResizeSVG

            //int testwidth = GetWidthByHeight(40, newSvgContent);
            //string resizedSVG = ResizeSVG(newSvgContent, testwidth, 40);
            // Console.WriteLine($"-------------\nRESIZED SVG:\n{resizedSVG}\n------------\n");


            if (string.IsNullOrWhiteSpace(newSvgContent))
            {
                // Console.WriteLine("Something went wrong :(");
                throw new NotSupportedException("Unsupported file type for conversion to SVG.");
            }

            // Console.WriteLine(svgContent);
            return newSvgContent;
        }

        public static string FormatSvg(string svgContent, string fileName)
        {
            try
            {
                string originalSvg = svgContent;
                // Console.WriteLine($"-------------\nOriginal SVG:\n{originalSvg}\n------------\n");
                // OK. nothing WRONG till this point. WE ARE GETTING ORIGINAL SVG

                // let's try to get the viewbox attribute.
                string outerViewBox = "";
                double origWidth = 0, origHeight = 0;
                Match viewBoxMatch = Regex.Match(originalSvg, @"viewBox\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                if (viewBoxMatch.Success)
                {
                    outerViewBox = viewBoxMatch.Groups[1].Value.Trim();
                    string[] parts = outerViewBox.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4 &&
                        double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out origWidth) &&
                        double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out origHeight))
                    {
                        // Console.WriteLine("yeet");
                    }
                }
                if (origWidth <= 0 || origHeight <= 0)
                {
                    // if we can't find the viewbox, fallback and extract width and height
                    Match widthMatch = Regex.Match(originalSvg, @"width\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    Match heightMatch = Regex.Match(originalSvg, @"height\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                    if (widthMatch.Success && heightMatch.Success)
                    {
                        // Console.WriteLine("yeet");
                        // ok, we are detecting it
                        string wStr = Regex.Replace(widthMatch.Groups[1].Value, "[^0-9.]", "");
                        string hStr = Regex.Replace(heightMatch.Groups[1].Value, "[^0-9.]", "");
                        if (!double.TryParse(wStr, NumberStyles.Any, CultureInfo.InvariantCulture, out origWidth))
                            origWidth = 160;
                        if (!double.TryParse(hStr, NumberStyles.Any, CultureInfo.InvariantCulture, out origHeight))
                            origHeight = 160;
                        outerViewBox = $"0 0 {origWidth} {origHeight}";
                    }
                    else
                    {
                        // in case we still can't define just use magic numbers
                        origWidth = 160;
                        origHeight = 160;
                        outerViewBox = "0 0 160 160";
                    }
                }
                // Console.WriteLine($"Extracted viewBox: {outerViewBox} (w={origWidth}, h={origHeight})");
                // OK. nothing WRONG till this point. WE ARE GETTING DA VIEWBOX

                // configure new dimension
                int newHeight = 100;
                int newWidth = (int)Math.Round(newHeight * (origWidth / origHeight));
                // Console.WriteLine($"Computed dimensions: Height: {newHeight}, Width: {newWidth}");
                // OK. nothing WRONG till this point. WE ARE GETTING WIDTH

                // let's try to get the inner content for original svg
                int firstTagClose = originalSvg.IndexOf('>');
                int lastTagOpen = originalSvg.LastIndexOf("</svg>", StringComparison.OrdinalIgnoreCase);
                if (firstTagClose < 0 || lastTagOpen < 0 || lastTagOpen <= firstTagClose)
                    throw new ArgumentException("Invalid SVG content provided.");
                string innerContent = originalSvg.Substring(firstTagClose + 1, lastTagOpen - firstTagClose - 1).Trim();
                // Console.WriteLine($"-------------\nInner content:\n{innerContent}\n------------\n");
                // OK. nothing WRONG till this point. WE ARE GETTING INNER CONTENT FOR ORIGINAL SVG

                // if def exists in original svg, we're gonna extract it
                string originalDefsInner = "";
                Match defsMatch = Regex.Match(innerContent, @"<defs\b[^>]*>(.*?)</defs>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (defsMatch.Success)
                {
                    originalDefsInner = defsMatch.Groups[1].Value.Trim();
                    innerContent = Regex.Replace(innerContent, @"<defs\b[^>]*>.*?</defs>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
                }
                // Console.WriteLine($"Extracted original defs inner content:\n{originalDefsInner}\n");
                // OK. nothing WRONG till this point. WE ARE GETTING DEFINITIONS FOR ORIGINAL SVG

                // let's create finalized defs
                string newDefs = $@"
  <defs>
    <clipPath id=""clip-{fileName}"">
      <rect x=""0"" y=""0"" width=""{origWidth}"" height=""{origHeight}"" rx=""8"" />
    </clipPath>
    {originalDefsInner}
  </defs>";
                // Console.WriteLine($"New defs block:\n{newDefs}\n");
                // OK. nothing WRONG till this point. WE ARE GETTING FINALIZED DEFINITION

                // let's assemble everything
                string resultSvg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" 
       xmlns:xlink=""http://www.w3.org/1999/xlink"" 
       width=""{newWidth}px"" height=""{newHeight}px"" 
       viewBox=""{outerViewBox}"">
{newDefs}
  <g clip-path=""url(#clip-{fileName})"">
    {innerContent}
  </g>
</svg>";
                return resultSvg;
            }
            catch (Exception ex)
            {
                throw new Exception("Something went wrong :(");
            }
        }

        public static int GetWidthByHeight(int height, byte[] fileBytes)
        {
            using (var msForDimensions = new MemoryStream(fileBytes))
            {
                using (var image = SixLabors.ImageSharp.Image.Load(msForDimensions))
                {
                    return (int)(image.Width * (height / (double)image.Height));
                }
            }
        }

        public static string UpdatePosition(string svg, double x, double y)
        {
            int svgStartIndex = svg.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);
            if (svgStartIndex != -1)
            {
                int svgTagEndIndex = svg.IndexOf('>', svgStartIndex);
                if (svgTagEndIndex != -1)
                {
                    string firstSvgTag = svg.Substring(svgStartIndex, svgTagEndIndex - svgStartIndex + 1);

                    bool hasX = Regex.IsMatch(firstSvgTag, @"\bx\s*=", RegexOptions.IgnoreCase);
                    bool hasY = Regex.IsMatch(firstSvgTag, @"\by\s*=", RegexOptions.IgnoreCase);
                    string modifiedTag = firstSvgTag;

                    if (hasX && hasY)
                    {
                        modifiedTag = Regex.Replace(
                            modifiedTag,
                            @"\bx\s*=\s*[""'][^""']*[""']",
                            $"x=\"{x}\"",
                            RegexOptions.IgnoreCase);
                        modifiedTag = Regex.Replace(
                            modifiedTag,
                            @"\by\s*=\s*[""'][^""']*[""']",
                            $"y=\"{y}\"",
                            RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        string tagWithoutClose = firstSvgTag.TrimEnd('>');
                        if (!hasX)
                        {
                            tagWithoutClose += $" x=\"{x}\"";
                        }
                        if (!hasY)
                        {
                            tagWithoutClose += $" y=\"{y}\"";
                        }
                        modifiedTag = tagWithoutClose + ">";
                    }

                    svg = svg.Replace(firstSvgTag, modifiedTag);
                }
            }
            return svg;
        }

        public static int GetWidthByHeight(int height, string svgContent)
        {
            var (originalWidth, originalHeight) = GetSvgDimensions(svgContent);
            double scaleFactor = (double)height / originalHeight;
            return (int)Math.Round(originalWidth * scaleFactor);
        }

        //public static int GetHeightByWidth(int width, string svgContent)
        //{
        //    var (originalWidth, originalHeight) = GetSvgDimensions(svgContent);
        //    double scaleFactor = (double)width / originalWidth;
        //    return (int)Math.Round(originalHeight * scaleFactor);
        //}

        private static (double Width, double Height) GetSvgDimensions(string svgContent)
        {
            // Console.WriteLine(svgContent);

            if (string.IsNullOrWhiteSpace(svgContent))
                throw new ArgumentNullException(nameof(svgContent));

            var widthMatch = Regex.Match(svgContent, "width\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
            var heightMatch = Regex.Match(svgContent, "height\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);


            if (widthMatch.Success && heightMatch.Success)
            {
                double width = ParseDimension(widthMatch.Groups[1].Value);
                double height = ParseDimension(heightMatch.Groups[1].Value);
                return (width, height);
            }

            var viewBoxMatch = Regex.Match(svgContent, "viewBox\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
            if (viewBoxMatch.Success)
            {
                var parts = viewBoxMatch.Groups[1].Value
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 &&
                    double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double vbWidth) &&
                    double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double vbHeight))
                {
                    return (vbWidth, vbHeight);
                }
                else
                {
                    throw new ArgumentException("SVG viewBox attribute does not contain valid values.");
                }
            }

            throw new ArgumentException("SVG content does not contain valid width/height attributes or a viewBox attribute.");
        }

        private static double ParseDimension(string dimension)
        {
            string numericPart = Regex.Replace(dimension, "[^0-9.\\-]", "");
            if (double.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                return value;
            }

            throw new FormatException($"Unable to parse dimension: {dimension}");
        }

        // ISSUE WITH RESIZE <= solved

        public static string ResizeSVG(string svg, double newWidth, double newHeight)
        {
            string newWidthStr = newWidth.ToString("0.##", CultureInfo.InvariantCulture) + "px";
            string newHeightStr = newHeight.ToString("0.##", CultureInfo.InvariantCulture) + "px";

            Regex svgTagRegex = new Regex(@"<svg\b[^>]*>", RegexOptions.IgnoreCase);
            Match match = svgTagRegex.Match(svg);
            if (!match.Success)
            {
                return svg;
            }

            string originalSvgTag = match.Value;
            string updatedSvgTag = originalSvgTag;

            if (Regex.IsMatch(originalSvgTag, @"\bwidth\s*=\s*""[^""]*""", RegexOptions.IgnoreCase))
            {
                updatedSvgTag = Regex.Replace(
                    updatedSvgTag,
                    @"\bwidth\s*=\s*""[^""]*""",
                    $"width=\"{newWidthStr}\"",
                    RegexOptions.IgnoreCase
                );
            }
            else
            {
                updatedSvgTag = updatedSvgTag.Replace("<svg", $"<svg width=\"{newWidthStr}\"");
            }

            if (Regex.IsMatch(originalSvgTag, @"\bheight\s*=\s*""[^""]*""", RegexOptions.IgnoreCase))
            {
                updatedSvgTag = Regex.Replace(
                    updatedSvgTag,
                    @"\bheight\s*=\s*""[^""]*""",
                    $"height=\"{newHeightStr}\"",
                    RegexOptions.IgnoreCase
                );
            }
            else
            {
                updatedSvgTag = updatedSvgTag.Replace("<svg", $"<svg height=\"{newHeightStr}\"");
            }

            string result = svg.Substring(0, match.Index)
                            + updatedSvgTag
                            + svg.Substring(match.Index + match.Length);
            return result;
        }

        public static async Task<IFormFile> DownloadImageAsFormFileAsync(string imageUrl)
        {
            using (var client = new HttpClient())
            {
                // GitHub API requires a User-Agent header.
                client.DefaultRequestHeaders.UserAgent.ParseAdd("StemmaApp");
                var response = await client.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Create a FormFile from the memory stream.
                    IFormFile formFile = new FormFile(memoryStream, 0, memoryStream.Length, "avatar", "avatar.jpg");
                    return formFile;
                }
            }
        }
    }
}

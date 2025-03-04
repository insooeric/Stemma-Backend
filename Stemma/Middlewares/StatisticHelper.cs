using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class StatisticHelper
{
    public static string GetStatisticSvg(Dictionary<string, float> dataSet)
    {
        if (dataSet.Count < 2 || dataSet.Count > 5)
            throw new ArgumentException("You must have between 2 to 5 items.");

        List<KeyValuePair<string, float>> items = dataSet.ToList();
        var others = items.FirstOrDefault(x => x.Key.Equals("Others", StringComparison.OrdinalIgnoreCase));
        if (!others.Equals(default(KeyValuePair<string, float>)))
        {
            items.RemoveAll(x => x.Key.Equals("Others", StringComparison.OrdinalIgnoreCase));
            items.Add(others);
        }

        string[] colors = { "#3498db", "#6879C9", "#9b59b6", "#9978A6", "#969696" };

        float R = 125f;
        float currentAngle = -90f;

        StringBuilder svgPaths = new StringBuilder();
        StringBuilder svgIndicators = new StringBuilder();
        List<float> boundaryAngles = new List<float>();

        for (int i = 0; i < items.Count; i++)
        {
            float percentage = items[i].Value;
            float sweepAngle = percentage / 100f * 360f;
            float startAngle = currentAngle;
            float endAngle = currentAngle + sweepAngle;
            int largeArcFlag = (sweepAngle > 180f) ? 1 : 0;

            double startRad = startAngle * Math.PI / 180.0;
            double endRad = endAngle * Math.PI / 180.0;

            float startX = R * (float)Math.Cos(startRad);
            float startY = R * (float)Math.Sin(startRad);
            float endX = R * (float)Math.Cos(endRad);
            float endY = R * (float)Math.Sin(endRad);

            string selectedColor = items[i].Key.Equals("Others", StringComparison.OrdinalIgnoreCase)
                ? "#969696"
                : colors[i];

            string path = $"<path d=\"M0,0 L{startX:F2},{startY:F2} A{R},{R} 0 {largeArcFlag},1 {endX:F2},{endY:F2} Z\" fill=\"{selectedColor}\"/>";
            svgPaths.AppendLine(path);
            boundaryAngles.Add(endAngle);

            if (i != items.Count - 1)
            {
                float textOffset = 8.0f;
                float midTextAngle = startAngle + sweepAngle / 2f;
                double midRad = midTextAngle * Math.PI / 180.0;
                float midX = R * (float)Math.Cos(midRad);
                float midY = R * (float)Math.Sin(midRad);

                float overallX = 150 + 0.6f * midX;
                float overallY = 142.5f + 0.6f * midY;

                bool isRight = overallX > 150;
                bool isBelow = overallY > 142.5f;

                float diagOffset = 10f / (float)Math.Sqrt(2);

                float line1X, line1Y, line2EndX, line2EndY, textX, textY;
                if (isRight && !isBelow)
                {
                    line1X = overallX + diagOffset;
                    line1Y = overallY - diagOffset;
                    line2EndX = line1X + 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2 + textOffset;
                    textY = line1Y - 8;
                }
                else if (isRight && isBelow)
                {
                    line1X = overallX + diagOffset;
                    line1Y = overallY + diagOffset;
                    line2EndX = line1X + 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2 + textOffset;
                    textY = line1Y + 16;
                }
                else if (!isRight && isBelow)
                {
                    line1X = overallX - diagOffset;
                    line1Y = overallY + diagOffset;
                    line2EndX = line1X - 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2 - textOffset;
                    textY = line1Y + 16;
                }
                else
                {
                    line1X = overallX - diagOffset;
                    line1Y = overallY - diagOffset;
                    line2EndX = line1X - 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2 - textOffset;
                    textY = line1Y - 8;
                }

                string indicator = $"<line x1=\"{overallX:F2}\" y1=\"{overallY:F2}\" x2=\"{line1X:F2}\" y2=\"{line1Y:F2}\" stroke=\"white\" stroke-width=\"1\"/>\n" +
                                   $"<line x1=\"{line1X:F2}\" y1=\"{line1Y:F2}\" x2=\"{line2EndX:F2}\" y2=\"{line2EndY:F2}\" stroke=\"white\" stroke-width=\"1\"/>\n" +
                                   $"<text x=\"{textX:F2}\" y=\"{textY:F2}\" fill=\"white\" font-size=\"16\" font-family=\"Arial, sans-serif\" text-anchor=\"middle\">{items[i].Key}</text>";
                svgIndicators.AppendLine(indicator);
            }

            currentAngle += sweepAngle;
        }

        float shift = boundaryAngles[boundaryAngles.Count - 1];
        List<float> wedgeAngles = new List<float>();
        foreach (var angle in boundaryAngles)
        {
            float newAngle = (angle - shift + 360f) % 360f;
            wedgeAngles.Add(newAngle);
        }

        int idxZero = wedgeAngles.FindIndex(x => Math.Abs(x) < 0.01f);
        if (idxZero > 0)
        {
            wedgeAngles = wedgeAngles.Skip(idxZero)
                                     .Concat(wedgeAngles.Take(idxZero))
                                     .ToList();
        }

        List<string> selectedColorList = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            selectedColorList.Add(items[i].Key.Equals("Others", StringComparison.OrdinalIgnoreCase)
                ? "#969696"
                : colors[i]);
        }

        //foreach (var color in selectedColorList)
        //{
        //    Console.WriteLine(color);
        //}

        List<string> gradientDefsList = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            string startColor;
            string endColor;
            if (i == 0)
            {
                startColor = "#969696";
                endColor = selectedColorList[i];
            } else if(i == items.Count)
            {
                startColor = selectedColorList[i - 1];
                endColor = "#969696";
            } else
            {
                startColor = selectedColorList[i - 1];
                endColor = selectedColorList[i];
            }
                string grad = $@"<linearGradient id=""gradiant{i + 1}"" gradientUnits=""objectBoundingBox"" x1=""0"" y1=""0"" x2=""1"" y2=""0"">
  <stop offset=""0%"" stop-color=""{startColor}""/>
  <stop offset=""5%"" stop-color=""{startColor}""/>
  <stop offset=""95%"" stop-color=""{endColor}""/>
  <stop offset=""100%"" stop-color=""{endColor}""/>
</linearGradient>";
            gradientDefsList.Add(grad);
        }

        StringBuilder defsBuilder = new StringBuilder();
        defsBuilder.AppendLine("    <clipPath id=\"holeClip\">");
        defsBuilder.AppendLine("      <circle cx=\"0\" cy=\"0\" r=\"80\"/>");
        defsBuilder.AppendLine("    </clipPath>");
        defsBuilder.AppendLine("    <linearGradient id=\"histGradient\" gradientUnits=\"objectBoundingBox\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">");
        defsBuilder.AppendLine("      <stop offset=\"0%\" stop-color=\"#3498db\"/>");
        defsBuilder.AppendLine("      <stop offset=\"100%\" stop-color=\"#9b59b6\"/>");
        defsBuilder.AppendLine("    </linearGradient>");
        defsBuilder.AppendLine("    <linearGradient id=\"titleGradient\" gradientUnits=\"objectBoundingBox\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">");
        defsBuilder.AppendLine("      <stop offset=\"0%\" stop-color=\"#3498db\"/>");
        defsBuilder.AppendLine("      <stop offset=\"100%\" stop-color=\"#9b59b6\"/>");
        defsBuilder.AppendLine("    </linearGradient>");
        foreach (string grad in gradientDefsList)
        {
            defsBuilder.AppendLine(grad);
        }

        int titleYPos = 35;
        StringBuilder svg = new StringBuilder();
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"285\" viewBox=\"0 0 300 285\" fill=\"none\" role=\"img\" aria-labelledby=\"descId\" x=\"0\" y=\"0\">");
        svg.AppendLine("  <title id=\"descId\">Pie chart</title>");
        svg.AppendLine("  <rect data-testid=\"card-bg\" x=\"0.5\" y=\"0.5\" rx=\"4.5\" width=\"299\" height=\"99%\" fill=\"#242424\" stroke=\"#e4e2e2\" stroke-opacity=\"1\"/>");
        svg.AppendLine("  <defs>");
        svg.Append(defsBuilder.ToString());
        svg.AppendLine("  </defs>");
        svg.AppendLine("  <text fill=\"url(#titleGradient)\" font-size=\"24\" font-weight=\"bold\" font-family=\"Arial, sans-serif\">");
        svg.AppendLine($"    <tspan x=\"110\" y=\"{titleYPos}\">");
        svg.AppendLine("      T");
        svg.AppendLine("      <animate attributeName=\"dy\"");
        svg.AppendLine("               values=\"0;10;0;0\"");
        svg.AppendLine("               keyTimes=\"0;0.05;0.167;1\"");
        svg.AppendLine("               calcMode=\"spline\"");
        svg.AppendLine("               keySplines=\"0.4 0 1 1;0 0 0.2 1;0 0 1 1\"");
        svg.AppendLine("               dur=\"3.6s\"");
        svg.AppendLine("               repeatCount=\"indefinite\" />");
        svg.AppendLine("    </tspan>");
        svg.AppendLine($"    <tspan x=\"128\" y=\"{titleYPos}\">");
        svg.AppendLine("      o");
        svg.AppendLine("      <animate attributeName=\"dy\"");
        svg.AppendLine("               values=\"0;0;10;0;0\"");
        svg.AppendLine("               keyTimes=\"0;0.139;0.189;0.306;1\"");
        svg.AppendLine("               calcMode=\"spline\"");
        svg.AppendLine("               keySplines=\"0 0 1 1;0.4 0 1 1;0 0 0.2 1;0 0 1 1\"");
        svg.AppendLine("               dur=\"3.6s\"");
        svg.AppendLine("               repeatCount=\"indefinite\" />");
        svg.AppendLine("    </tspan>");
        svg.AppendLine($"    <tspan x=\"146\" y=\"{titleYPos}\">");
        svg.AppendLine("      p");
        svg.AppendLine("      <animate attributeName=\"dy\"");
        svg.AppendLine("               values=\"0;0;10;0;0\"");
        svg.AppendLine("               keyTimes=\"0;0.278;0.328;0.444;1\"");
        svg.AppendLine("               calcMode=\"spline\"");
        svg.AppendLine("               keySplines=\"0 0 1 1;0.4 0 1 1;0 0 0.2 1;0 0 1 1\"");
        svg.AppendLine("               dur=\"3.6s\"");
        svg.AppendLine("               repeatCount=\"indefinite\" />");
        svg.AppendLine("    </tspan>");
        svg.AppendLine($"    <tspan x=\"170\" y=\"{titleYPos}\">");
        svg.AppendLine($"      {items.Count-1}");
        svg.AppendLine("      <animate attributeName=\"dy\"");
        svg.AppendLine("               values=\"0;0;10;0;0\"");
        svg.AppendLine("               keyTimes=\"0;0.417;0.467;0.583;1\"");
        svg.AppendLine("               calcMode=\"spline\"");
        svg.AppendLine("               keySplines=\"0 0 1 1;0.4 0 1 1;0 0 0.2 1;0 0 1 1\"");
        svg.AppendLine("               dur=\"3.6s\"");
        svg.AppendLine("               repeatCount=\"indefinite\" />");
        svg.AppendLine("    </tspan>");
        svg.AppendLine("  </text>");

        svg.AppendLine("  <g transform=\"translate(150,142.5) scale(0.6)\">");
        svg.Append(svgPaths.ToString());

        for (int i = 0; i < wedgeAngles.Count; i++)
        {
            svg.AppendLine($"<path d=\"M0,0 L -4.36,-124.92 A125,125 0 0,1 4.36,-124.92 Z\" fill=\"url(#gradiant{i + 1})\" transform=\"rotate({wedgeAngles[i]:F2},0,0)\" />");
        }

        svg.AppendLine("    <circle cx=\"0\" cy=\"0\" r=\"70\" fill=\"#242424\"/>");
        svg.AppendLine("    <g transform=\"translate(0,10)\">");
        svg.AppendLine("      <rect x=\"-36\" y=\"-40\" width=\"20\" height=\"70\" fill=\"url(#histGradient)\" rx=\"5\">");
        svg.AppendLine("        <animate attributeName=\"height\" values=\"70;10;70\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("        <animate attributeName=\"y\" values=\"-40;20;-40\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("      </rect>");
        svg.AppendLine("      <rect x=\"-10\" y=\"-10\" width=\"20\" height=\"40\" fill=\"url(#histGradient)\" rx=\"5\">");
        svg.AppendLine("        <animate attributeName=\"height\" values=\"40;70;10;40\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("        <animate attributeName=\"y\" values=\"-10;-40;20;-10\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("      </rect>");
        svg.AppendLine("      <rect x=\"16\" y=\"20\" width=\"20\" height=\"10\" fill=\"url(#histGradient)\" rx=\"5\">");
        svg.AppendLine("        <animate attributeName=\"height\" values=\"10;70;10\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("        <animate attributeName=\"y\" values=\"20;-40;20\" dur=\"3s\" repeatCount=\"indefinite\"/>");
        svg.AppendLine("      </rect>");
        svg.AppendLine("    </g>");
        svg.AppendLine("  </g>");
        svg.AppendLine(svgIndicators.ToString());
        svg.AppendLine("  <g id=\"legend\">");
        svg.AppendLine("    <rect x=\"10\" y=\"263\" width=\"8\" height=\"8\" fill=\"#969696\"/>");
        svg.AppendLine("    <text x=\"23\" y=\"271\" fill=\"white\" font-size=\"12\" font-family=\"Arial, sans-serif\">Others</text>");
        svg.AppendLine("  </g>");

        svg.AppendLine("</svg>");

        return svg.ToString();
    }
}

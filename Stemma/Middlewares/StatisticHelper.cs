using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class StatisticHelper
{
    public static string GetStatisticSvg(Dictionary<string, float> dataSet)
    {
        if (dataSet.Count < 2 || dataSet.Count > 5)
            throw new ArgumentException("You must have between 1 to 4 items.");

        List<KeyValuePair<string, float>> items = dataSet.ToList();
        var others = items.FirstOrDefault(x => x.Key.Equals("Others", StringComparison.OrdinalIgnoreCase));
        if (!others.Equals(default(KeyValuePair<string, float>)))
        {
            items.RemoveAll(x => x.Key.Equals("Others", StringComparison.OrdinalIgnoreCase));
            items.Add(others);
        }

        string[] colors = new string[] { "#3498db", "#e67e22", "#9b59b6", "#f1c40f", "#969696" };

        float R = 125f;
        float currentAngle = -90f;

        StringBuilder svgPaths = new StringBuilder();
        StringBuilder svgIndicators = new StringBuilder();

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

            string selectedColor = "";
            if (items[i].Key.Equals("Others"))
            {
                selectedColor = "#969696";
            } else
            {
                selectedColor = colors[i];
            }

                string path = $"<path d=\"M0,0 L{startX:F2},{startY:F2} A{R},{R} 0 {largeArcFlag},1 {endX:F2},{endY:F2} Z\" fill=\"{selectedColor}\"/>";
            svgPaths.AppendLine(path);

            if (!items[i].Key.Equals("Others", StringComparison.OrdinalIgnoreCase) && i != items.Count - 1)
            {
                float midAngle = startAngle + sweepAngle / 2f;
                double midRad = midAngle * Math.PI / 180.0;
                float midX = R * (float)Math.Cos(midRad);
                float midY = R * (float)Math.Sin(midRad);

                float overallX = 150 + 0.6f * midX;
                float overallY = 142.5f + 0.6f * midY;

                bool isRight = overallX > 150;
                bool isBelow = overallY > 142.5;

                float diagOffset = 10f / (float)Math.Sqrt(2);

                float line1X, line1Y, line2EndX, line2EndY, textX, textY;
                if (isRight && !isBelow)
                {
                    line1X = overallX + diagOffset;
                    line1Y = overallY - diagOffset;

                    line2EndX = line1X + 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2;
                    textY = line1Y - 8;
                }
                else if (isRight && isBelow)
                {
                    line1X = overallX + diagOffset;
                    line1Y = overallY + diagOffset;

                    line2EndX = line1X + 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2;
                    textY = line1Y + 16;
                }
                else if (!isRight && isBelow)
                {
                    line1X = overallX - diagOffset;
                    line1Y = overallY + diagOffset;

                    line2EndX = line1X - 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2;
                    textY = line1Y + 16;
                }
                else
                {
                    line1X = overallX - diagOffset;
                    line1Y = overallY - diagOffset;

                    line2EndX = line1X - 40;
                    line2EndY = line1Y;
                    textX = (line1X + line2EndX) / 2;
                    textY = line1Y - 8;
                }


                string indicator = $"<line x1=\"{overallX:F2}\" y1=\"{overallY:F2}\" x2=\"{line1X:F2}\" y2=\"{line1Y:F2}\" stroke=\"white\" stroke-width=\"1\"/>\n" +
                                   $"<line x1=\"{line1X:F2}\" y1=\"{line1Y:F2}\" x2=\"{line2EndX:F2}\" y2=\"{line2EndY:F2}\" stroke=\"white\" stroke-width=\"1\"/>\n" +
                                   $"<text x=\"{textX:F2}\" y=\"{textY:F2}\" fill=\"white\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\">{items[i].Key}</text>";
                svgIndicators.AppendLine(indicator);
            }

            currentAngle += sweepAngle;
        }


        StringBuilder svg = new StringBuilder();
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"285\" viewBox=\"0 0 300 285\" fill=\"none\" role=\"img\" aria-labelledby=\"descId\" x=\"0\" y=\"0\">");
        svg.AppendLine("  <title id=\"descId\">Project Diagram – Diagram Flipped Horizontally with Animated Central Histogram</title>");
        svg.AppendLine("  <rect data-testid=\"card-bg\" x=\"0.5\" y=\"0.5\" rx=\"4.5\" width=\"299\" height=\"99%\" fill=\"#242424\" stroke=\"#e4e2e2\" stroke-opacity=\"1\"/>");
        svg.AppendLine("  <defs>");
        svg.AppendLine("    <clipPath id=\"holeClip\">");
        svg.AppendLine("      <circle cx=\"0\" cy=\"0\" r=\"80\"/>");
        svg.AppendLine("    </clipPath>");
        svg.AppendLine("    <linearGradient id=\"histGradient\" gradientUnits=\"objectBoundingBox\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">");
        svg.AppendLine("      <stop offset=\"0%\" stop-color=\"#3498db\"/>");
        svg.AppendLine("      <stop offset=\"100%\" stop-color=\"#9b59b6\"/>");
        svg.AppendLine("    </linearGradient>");
        svg.AppendLine("  </defs>");
        svg.AppendLine("  <g transform=\"translate(150,142.5) scale(0.6)\">");
        svg.AppendLine(svgPaths.ToString());
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
        svg.AppendLine("</svg>");

        return svg.ToString();
    }
}

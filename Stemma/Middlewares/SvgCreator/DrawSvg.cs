using Stemma.Models;
using System.Text;

namespace Stemma.Middlewares.SvgCreator
{
    public static class DrawSvg
    {
        public static string Draw(Dictionary<(int row, int col), Cell> cellDic, int[,] grid, int gap)
        {
            List<int> numOfGapInRowList = new List<int>();
            List<int> numOfGapInColList = new List<int>();
            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);
            double gridWidth = 0;
            double gridHeight = 0;

            for (int r = 0; r < numOfRow; r++)
            {
                numOfGapInRowList.Add(0);
                for (int c = 1; c < numOfCol; c++)
                {
                    if (grid[r, c] != 0)
                    {
                        numOfGapInRowList[r]++;
                    }
                }
            }

            for (int c = 0; c < numOfCol; c++)
            {
                numOfGapInColList.Add(0);
                for (int r = 1; r < numOfRow; r++)
                {
                    if (grid[r, c] != 0)
                    {
                        numOfGapInColList[c]++;
                    }
                }
            }

            //Console.WriteLine("Num of row gap:");
            //foreach (var item in numOfGapInRowList)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();
            //Console.WriteLine("Num of col gap:");
            //foreach (var item in numOfGapInColList)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();

            //gridWidth += numOfGapInRowList.Max() * (double)gap;

            double maxGridWidth = 0;
            double maxGridHeight = 0;

            for(int r = 0; r < numOfRow; r++)
            {
                maxGridWidth = 0;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r,c)];
                    maxGridWidth += cell.cellWidth;
                }
                if(gridWidth < maxGridWidth)
                {
                    gridWidth = maxGridWidth;
                }
            }
            gridWidth += numOfGapInRowList.Max() * (double)gap;

            for(int c = 0; c < numOfCol; c++)
            {
                maxGridHeight = 0;
                for (int r = 0;r < numOfRow; r++)
                {
                    Cell cell = cellDic[(r,c)];
                    maxGridHeight += cell.cellHeight;
                }
                if(gridHeight < maxGridHeight)
                {
                    gridHeight = maxGridHeight;
                }
            }
            gridHeight += numOfGapInColList.Max() * (double)gap;

            // Console.WriteLine($"Grid size: {gridWidth} x {gridHeight}");


            StringBuilder svgBuilder = new StringBuilder();
            svgBuilder.AppendLine(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{gridWidth}\" height=\"{gridHeight}\" viewBox=\"0 0 {gridWidth} {gridHeight}\">");
            
            for(int r = 0; r < numOfRow; r++)
            {
                for(int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r,c)];
                    string positionedBadgeSvg = ImageHelper.UpdatePosition(cell.svgString, cell.startPosX, cell.startPosY);
                    svgBuilder.AppendLine(positionedBadgeSvg);
                }
            }

            svgBuilder.AppendLine("</svg>");
            return svgBuilder.ToString();
        }
    }
}

using Stemma.Models;
using System.Text;

namespace Stemma.Middlewares.SvgCreator
{
    public static class DrawSvg
    {
        public static string DrawNonFit(Dictionary<(int row, int col), Cell> cellDic, int[,] grid, int gap)
        {
            List<int> numOfGapInRowList = new List<int>();
            List<int> numOfGapInColList = new List<int>();
            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);
            double gridWidth = 0;
            double gridHeight = 0;

            // ------------------DEBUG LOG-------------------

            //for (int r = 0; r < numOfRow; r++)
            //{
            //    for(int c = 0; c < numOfCol; c++)
            //    {
            //        Cell cell = cellDic[(r, c)];
            //        Console.Write($"({cell.imageWidth}x{cell.imageHeight}, {cell.cellWidth}x{cell.cellHeight}) ");
            //    }
            //    Console.WriteLine();
            //}

            double maxGridWidth = 0;
            double maxGridHeight = 0;
            int currentNumOfGapInRow = -1;
            int currentNumOfGapInCol = -1;
            int selectedRowForGap = 0;
            int selectedColForGap = 0;

            for (int r = 0; r < numOfRow; r++)
            {
                bool isIndent = true;
                maxGridWidth = 0;
                currentNumOfGapInRow = -1;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r, c)];
                    if (cell.isDefaultCell)
                    {
                        continue;
                    } else
                    {
                        isIndent = false;
                    }
                }

                if (!isIndent)
                {
                    for (int c = 0; c < numOfCol; c++)
                    {
                        Cell cell = cellDic[(r, c)];
                        //if (cell.isEmptyCell)
                        //{
                        //    continue;
                        //}
                        maxGridWidth += cell.cellWidth;

                        currentNumOfGapInRow++;
                    }

                    numOfGapInRowList.Add(currentNumOfGapInRow);

                    if (gridWidth < maxGridWidth)
                    {
                        gridWidth = maxGridWidth;
                        selectedRowForGap = r;
                    }
                }
            }

            for (int c = 0; c < numOfCol ; c++)
            {
                bool isIndent = true;
                maxGridHeight = 0;
                currentNumOfGapInCol = -1;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cell = cellDic[(r, c)];
                    if (cell.isDefaultCell)
                    {
                        continue;
                    }
                    else
                    {
                        isIndent = false;
                    }
                }
                if (!isIndent)
                {
                    for (int r = 0; r < numOfRow; r++)
                    {
                        Cell cell = cellDic[(r, c)];
                        if (cell.isEmptyCell)
                        {
                            continue;
                        }
                        maxGridHeight += cell.cellHeight;

                        currentNumOfGapInCol++;
                    }

                    numOfGapInColList.Add(currentNumOfGapInCol);

                    if (gridHeight < maxGridHeight)
                    {
                        gridHeight = maxGridHeight;
                        selectedColForGap = c;
                    }
                }
            }

            //foreach (int numOfGap in numOfGapInRowList)
            //{
            //    Console.WriteLine($"numOfGapInRowList: {numOfGap}");
            //}
            //Console.WriteLine();
            //foreach (int numOfGap in numOfGapInColList)
            //{
            //    Console.WriteLine($"numOfGapInColList: {numOfGap}");
            //}

            //Console.WriteLine($"Selected Row for Gap: {numOfGapInRowList[selectedRowForGap]}");
            //Console.WriteLine($"Selected Col for Gap: {numOfGapInColList[selectedColForGap]}");

            //gridWidth += numOfGapInRowList[selectedRowForGap] * (double)gap;
            //gridHeight += numOfGapInColList[selectedColForGap] * (double)gap;

            //Console.WriteLine($"Grid size: {gridWidth} x {gridHeight}");
            //Console.WriteLine();


            // yeah, that works



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

        public static string DrawRowFit(Dictionary<(int row, int col), Cell> cellDic, int[,] grid, int gap)
        {
            List<int> numOfGapInRowList = new List<int>();
            List<int> numOfGapInColList = new List<int>();
            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);
            double gridWidth = 0;
            double gridHeight = 0;

            // ------------------DEBUG LOG-------------------

            //for (int r = 0; r < numOfRow; r++)
            //{
            //    for (int c = 0; c < numOfCol; c++)
            //    {
            //        Cell cell = cellDic[(r, c)];
            //        Console.Write($"({cell.imageWidth}x{cell.imageHeight}, {cell.cellWidth}x{cell.cellHeight}) ");
            //    }
            //    Console.WriteLine();
            //}

            double maxGridWidth = 0;
            double maxGridHeight = 0;
            int currentNumOfGapInRow = -1;
            int currentNumOfGapInCol = -1;
            int selectedRowForGap = 0;
            int selectedColForGap = 0;

            for (int r = 0; r < numOfRow; r++)
            {
                bool isIndent = true;
                maxGridWidth = 0;
                currentNumOfGapInRow = -1;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r, c)];
                    if (cell.isDefaultCell)
                    {
                        continue;
                    }
                    else
                    {
                        isIndent = false;
                    }
                }

                if (!isIndent)
                {
                    for (int c = 0; c < numOfCol; c++)
                    {
                        Cell cell = cellDic[(r, c)];
                        if (cell.isEmptyCell)
                        {
                            continue;
                        }
                        maxGridWidth += cell.imageWidth;

                        currentNumOfGapInRow++;
                    }

                    numOfGapInRowList.Add(currentNumOfGapInRow);

                    if (gridWidth < maxGridWidth)
                    {
                        gridWidth = maxGridWidth;
                        selectedRowForGap = r;
                    }
                }
            }

            for (int c = 0; c < numOfCol; c++)
            {
                bool isIndent = true;
                maxGridHeight = 0;
                currentNumOfGapInCol = -1;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cell = cellDic[(r, c)];
                    if (cell.isDefaultCell)
                    {
                        continue;
                    }
                    else
                    {
                        isIndent = false;
                    }
                }
                if (!isIndent)
                {
                    for (int r = 0; r < numOfRow; r++)
                    {
                        Cell cell = cellDic[(r, c)];
                        if (cell.isEmptyCell)
                        {
                            continue;
                        }
                        maxGridHeight += cell.imageHeight;

                        currentNumOfGapInCol++;
                    }

                    numOfGapInColList.Add(currentNumOfGapInCol);

                    if (gridHeight < maxGridHeight)
                    {
                        gridHeight = maxGridHeight;
                        selectedColForGap = c;
                    }
                }
            }

            //foreach (int numOfGap in numOfGapInRowList)
            //{
            //    Console.WriteLine($"numOfGapInRowList: {numOfGap}");
            //}
            //Console.WriteLine();
            //foreach (int numOfGap in numOfGapInColList)
            //{
            //    Console.WriteLine($"numOfGapInColList: {numOfGap}");
            //}

            //Console.WriteLine($"Selected Row for Gap: {numOfGapInRowList[selectedRowForGap]}");
            //Console.WriteLine($"Selected Col for Gap: {numOfGapInColList[selectedColForGap]}");

            gridWidth += numOfGapInRowList[selectedRowForGap] * (double)gap;
            gridHeight += numOfGapInColList[selectedColForGap] * (double)gap;

            //Console.WriteLine($"Grid size: {gridWidth} x {gridHeight}");
            //Console.WriteLine();


            // yeah, that works



            StringBuilder svgBuilder = new StringBuilder();
            svgBuilder.AppendLine(
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{gridWidth}\" height=\"{gridHeight}\" viewBox=\"0 0 {gridWidth} {gridHeight}\">");

            for (int r = 0; r < numOfRow; r++)
            {
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r, c)];
                    string positionedBadgeSvg = ImageHelper.UpdatePosition(cell.svgString, cell.startPosX, cell.startPosY);
                    svgBuilder.AppendLine(positionedBadgeSvg);
                }
            }

            svgBuilder.AppendLine("</svg>");
            return svgBuilder.ToString();
        }
    }
}

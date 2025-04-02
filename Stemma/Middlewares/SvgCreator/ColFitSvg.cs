using Stemma.Models;

namespace Stemma.Middlewares.SvgCreator
{
    public static class ColFitSvg
    {
        public enum CellGroupType
        {
            Image,
            Default,
            Empty
        }

        public static CellGroupType GetCellGroupType(Cell cell)
        {
            if (!cell.isEmptyCell && !cell.isDefaultCell)
                return CellGroupType.Image;
            else if (cell.isDefaultCell)
                return CellGroupType.Default;
            else
                return CellGroupType.Empty;
        }

        private static bool ShouldMerge(CellGroupType current, CellGroupType next)
        {
            return current == next;
        }

        private static List<List<Cell>> CustomDefaultGroupingForColumn(int col, int numOfRow, Dictionary<(int, int), Cell> cellDic)
        {
            List<List<Cell>> segments = new List<List<Cell>>();
            if (numOfRow == 0)
                return segments;
            if (numOfRow == 1)
            {
                segments.Add(new List<Cell>() { cellDic[(0, col)] });
                return segments;
            }

            List<Cell> topGroup = new List<Cell>() { cellDic[(0, col)] };
            List<Cell> bottomGroup = new List<Cell>() { cellDic[(numOfRow - 1, col)] };

            segments.Add(topGroup);

            List<List<Cell>> midGroups = new List<List<Cell>>();
            int index = 1;
            while (index < numOfRow - 1)
            {
                if (index + 1 < numOfRow - 1)
                {
                    midGroups.Add(new List<Cell>() { cellDic[(index, col)], cellDic[(index + 1, col)] });
                    index += 2;
                }
                else
                {
                    midGroups.Add(new List<Cell>() { cellDic[(index, col)] });
                    index++;
                }
            }
            segments.AddRange(midGroups);
            segments.Add(bottomGroup);
            return segments;
        }

        public static Dictionary<(int row, int col), Cell> Create(
            int[,] grid,
            Dictionary<(int row, int col), Cell> cellDic,
            string alignType,
            int gap)
        {
            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);

            List<List<List<Cell>>> colList = new List<List<List<Cell>>>();
            for (int c = 0; c < numOfCol; c++)
            {
                bool isAllDefault = true;
                for (int r = 0; r < numOfRow; r++)
                {
                    if (!cellDic[(r, c)].isDefaultCell)
                    {
                        isAllDefault = false;
                        break;
                    }
                }
                if (isAllDefault)
                {
                    colList.Add(CustomDefaultGroupingForColumn(c, numOfRow, cellDic));
                    continue;
                }

                List<List<Cell>> segmentList = new List<List<Cell>>();
                List<Cell> currentGroup = new List<Cell>();
                CellGroupType? currentGroupType = null;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    CellGroupType cellType = GetCellGroupType(cellObj);

                    if (currentGroupType == null)
                        currentGroupType = cellType;

                    if (ShouldMerge(currentGroupType.Value, cellType))
                        currentGroup.Add(cellObj);
                    else
                    {
                        segmentList.Add(currentGroup);
                        currentGroup = new List<Cell>() { cellObj };
                        currentGroupType = cellType;
                    }
                }
                if (currentGroup.Count > 0)
                    segmentList.Add(currentGroup);
                colList.Add(segmentList);
            }

            int debugCounter = 0;
            foreach (var segments in colList)
            {
                Console.WriteLine($"Column #{debugCounter}:");
                Console.Write("\t");
                foreach (var group in segments)
                {
                    Console.Write("(");
                    foreach (Cell cell in group)
                    {
                        Console.Write($"{cell.imageWidth}x{cell.imageHeight} ");
                    }
                    Console.Write(")");
                }
                Console.WriteLine();
                debugCounter++;
            }

            int numOfSegmentInCol = 0;
            List<List<double>> colHeightList = new List<List<double>>();
            foreach (var segmentList in colList)
            {
                List<double> segHeights = new List<double>();
                int segCount = 0;
                foreach (var group in segmentList)
                {
                    double totalHeight = 0;
                    foreach (var cellObj in group)
                    {
                        totalHeight += cellObj.imageHeight;
                        totalHeight += gap;
                    }
                    if (totalHeight > 0)
                        totalHeight -= gap;
                    segCount++;
                    segHeights.Add(totalHeight);
                }
                colHeightList.Add(segHeights);
                if (numOfSegmentInCol < segCount)
                    numOfSegmentInCol = segCount;
            }

            List<double> allocatedHeights = new List<double>();
            for (int i = 0; i < numOfSegmentInCol; i++)
                allocatedHeights.Add(0);
            foreach (var segHeights in colHeightList)
            {
                int idx = 0;
                foreach (double h in segHeights)
                {
                    if (allocatedHeights[idx] < h)
                        allocatedHeights[idx] = h;
                    idx++;
                }
            }

            Console.WriteLine("\nAllocated Heights across segments:");
            foreach (double h in allocatedHeights)
                Console.WriteLine(h);
            Console.WriteLine();

            List<double> colWidthList = new List<double>();
            for (int c = 0; c < numOfCol; c++)
            {
                double maxWidth = 0;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (!cellObj.isEmptyCell && !cellObj.isDefaultCell)
                    {
                        if (cellObj.imageWidth > maxWidth)
                            maxWidth = cellObj.imageWidth;
                    }
                    else if (cellObj.isDefaultCell)
                    {
                        maxWidth = Math.Max(maxWidth, 20);
                    }
                }
                colWidthList.Add(maxWidth);
            }

            List<double> colBaseXList = new List<double>();
            double cumulativeX = 0;
            for (int c = 0; c < numOfCol; c++)
            {
                colBaseXList.Add(cumulativeX);
                cumulativeX += colWidthList[c] + gap;
            }

            for (int c = 0; c < numOfCol; c++)
            {
                double currentY = 0;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (cellObj.isDefaultCell)
                    {
                        cellObj.startPosY = currentY;
                        cellObj.startPosX = colBaseXList[c];
                        cellObj.cellHeight = 10;
                        cellObj.cellWidth = 20;
                        currentY += 10 + gap;
                    }
                    else if (cellObj.isEmptyCell)
                    {
                        cellObj.startPosY = currentY;
                        cellObj.startPosX = colBaseXList[c];
                        cellObj.cellHeight = 0;
                        cellObj.cellWidth = colWidthList[c];
                        currentY += 0 + gap;
                    }
                    cellDic[(r, c)] = cellObj;
                }
            }

            for (int c = 0; c < numOfCol; c++)
            {
                var segments = colList[c];
                double baseX = colBaseXList[c];
                double colWidth = colWidthList[c];

                double groupBaseY = 0;
                for (int r = 0; r < numOfRow; r++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (!cellObj.isDefaultCell && !cellObj.isEmptyCell)
                    {
                        groupBaseY = cellObj.startPosY;
                        break;
                    }
                    else
                    {
                        if (cellObj.isDefaultCell)
                            groupBaseY += 10 + gap;
                        else if (cellObj.isEmptyCell)
                            groupBaseY += 0 + gap;
                    }
                }

                double cumulativeSegmentY = groupBaseY;
                for (int segIndex = 0; segIndex < segments.Count; segIndex++)
                {
                    var imageGroup = segments[segIndex];
                    double allocatedHeight = allocatedHeights[segIndex];

                    double groupHeight = 0;
                    for (int i = 0; i < imageGroup.Count; i++)
                    {
                        groupHeight += imageGroup[i].imageHeight;
                        if (i > 0)
                            groupHeight += gap;
                    }

                    double innerOffsetY = 0;
                    double currentYWithinGroup = 0;
                    foreach (var cellObj in imageGroup)
                    {
                        double finalY = cumulativeSegmentY + innerOffsetY + currentYWithinGroup;
                        double finalX = baseX; 
                        cellObj.startPosX = finalX;
                        cellObj.startPosY = finalY;
                        cellObj.cellHeight = allocatedHeight;
                        cellObj.cellWidth = colWidth;

                        cellDic[(cellObj.rowIndex, cellObj.colIndex)] = cellObj;

                        currentYWithinGroup += cellObj.imageHeight;
                        if (cellObj != imageGroup.Last())
                            currentYWithinGroup += gap;
                    }
                    cumulativeSegmentY += allocatedHeight;
                }
            }

            Console.WriteLine("Final Cell Properties:");
            for (int r = 0; r < numOfRow; r++)
            {
                Console.WriteLine($"Row #{r}:");
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    Console.WriteLine($"\tColumn #{c}: ImageWidth: {cellObj.imageWidth}, ImageHeight: {cellObj.imageHeight}, " +
                                      $"CellWidth: {cellObj.cellWidth}, CellHeight: {cellObj.cellHeight}, " +
                                      $"StartPos X: {cellObj.startPosX}, StartPos Y: {cellObj.startPosY}");
                }
            }

            return cellDic;
        }
    }
}
using Microsoft.AspNetCore.Http.HttpResults;
using Stemma.Models;

namespace Stemma.Middlewares.SvgCreator
{
    public static class RowFitSvg
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

        // RULE34: merge if same type, or if current group is Image and next cell is Empty.
        public static bool ShouldMerge(CellGroupType current, CellGroupType next)
        {
            if (current == CellGroupType.Image && next == CellGroupType.Empty)
                return true;
            return current == next;
        }

        // custom segmentation for rows that are entirely default.
        // example) if there are 7 cells
        // we wanna group it like:
        // group1: col0; group2: col1-2; group3: col3-4; group4: col5; group5: col6.
        public static List<List<Cell>> Grouping(int row, int numOfCol, Dictionary<(int, int), Cell> cellDic)
        {
            List<List<Cell>> segments = new List<List<Cell>>();
            if (numOfCol == 0)
                return segments;
            if (numOfCol == 1)
            {
                segments.Add(new List<Cell>() { cellDic[(row, 0)] });
                return segments;
            }

            // ok. let's start.
            // we always treat the first and last default cells as separate groups.
            List<Cell> leftGroup = new List<Cell>() { cellDic[(row, 0)] };
            List<Cell> rightGroup = new List<Cell>() { cellDic[(row, numOfCol - 1)] };

            segments.Add(leftGroup);

            List<List<Cell>> midGroups = new List<List<Cell>>();
            int index = 1;
            int countMid = numOfCol - 2; // number of cells in the middle
            while (index < numOfCol - 1)
            {
                // if at least 2 cells remain, pair them;
                if (index + 1 < numOfCol - 1)
                {
                    midGroups.Add(new List<Cell>() { cellDic[(row, index)], cellDic[(row, index + 1)] });
                    index += 2;
                }
                else // otherwise, group the single leftover.
                {
                    midGroups.Add(new List<Cell>() { cellDic[(row, index)] });
                    index++;
                }
            }

            // adding mid groups...
            segments.AddRange(midGroups);
            // adding end group
            segments.Add(rightGroup);

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

            // OK, WE ARE KEEPING IN TRACK OF EMPTY, DEFAULT, AND ACTUAL IMAGE.
            // the thing is, default always exists when entire row is default.
            // we gotta group them up.

            List<List<List<Cell>>> rowList = new List<List<List<Cell>>>();
            for (int r = 0; r < numOfRow; r++)
            {
                // check if the entire row is default.
                bool isAllDefault = true;
                for (int c = 0; c < numOfCol; c++)
                {
                    if (!cellDic[(r, c)].isDefaultCell)
                    {
                        isAllDefault = false;
                        break;
                    }
                }
                if (isAllDefault)
                {
                    rowList.Add(Grouping(r, numOfCol, cellDic));
                    continue;
                }

                List<List<Cell>> segmentList = new List<List<Cell>>();
                List<Cell> currentGroup = new List<Cell>();
                CellGroupType? currentGroupType = null;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    CellGroupType cellType = GetCellGroupType(cellObj);

                    if (currentGroupType == null)
                        currentGroupType = cellType;

                    if (cellType == currentGroupType)
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
                rowList.Add(segmentList);
            }

            //int debug_counter = 0;
            //foreach (List<List<Cell>> cellList in rowList)
            //{
            //    Console.WriteLine($"Row #{debug_counter}: ");
            //    Console.Write("\t");
            //    foreach (List<Cell> cellGroup in cellList)
            //    {
            //        Console.Write("(");
            //        foreach(Cell cell in cellGroup)
            //        {
            //            Console.Write($"{cell.imageWidth}x{cell.imageHeight} ");
            //        }
            //        Console.Write(")");
            //    }
            //    Console.WriteLine();
            //    debug_counter++;
            //}


            int numOfSagementInRow = 0;
            List<double> sagementWidthList = new List<double>();
            List<List<double>> rowWidthList = new List<List<double>>();
            foreach (List<List<Cell>> sagementList in rowList)
            {
                int segCountInThisRow = 0;
                foreach (List<Cell> imageGroup in sagementList)
                {
                    double totalWidth = 0;
                    foreach (Cell cellObj in imageGroup)
                    {
                        totalWidth += cellObj.imageWidth;
                        totalWidth += gap;
                    }
                    if (totalWidth > 0)
                    {
                        totalWidth -= gap; // remove trailing gap
                    }
                    segCountInThisRow++;
                    sagementWidthList.Add(totalWidth);
                }
                rowWidthList.Add(sagementWidthList);
                sagementWidthList = new List<double>();
                if (numOfSagementInRow < segCountInThisRow)
                {
                    numOfSagementInRow = segCountInThisRow;
                }
            }

            //foreach(List<double> a in rowWidthList)
            //{
            //    foreach (double b in a)
            //    {
            //        Console.WriteLine(b);
            //    }
            //    Console.WriteLine();
            //}
            // ok. we have something here...

            // for each segment index, take the maximum width across rows.
            List<double> cellWidthList = new List<double>();
            for (int i = 0; i < numOfSagementInRow; i++)
            {
                cellWidthList.Add(0);
            }
            foreach (List<double> widthList in rowWidthList)
            {
                int rowIndex = 0;
                foreach (double width in widthList)
                {
                    if (cellWidthList[rowIndex] < width)
                    {
                        cellWidthList[rowIndex] = width;
                    }
                    rowIndex++;
                }
            }

            //Console.WriteLine("\nCell Width across row");
            //foreach(double cellWidth in cellWidthList)
            //{
            //    Console.WriteLine(cellWidth);
            //}
            //Console.WriteLine();
            // data is accurate here.

            for(int r = 0; r < numOfRow; r++)
            {
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cell = cellDic[(r,c)];
                }
            }

            // compute row heights (from image cells only, default cells are fixed)
            List<double> rowHeightList = new List<double>();
            for (int r = 0; r < numOfRow; r++)
            {
                double maxHeight = 0;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (!cellObj.isEmptyCell && !cellObj.isDefaultCell)
                    {
                        if (maxHeight < cellObj.imageHeight)
                        {
                            maxHeight = cellObj.imageHeight;
                        }
                    }
                    else if (cellObj.isDefaultCell)
                    {
                        maxHeight = Math.Max(maxHeight, cellObj.imageHeight); // defaul
                    }
                }
                rowHeightList.Add(maxHeight);
            }

            //Console.WriteLine("Row Heights");
            //foreach(double cellHeight in rowHeightList)
            //{
            //    Console.WriteLine(cellHeight);
            //}
            //Console.WriteLine();

            // compute cumulative Y positions for each row.
            List<double> rowBaseYList = new List<double>();
            double cumulativeY = 0;
            for (int r = 0; r < numOfRow; r++)
            {
                rowBaseYList.Add(cumulativeY);
                cumulativeY += rowHeightList[r] + gap;
            }

            // breaking align type into horizontal and vertical components.
            string horizontalAlign, verticalAlign;
            if (alignType.Equals("topleft") || alignType.Equals("left") || alignType.Equals("bottomleft"))
                horizontalAlign = "left";
            else if (alignType.Equals("topright") || alignType.Equals("right") || alignType.Equals("bottomright"))
                horizontalAlign = "right";
            else
                horizontalAlign = "center";

            if (alignType.Equals("topleft") || alignType.Equals("top") || alignType.Equals("topright"))
                verticalAlign = "top";
            else if (alignType.Equals("bottomleft") || alignType.Equals("bottom") || alignType.Equals("bottomright"))
                verticalAlign = "bottom";
            else
                verticalAlign = "center";

            // we'll have to assign positions for default and empty cells in grid order.
            // NOTE: for each row, go through each column and update X positions.
            for (int r = 0; r < numOfRow; r++)
            {
                double currentX = 0;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (cellObj.isDefaultCell)
                    {
                        cellObj.startPosX = currentX;
                        cellObj.startPosY = rowBaseYList[r];
                        cellObj.cellWidth = 20;
                        cellObj.cellHeight = 10;
                        currentX += 20 + gap;
                    }
                    else if (cellObj.isEmptyCell)
                    {
                        cellObj.startPosX = currentX;
                        cellObj.startPosY = rowBaseYList[r];
                        cellObj.cellWidth = 0;
                        cellObj.cellHeight = rowHeightList[r];
                        currentX += 0 + gap;
                    }
                    cellDic[(r, c)] = cellObj;
                }
            }

            // placing fucking junks
            for (int r = 0; r < numOfRow; r++)
            {
                List<List<Cell>> segments = rowList[r];
                double baseY = rowBaseYList[r];
                double rowHeight = rowHeightList[r];

                // find where the first image cell should appear by scanning the row.
                double groupBaseX = 0;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r, c)];
                    if (!cellObj.isDefaultCell && !cellObj.isEmptyCell)
                    {
                        groupBaseX = cellObj.startPosX;
                        break;
                    }
                    else
                    {
                        if (cellObj.isDefaultCell)
                            groupBaseX += 20 + gap;
                        else if (cellObj.isEmptyCell)
                            groupBaseX += 0 + gap;
                    }
                }

                double cumulativeSegmentX = groupBaseX;

                for (int segIndex = 0; segIndex < segments.Count; segIndex++)
                {
                    List<Cell> imageGroup = segments[segIndex];
                    double allocatedWidth = cellWidthList[segIndex];

                    // we need to think about the actual width of this group.
                    double groupWidth = 0;
                    for (int j = 0; j < imageGroup.Count; j++)
                    {
                        groupWidth += imageGroup[j].imageWidth;
                        if (j > 0)
                        {
                            groupWidth += gap;
                        }
                    }

                    // setting offsets
                    double innerOffsetX = 0;
                    if (horizontalAlign.Equals("center"))
                        innerOffsetX = (allocatedWidth - groupWidth) / 2;
                    else if (horizontalAlign.Equals("right"))
                        innerOffsetX = allocatedWidth - groupWidth;

                    double currentXWithinGroup = 0;
                    foreach (Cell cellObj in imageGroup)
                    {
                        double finalX = cumulativeSegmentX + innerOffsetX + currentXWithinGroup;
                        double innerOffsetY = 0;
                        if (verticalAlign.Equals("center"))
                            innerOffsetY = (rowHeight - cellObj.imageHeight) / 2;
                        else if (verticalAlign.Equals("bottom"))
                            innerOffsetY = rowHeight - cellObj.imageHeight;

                        double finalY = baseY + innerOffsetY;

                        cellObj.startPosX = finalX;
                        cellObj.startPosY = finalY;
                        cellObj.cellWidth = allocatedWidth;
                        cellObj.cellHeight = rowHeight;

                        cellDic[(cellObj.rowIndex, cellObj.colIndex)] = cellObj;

                        currentXWithinGroup += cellObj.imageWidth;
                        if (cellObj != imageGroup.Last())
                        {
                            currentXWithinGroup += gap;
                        }
                    }
                    cumulativeSegmentX += allocatedWidth;
                }
            }

            //Console.WriteLine($"Horizontal: {horizontalAlign}, Vertical: {verticalAlign}");
            //Console.WriteLine("Cell Properties:");
            //for (int r = 0; r < numOfRow; r++)
            //{
            //    Console.WriteLine($"Row #{r}:");
            //    for (int c = 0; c < numOfCol; c++)
            //    {
            //        Cell cellObj = cellDic[(r, c)];
            //        Console.WriteLine($"\tColumn #{c}:");
            //        Console.WriteLine($"\t\tImageWidth: {cellObj.imageWidth}");
            //        Console.WriteLine($"\t\tImageHeight: {cellObj.imageHeight}");
            //        Console.WriteLine($"\t\tCellWidth: {cellObj.cellWidth}");
            //        Console.WriteLine($"\t\tCellHeight: {cellObj.cellHeight}");
            //        Console.WriteLine($"\t\tStartPos X: {cellObj.startPosX}");
            //        Console.WriteLine($"\t\tStartPos Y: {cellObj.startPosY}");
            //    }
            //}

            return cellDic;
        }
    }
}

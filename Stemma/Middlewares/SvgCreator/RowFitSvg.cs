using Stemma.Models;

namespace Stemma.Middlewares.SvgCreator
{
    // IDEA? 
    // what if we just read json that contains the cell data?
    // that'd be easier instead of dealing with these whole shit.
    // yeah, let's do that
    public static class RowFitSvg
    {
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
            // TODO: REMOVE MAGIC NUMBERS

            var rowList = new List<List<List<Cell>>>();
            for (int r = 0; r < numOfRow; r++)
            {
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
                    rowList.Add(Grouping(r, numOfCol, cellDic));
                else
                {
                    var segList = new List<List<Cell>>();
                    var currentGroup = new List<Cell>();
                    CellGroupType? currType = null;
                    for (int c = 0; c < numOfCol; c++)
                    {
                        var cell = cellDic[(r, c)];
                        var type = GetCellGroupType(cell);
                        if (currType == null)
                            currType = type;
                        if (currType.HasValue && ShouldMerge(currType.Value, type))
                            currentGroup.Add(cell);
                        else
                        {
                            segList.Add(currentGroup);
                            currentGroup = new List<Cell>() { cell };
                            currType = type;
                        }
                    }
                    if (currentGroup.Count > 0)
                        segList.Add(currentGroup);
                    rowList.Add(segList);
                }
            }

            // ------------------DEBUG LOG-------------------
            //for (int r = 0; r < rowList.Count; r++)
            //{
            //    Console.Write($"Row #{r}: ");
            //    foreach (var group in rowList[r])
            //    {
            //        Console.Write("(");
            //        foreach (var cell in group)
            //            Console.Write($"{cell.imageWidth}x{cell.imageHeight} ");
            //        Console.Write(") ");
            //    }
            //    Console.WriteLine();
            //}
            // yeah, that works

            // calc cellWidthList for non-default rows
            int maxNumGroups = 0;
            var nonDefWidths = new List<List<double>>();
            for (int r = 0; r < rowList.Count; r++)
            {
                bool defRow = true;
                foreach (var group in rowList[r])
                {
                    if (group.Count == 0 || !group[0].isDefaultCell)
                    {
                        defRow = false;
                        break;
                    }
                }
                if (defRow) continue;
                var grpWidths = new List<double>();
                foreach (var group in rowList[r])
                {
                    double total = 0; int count = 0;
                    foreach (var cell in group)
                    {
                        if (cell.imageWidth > 0)
                        {
                            if (count > 0) total += gap;
                            total += cell.imageWidth;
                            count++;
                        }
                    }
                    grpWidths.Add(total);
                }
                maxNumGroups = Math.Max(maxNumGroups, grpWidths.Count);
                nonDefWidths.Add(grpWidths);
            }
            var cellWidthList = new List<double>();
            for (int i = 0; i < maxNumGroups; i++)
            {
                double maxW = 0;
                foreach (var widths in nonDefWidths)
                {
                    if (i < widths.Count && widths[i] > maxW)
                        maxW = widths[i];
                }
                cellWidthList.Add(maxW);
            }

            // ------------------DEBUG LOG-------------------
            //Console.WriteLine("Computed cellWidthList:");
            //foreach(var w in cellWidthList)
            //    Console.WriteLine(w);
            // yup, that works

            // calc cellHeightList for non-default rows
            int maxNumGroupsForHeight = 0;
            var nonDefHeights = new List<List<double>>();
            for (int r = 0; r < rowList.Count; r++)
            {
                bool defRow = true;
                foreach (var group in rowList[r])
                {
                    if (group.Count == 0 || !group[0].isDefaultCell)
                    {
                        defRow = false;
                        break;
                    }
                }
                if (defRow) continue;
                var grpHeights = new List<double>();
                foreach (var group in rowList[r])
                {
                    double maxH = 0;
                    foreach (var cell in group)
                        if (cell.imageHeight > maxH)
                            maxH = cell.imageHeight;
                    grpHeights.Add(maxH);
                }
                maxNumGroupsForHeight = Math.Max(maxNumGroupsForHeight, grpHeights.Count);
                nonDefHeights.Add(grpHeights);
            }
            var cellHeightList = new List<double>();
            for (int i = 0; i < maxNumGroupsForHeight; i++)
            {
                double maxH = 0;
                foreach (var heights in nonDefHeights)
                {
                    if (i < heights.Count && heights[i] > maxH)
                        maxH = heights[i];
                }
                cellHeightList.Add(maxH);
            }

            // ------------------DEBUG LOG-------------------
            //Console.WriteLine("Computed cellHeightList:");
            //foreach (var h in cellHeightList)
            //    Console.WriteLine(h);
            // yup, that works

            // calc rowBaseY for EVERY row.
            var rowBaseYMapping = new Dictionary<int, double>();
            double cumY = 0;
            for (int r = 0; r < numOfRow; r++)
            {
                double rowH = 0;
                bool defRow = true;
                foreach (var group in rowList[r])
                {
                    if (group.Count == 0 || !group[0].isDefaultCell)
                    {
                        defRow = false; break;
                    }
                }
                if (defRow)
                    rowH = cellDic[(r, 0)].imageHeight;
                else
                {
                    foreach (var group in rowList[r])
                        foreach (var cell in group)
                            if (cell.imageHeight > rowH)
                                rowH = cell.imageHeight;
                }
                rowBaseYMapping[r] = cumY;
                cumY += rowH + gap;
            }

            // ------------------DEBUG LOG-------------------
            //Console.WriteLine("Computed rowBaseYMapping:");
            //foreach (var kvp in rowBaseYMapping)
            //    Console.WriteLine($"Row {kvp.Key}: Base Y = {kvp.Value}");
            // yup, that works

            // setting up aligment types
            string horAlign = (alignType == "topleft" || alignType == "left" || alignType == "bottomleft") ? "left" :
                              (alignType == "topright" || alignType == "right" || alignType == "bottomright") ? "right" :
                              "center";
            string verAlign = (alignType == "topleft" || alignType == "top" || alignType == "topright") ? "top" :
                              (alignType == "bottomleft" || alignType == "bottom" || alignType == "bottomright") ? "bottom" :
                              "center";

            // ------------------DEBUG LOG-------------------
            //Console.WriteLine($"Horizontal Alignment: {horAlign}");
            //Console.WriteLine($"Vertical Alignment: {verAlign}");
            // yup, that works

            // EVERYTHING FINE TILL HERE

            // placing fucking junks
            // we'll have to assign positions for default and empty cells in grid order.
            // NOTE: for each row, go through each column and update X positions.
            for (int r = 0; r < numOfRow; r++)
            {
                // see if row is all default
                bool defRow = true;
                foreach (var group in rowList[r])
                {
                    if (group.Count == 0 || !group[0].isDefaultCell)
                    {
                        defRow = false;
                        break;
                    }
                }
                // if yay?
                if (defRow)
                {
                    double cumX = 0;
                    for (int c = 0; c < numOfCol; c++)
                    {
                        var cell = cellDic[(r, c)];
                        cell.startPosX = cumX;
                        cell.startPosY = rowBaseYMapping[r];
                        cell.cellWidth = cell.imageWidth;
                        cell.cellHeight = cell.imageHeight;
                        cumX += cell.imageWidth + gap;
                        cellDic[(r, c)] = cell;
                    }
                }
                // if nay?
                else
                {
                    double baseY = rowBaseYMapping[r];
                    double rowH = 0;
                    foreach (var group in rowList[r])
                        foreach (var cell in group)
                            if (cell.imageHeight > rowH)
                                rowH = cell.imageHeight;
                    double cumX = 0;
                    for (int i = 0; i < rowList[r].Count; i++)
                    {
                        // we need to think about the actual width of this group.
                        var group = rowList[r][i];
                        double allocatedWidth = cellWidthList[i];
                        double groupWidth = 0; int count = 0;
                        foreach (var cell in group)
                        {
                            if (cell.imageWidth > 0)
                            {
                                if (count > 0) groupWidth += gap;
                                groupWidth += cell.imageWidth;
                                count++;
                            }
                        }

                        // setting offsets
                        double offsetX = horAlign == "center" ? (allocatedWidth - groupWidth) / 2 :
                                          horAlign == "right" ? allocatedWidth - groupWidth : 0;
                        double curXWithin = 0;
                        foreach (var cell in group)
                        {
                            double finalX = cumX + offsetX + curXWithin;
                            double offY = verAlign == "center" ? (rowH - cell.imageHeight) / 2 :
                                          verAlign == "bottom" ? rowH - cell.imageHeight : 0;
                            double finalY = baseY + offY;
                            cell.startPosX = finalX;
                            cell.startPosY = finalY;
                            cell.cellWidth = allocatedWidth;
                            cell.cellHeight = rowH;
                            cellDic[(cell.rowIndex, cell.colIndex)] = cell;
                            curXWithin += cell.imageWidth;
                            if (cell != group.Last()) curXWithin += gap;
                        }
                        cumX += allocatedWidth + gap;
                    }
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

            // throw new Exception("TESTING");
            return cellDic;
        }

        // nah... leave it here.
        // this should work in any matter...? <= yeah, it works.
        public enum CellGroupType { Image, Default, Empty }

        public static CellGroupType GetCellGroupType(Cell cell)
        {
            if (!cell.isEmptyCell && !cell.isDefaultCell) return CellGroupType.Image;
            else if (cell.isDefaultCell) return CellGroupType.Default;
            else return CellGroupType.Empty;
        }

        // RULE34: merge if same type, or if current group is Image and next cell is EMPTY.
        public static bool ShouldMerge(CellGroupType current, CellGroupType next)
        {
            return (current == CellGroupType.Image && next == CellGroupType.Empty) || current == next;
        }

        // custom segmentation for rows that are entirely default.
        public static List<List<Cell>> Grouping(int row, int numOfCol, Dictionary<(int, int), Cell> cellDic)
        {
            var segments = new List<List<Cell>>();
            for (int c = 0; c < numOfCol; c++)
                segments.Add(new List<Cell>() { cellDic[(row, c)] });
            return segments;
        }
    }
}

using Stemma.Models;

namespace Stemma.Middlewares.SvgCreator
{
    public static class NoneFitSvg
    {
        public static Dictionary<(int row, int col), Cell> Create(int[,] grid, Dictionary<(int row, int col), Cell> cellDic, string alignType, int gap)
        {
            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);
            

            List<double> cellHeightList = new List<double>();
            List<double> cellWidthList = new List<double>();

            for (int r = 0; r < numOfRow; r++)
            {
                double cellMaxHeight = 0;
                for (int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r,c)];
                    //Console.WriteLine($"Dimension: {cellObj.imageWidth} x {cellObj.imageHeight}");
                    if(cellMaxHeight < cellObj.imageHeight)
                    {
                        cellMaxHeight = cellObj.imageHeight;
                    }
                }

                cellHeightList.Add(cellMaxHeight);

                //Console.WriteLine() ;
            }

            for (int c = 0; c < numOfCol; c++)
            {
                double cellMaxWidth = 0;
                for(int r = 0; r < numOfRow; r++)
                {
                    Cell cellObj = cellDic[(r,c)];

                    if(cellMaxWidth < cellObj.imageWidth)
                    {
                        cellMaxWidth = cellObj.imageWidth;
                    }
                }

                cellWidthList.Add(cellMaxWidth);
            }

            //Console.WriteLine("Cell height list:");
            //foreach (double cellHeight in cellHeightList)
            //{
            //    Console.WriteLine(cellHeight + " ");
            //}
            //Console.WriteLine();

            //Console.WriteLine("Cell width list:");
            //foreach (double cellWidth in cellWidthList)
            //{
            //    Console.WriteLine(cellWidth + " ");
            //}
            //Console.WriteLine();

            // Console.WriteLine("yeet");

            for(int r = 0; r < numOfRow; r++)
            {
                for(int c = 0; c < numOfCol; c++)
                {
                    Cell cellObj = cellDic[(r,c)];
                    cellObj.cellHeight = cellHeightList[r];
                    cellObj.cellWidth = cellWidthList[c];

                    double tmpStartPosX = 0;
                    double tmpStartPosY = 0;

                    // determine start pos
                    double baseOffsetX = 0;
                    if (c > 0)
                    {
                        for (int cellColIndex = c; cellColIndex > 0; cellColIndex--)
                        {
                            baseOffsetX += cellWidthList[cellColIndex - 1] + gap;
                        }
                    }

                    double baseOffsetY = 0;
                    if (r > 0)
                    {
                        for (int cellRowIndex = r; cellRowIndex > 0; cellRowIndex--)
                        {
                            baseOffsetY += cellHeightList[cellRowIndex - 1] + gap;
                        }
                    }

                    double innerOffsetX = 0;
                    double innerOffsetY = 0;

                    switch (alignType)
                    {
                        case "topleft":
                            innerOffsetX = 0;
                            innerOffsetY = 0;
                            break;
                        case "top":
                            innerOffsetX = (cellObj.cellWidth - cellObj.imageWidth) / 2;
                            innerOffsetY = 0;
                            break;
                        case "topright":
                            innerOffsetX = cellObj.cellWidth - cellObj.imageWidth;
                            innerOffsetY = 0;
                            break;
                        case "left":
                            innerOffsetX = 0;
                            innerOffsetY = (cellObj.cellHeight - cellObj.imageHeight) / 2;
                            break;
                        case "center":
                            innerOffsetX = (cellObj.cellWidth - cellObj.imageWidth) / 2;
                            innerOffsetY = (cellObj.cellHeight - cellObj.imageHeight) / 2;
                            break;
                        case "right":
                            innerOffsetX = cellObj.cellWidth - cellObj.imageWidth;
                            innerOffsetY = (cellObj.cellHeight - cellObj.imageHeight) / 2;
                            break;
                        case "bottomleft":
                            innerOffsetX = 0;
                            innerOffsetY = cellObj.cellHeight - cellObj.imageHeight;
                            break;
                        case "bottom":
                            innerOffsetX = (cellObj.cellWidth - cellObj.imageWidth) / 2;
                            innerOffsetY = cellObj.cellHeight - cellObj.imageHeight;
                            break;
                        case "bottomright":
                            innerOffsetX = cellObj.cellWidth - cellObj.imageWidth;
                            innerOffsetY = cellObj.cellHeight - cellObj.imageHeight;
                            break;
                        default:
                            innerOffsetX = 0;
                            innerOffsetY = 0;
                            break;
                    }

                    tmpStartPosX = baseOffsetX + innerOffsetX;
                    tmpStartPosY = baseOffsetY + innerOffsetY;



                    cellObj.startPosX = tmpStartPosX;
                    cellObj.startPosY = tmpStartPosY;

                    cellDic[(r,c)] = cellObj;
                }
            }

            //for (int c = 0; c < numOfCol; c++)
            //{
            //    for (int r = 0; r < numOfRow; r++)
            //    {
            //        cellDic[(r, c)].cellWidth = cellWidthList[c];
            //        cellDic[(r, c)].startPosX = r * cellWidthList[c];
            //    }
            //}

            //Console.WriteLine("Cell Properties:");
            //for(int r = 0; r < numOfRow; r++)
            //{
            //    Console.WriteLine($"Row #{r}:");
            //    for(int c=0; c < numOfCol; c++)
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

            //foreach (Cell cell in cells)
            //{
            //    //int assignedRow = cell.rowIndex;
            //    //int assignedCol = cell.colIndex;


            //}
            return cellDic;
        }
    }
}

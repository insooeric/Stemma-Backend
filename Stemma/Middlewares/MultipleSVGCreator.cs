using Stemma.Middlewares.SvgCreator;
using Stemma.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Stemma.Middlewares
{
    public static class MultipleSVGCreator
    {

        public static string Create(List<ImageObject> imageObjects, int[,] grid, string fitContent, string alignType, int _gap, int emptyCellWidth, int emptyCellHeight)
        {
            //Console.WriteLine("---------------------");
            //Console.WriteLine("Creating multiple SVGs");
            //Console.WriteLine($"Num of images: {imageObjects.Count}");
            //Console.WriteLine($"Grid: {grid.GetLength(0)}x{grid.GetLength(1)}");
            //Console.WriteLine($"Fit content: {fitContent}");
            //Console.WriteLine($"Align: {alignType}");
            //Console.WriteLine("---------------------");
            // !!!GRID IS ALWAYS VALID!!!
            // !!!GRID IS ALWAYS VALID!!!
            // !!!GRID IS ALWAYS VALID!!!
            // !!!GRID IS ALWAYS VALID!!!

            // const double targetHeight = 40;
            int gap = _gap;


            int numOfRow = grid.GetLength(0);
            int numOfCol = grid.GetLength(1);


            List<(string svg, double width, double height)> badgeSvgs = new List<(string svg, double width, double height)>();
            foreach (var image in imageObjects)
            {
                string badgeSvg = new string(image.imageInSvg);
                int newHeight = 40;
                int newWidth = newHeight; // fallback
                //if (!fitContent)
                //{
                    newWidth = ImageHelper.GetWidthByHeight(newHeight, badgeSvg);
                // }
                badgeSvg = ImageHelper.ResizeSVG(badgeSvg, newWidth, newHeight);
                badgeSvgs.Add((badgeSvg, newWidth, newHeight));
            }




            for (int r = 0; r < numOfRow; r++)
            {
                bool isEmptyRow = true;
                for (int c = 0; c < numOfCol; c++)
                {
                    if (grid[r, c] != 0)
                    {
                        isEmptyRow = false;
                        break;            
                    }
                }

                if (isEmptyRow)
                {
                    for (int c = 0; c < numOfCol; c++)
                    {
                        grid[r, c] = -1;
                    }
                }
            }


            for (int c = 0; c < numOfCol; c++)
            {
                bool isEmptyCol = true;  
                for (int r = 0; r < numOfRow; r++)
                {
                    if (grid[r, c] != 0 && grid[r, c] != -1)
                    {
                        isEmptyCol = false; 
                        break;        
                    }
                }

                if (isEmptyCol)
                {
                    for (int r = 0; r < numOfRow; r++)
                    {
                        grid[r, c] = -1; 
                    }
                }
            }




            //Console.WriteLine("Grid:");
            //for (int i = 0; i < numOfRow; i++)
            //{
            //    for (int j = 0; j < numOfCol; j++)
            //    {
            //        if (grid[i, j] != 0)
            //            Console.Write(grid[i, j] + "\t");
            //        else
            //            Console.Write("0\t");
            //    }
            //    Console.WriteLine();
            //}

            //Console.WriteLine("------end------");

            double gridWidth = 0;
            double gridHeight = 0;

            // List<Cell> cells = new List<Cell>();
            Dictionary<(int row, int col), Cell> cellDictionary = new Dictionary<(int, int), Cell>();
            int idval = 1;

            for (int r = 0; r < numOfRow; r++)
            {
                for (int c = 0; c < numOfCol; c++)
                {
                    if (grid[r, c] > 0)
                    {
                        var tmpSvg = badgeSvgs[grid[r, c] - 1];
                        cellDictionary[(r, c)] = new Cell(idval, tmpSvg.svg, tmpSvg.width, tmpSvg.height, 0, 0, false, false, 0, 0, r, c);
                    }
                    else if(grid[r, c] == 0)
                    {
                        // cells.Add(new Cell("", 0, 0, 0, 0, true, false, 0, 0, r, c));
                        cellDictionary[(r, c)] = new Cell(0, "", 0, 0, 0, 0, true, false, 0, 0, r, c);
                    }
                    else if( grid[r, c] == -1)
                    {
                        //cells.Add(new Cell("", emptyCellWidth, emptyCellHeight, 0, 0, false, true, 0, 0, r, c));
                        cellDictionary[(r, c)] = new Cell(-1, "", emptyCellWidth, emptyCellHeight, 0, 0, false, true, 0, 0, r, c);
                    }
                    else
                    {
                        throw new Exception("this shouldn't happen :(");
                    }
                    idval++;
                }
            }

            // WE NEED TO REDEFINE POSITION X, Y, GAPS

            //string innerSvg = "";

            Dictionary < (int row, int col), Cell > resultCellDic = new Dictionary<(int row, int col), Cell>();
            switch (fitContent)
            {
                case "none":
                    resultCellDic = NoneFitSvg.Create(grid, cellDictionary, alignType, gap);
                    break;
                case "row":
                    resultCellDic = RowFitSvg.Create(grid, cellDictionary, alignType, gap);
                    break;
                case "col":
                    // TODO: yeah... work with this
                    // resultCellDic = ColFitSvg.Create(grid, cellDictionary, alignType, gap);
                    break;

                // ahh... nah... let's think about this later...
                //case "all":
                //    break;
                default:
                    throw new Exception("this error shouldn't happen...");
            }


            return DrawSvg.Draw(resultCellDic, grid, gap);
        }
    }
}

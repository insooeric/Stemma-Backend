namespace Stemma.Middlewares
{
    public static class GridHelper
    {
        public static int[,] GetGrid(int numOfItems, string? rowStr, string? colStr)
        {
            List<int> templateRow = new List<int>();
            List<int> templateCol = new List<int>();

            int numOfItemsLeft = numOfItems;



            // if both null?
            if (rowStr == null && colStr == null)
            {
                templateRow.Add(numOfItems);
                for (int i = 0; i < numOfItems; i++)
                {
                    templateCol.Add(1);
                }
            }

            // if we have row?
            if (rowStr != null && colStr == null)
            {
                List<int> parsedRow = ParseTemplate(rowStr);

                if (parsedRow.Count == 1)
                {
                    int desiredRows = parsedRow.First();
                    if (desiredRows < 1 || desiredRows > numOfItems)
                    {
                        throw new ArgumentException("Invalid number of rows.");
                    }

                    int itemsLeft = numOfItems;
                    int remainingRows = desiredRows;
                    while (remainingRows > 1)
                    {
                        int currentRow = (int)Math.Ceiling((double)(itemsLeft - 1) / (remainingRows - 1));
                        templateRow.Add(currentRow);
                        itemsLeft -= currentRow;
                        remainingRows--;
                    }

                    templateRow.Add(itemsLeft);
                    int maxColumns = templateRow.Max();
                    for (int colIndex = 1; colIndex <= maxColumns; colIndex++)
                    {
                        int count = templateRow.Count(rowCount => rowCount >= colIndex);
                        templateCol.Add(count);
                    }
                } else if (parsedRow.Count > 1)
                {
                    foreach (var item in parsedRow)
                    {
                        templateRow.Add(item);
                    }
                    if(templateRow.Sum() != numOfItems)
                    {
                        throw new ArgumentException("Invalid number of rows.");
                    }

                    int maxColumns = templateRow.Max();
                    for (int colIndex = 1; colIndex <= maxColumns; colIndex++)
                    {
                        int count = templateRow.Count(rowCount => rowCount >= colIndex);
                        templateCol.Add(count);
                    }
                }
            }

            // if we have col?
            if (rowStr == null && colStr != null)
            {
                List<int> parsedCol = ParseTemplate(colStr);

                if (parsedCol.Count == 1)
                {
                    int desiredCols = parsedCol.First();
                    if (desiredCols < 1 || desiredCols > numOfItems)
                    {
                        throw new ArgumentException("Invalid number of columns.");
                    }

                    int itemsLeft = numOfItems;
                    int remainingCols = desiredCols;
                    while (remainingCols > 1)
                    {
                        int currentCol = (int)Math.Ceiling((double)itemsLeft / remainingCols);
                        templateCol.Add(currentCol);
                        itemsLeft -= currentCol;
                        remainingCols--;
                    }
                    templateCol.Add(itemsLeft);

                    int maxRows = templateCol.Max();
                    for (int rowIndex = 1; rowIndex <= maxRows; rowIndex++)
                    {
                        int count = templateCol.Count(c => c >= rowIndex);
                        templateRow.Add(count);
                    }
                }
                else if (parsedCol.Count > 1)
                {
                    foreach (var item in parsedCol)
                    {
                        templateCol.Add(item);
                    }
                    if (templateCol.Sum() != numOfItems)
                    {
                        throw new ArgumentException("Invalid number of columns.");
                    }

                    int maxRows = templateCol.Max();
                    for (int rowIndex = 1; rowIndex <= maxRows; rowIndex++)
                    {
                        int count = templateCol.Count(c => c >= rowIndex);
                        templateRow.Add(count);
                    }
                }
            }

            // if we have both row and col?
            // TODO: IMPLEMENT THIS
            if (rowStr != null && colStr != null)
            {
                List<int> parsedRow = ParseTemplate(rowStr);
                List<int> parsedCol = ParseTemplate(colStr);


                if (parsedRow.Count == 1 && parsedCol.Count == 1)
                {
                    //Console.WriteLine("flag1");
                    //var validator = Validator.CheckValidGrid(numOfItems, parsedRow.First(), parsedCol.First());
                    //if (!validator.isValid)
                    //{
                    //    throw new ArgumentException($"Invalid dimension. {validator.msg}");
                    //}

                    int numOfRow = parsedRow.First();
                    int numOfCol = parsedCol.First();

                    //Console.WriteLine($"row={numOfRow}");
                    //Console.WriteLine($"col={numOfCol}");

                    // templateRow = new List<int> { numOfCol };


                    //for (int j = 0; j < numOfCol; j++)
                    //{
                    //    templateCol.Add(1);
                    //}

                    // suppose we have 7 items
                    // and we have 2 rows and 4 columns
                    // since template row is 0,0,0,0
                    // and template col is 0,0 initially

                    int[,] occupiedGrid = new int[numOfRow, numOfCol];
                    //Console.WriteLine("Occupied grid:");
                    //Console.WriteLine($"row: {occupiedGrid.GetLength(0)}");
                    //Console.WriteLine($"col: {occupiedGrid.GetLength(1)}");

                    for (int r = 0; r < occupiedGrid.GetLength(0) ; r++)
                    {
                        occupiedGrid[r, 0] = 1;
                        numOfItemsLeft--;
                    }

                    for (int c = 1; c < occupiedGrid.GetLength(1); c++)
                    {
                        occupiedGrid[0, c] = 1;
                        numOfItemsLeft--;
                    }



                    for(int r = 1; r < occupiedGrid.GetLength(0); r++)
                    {
                        for (int c = 1; c < occupiedGrid.GetLength(1); c++)
                        {
                            if (numOfItemsLeft < 1)
                            {
                                break;
                            }
                            occupiedGrid[r, c] = 1;
                            numOfItemsLeft--;
                        }
                        if (numOfItemsLeft < 1)
                        {
                            break;
                        }
                    }

                    //Console.WriteLine("Final occupied grid:");
                    //for (int r = 0; r < occupiedGrid.GetLength(0); r++)
                    //{
                    //    for (int c = 0; c < occupiedGrid.GetLength(1); c++)
                    //    {
                    //        Console.Write(occupiedGrid[r, c] + " ");
                    //    }
                    //    Console.WriteLine();
                    //}
                    //Console.WriteLine();

                    for (int r = 0; r < occupiedGrid.GetLength(0); r++)
                    {
                        int count = 0;
                        for (int c = 0; c < occupiedGrid.GetLength(1); c++)
                        {
                            if (occupiedGrid[r, c] == 1)
                            {
                                count++;
                            }
                        }
                        templateRow.Add(count);
                    }

                    for (int c = 0; c < occupiedGrid.GetLength(1); c++)
                    {
                        int count = 0;
                        for (int r = 0; r < occupiedGrid.GetLength(0); r++)
                        {
                            if (occupiedGrid[r, c] == 1)
                            {
                                count++;
                            }
                        }
                        templateCol.Add(count);
                    }

                    // DAT FKING WORKS!
                }
                if (parsedRow.Count > 1 && parsedCol.Count == 1)
                {
                    //Console.WriteLine("flag2");
                    int numOfRow = parsedRow.Count;
                    int numOfCol = parsedCol.First();

                    //Console.WriteLine($"row={numOfRow}");
                    //Console.WriteLine($"col={numOfCol}");


                    foreach (var item in parsedRow)
                    {
                        templateRow.Add(item);
                    }

                    if (templateRow.Sum() != numOfItems)
                    {
                        throw new ArgumentException("Invalid number of rows.");
                    }

                    if (numOfCol != parsedRow.Max())
                    {
                        throw new ArgumentException($"Invalid number of columns. There should be exact number of {parsedRow.Max()} columns.");
                    }

                    for (int colIndex = 1; colIndex <= numOfCol; colIndex++)
                    {
                        int count = templateRow.Count(rowCount => rowCount >= colIndex);
                        templateCol.Add(count);
                    }
                }
                if (parsedRow.Count == 1 && parsedCol.Count > 1)
                {
                    //Console.WriteLine("flag3");

                    int numOfRow = parsedRow.First();
                    int numOfCol = parsedCol.Count();

                    //Console.WriteLine($"row={numOfRow}");
                    //Console.WriteLine($"col={numOfCol}");





                    foreach (var item in parsedCol)
                    {
                        templateCol.Add(item);
                    }

                    if (templateCol.Sum() != numOfItems)
                    {
                        throw new ArgumentException("Invalid number of columns.");
                    }

                    if (numOfRow != parsedCol.Max())
                    {
                        throw new ArgumentException($"Invalid number of rows. There should be exact number of {parsedCol.Max()} rows.");
                    }

                    int maxRows = templateCol.Max();
                    for (int rowIndex = 1; rowIndex <= maxRows; rowIndex++)
                    {
                        int count = templateCol.Count(c => c >= rowIndex);
                        templateRow.Add(count);
                    }
                }
                if (parsedRow.Count > 1 && parsedCol.Count > 1)
                {
                    //Console.WriteLine("flag4");
                    int numOfItemInRowTmpl = parsedRow.Sum();
                    int numOfItemInColTmpl = parsedCol.Sum();

                    foreach (var item in parsedRow)
                    {
                        templateRow.Add(item);
                    }
                    foreach (var item in parsedCol)
                    {
                        templateCol.Add(item);
                    }

                    string errormsg = "Invalid number of items. ";
                    bool hasError = false;
                    if (numOfItemInRowTmpl != numOfItems)
                    {
                        errormsg += $"Sum of items in row template must be exactly {numOfItems} items. ";
                        hasError = true;
                    }
                    if (numOfItemInColTmpl != numOfItems)
                    {
                        errormsg += $"Sum of items in column template must be exactly {numOfItems} items.";
                        hasError = true;
                    }

                    if (hasError)
                    {
                        throw new ArgumentException(errormsg);
                    }
                }

                //if (templateRow.Sum() != numOfItems || templateCol.Sum() != numOfItems)
                //{
                //    throw new ArgumentException("Invalid dimension.");
                //}
            }

            //Console.Write("TemplateRow: ");
            //foreach (var item in templateRow)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();

            //Console.Write("TemplateCol: ");
            //foreach (var item in templateCol)
            //{
            //    Console.Write(item + " ");
            //}
            //Console.WriteLine();

            var validationResult = Validator.CheckValidTemplate(numOfItems, templateRow, templateCol);
            if (!validationResult.isValid)
            {
                throw new ArgumentException(validationResult.msg);
            }

            bool[,]? solution = PuzzleSolver(templateRow, templateCol);
            if (solution == null)
            {
                throw new Exception("No solution found.");
            }

            int row = templateRow.Count;
            int col = templateCol.Count;
            int itemCount = 1;

            int[,] grid = new int[row, col];

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (solution[i, j])
                    {
                        grid[i, j] = itemCount;
                        itemCount++;
                    } else
                    {
                        grid[i, j] = 0;
                    }
                }
                // Console.WriteLine();
            }

            return grid;
        }

        private static List<int> ParseTemplate(string template)
        {
            return template.Split(',')
                           .Select(s => int.Parse(s.Trim()))
                           .ToList();
        }

        private static bool[,]? PuzzleSolver(List<int> templateRow, List<int> templateCol)
        {
            //Console.WriteLine("PuzzleSolver");
            int rowCount = templateRow.Count;
            int colCount = templateCol.Count;
            bool[,]? solution = new bool[rowCount, colCount];

            List<List<bool[]>> possibleRows = new List<List<bool[]>>();
            for (int r = 0; r < rowCount; r++)
            {
                int onesNeeded = templateRow[r];
                List<bool[]> patterns = new List<bool[]>();
                GenerateRowPatterns(new bool[colCount], 0, onesNeeded, patterns);
                possibleRows.Add(patterns);
            }

            bool solved = Solve(0, new bool[rowCount, colCount], possibleRows, templateCol, rowCount, colCount, out solution);
            if(!solved)
            {
                throw new Exception("No solution found.");
            }
            return solution;
        }
        private static void GenerateRowPatterns(bool[] row, int index, int onesLeft, List<bool[]> patterns)
        {
            int colCount = row.Length;
            if (index == colCount)
            {
                if (onesLeft == 0)
                {
                    bool[] pattern = new bool[colCount];
                    Array.Copy(row, pattern, colCount);
                    patterns.Add(pattern);
                }
                return;
            }

            if (onesLeft > 0)
            {
                row[index] = true;
                GenerateRowPatterns(row, index + 1, onesLeft - 1, patterns);
            }

            row[index] = false;
            GenerateRowPatterns(row, index + 1, onesLeft, patterns);
        }

        private static bool Solve(int rowIndex, bool[,] current, List<List<bool[]>> possibleRows, List<int> templateCol, int rowCount, int colCount, out bool[,]? solution)
        {
            if (rowIndex == rowCount)
            {
                for (int c = 0; c < colCount; c++)
                {
                    int colSum = 0;
                    for (int r = 0; r < rowCount; r++)
                    {
                        if (current[r, c])
                            colSum++;
                    }
                    if (colSum != templateCol[c])
                    {
                        solution = null;
                        return false;
                    }
                }
                solution = (bool[,])current.Clone();
                return true;
            }

            foreach (bool[] rowPattern in possibleRows[rowIndex])
            {
                for (int c = 0; c < colCount; c++)
                {
                    current[rowIndex, c] = rowPattern[c];
                }

                bool valid = true;
                for (int c = 0; c < colCount; c++)
                {
                    int colSum = 0;
                    for (int r = 0; r <= rowIndex; r++)
                    {
                        if (current[r, c])
                            colSum++;
                    }

                    int remainingRows = rowCount - rowIndex - 1;
                    if (colSum > templateCol[c] || colSum + remainingRows < templateCol[c])
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid)
                    continue;


                if (Solve(rowIndex + 1, current, possibleRows, templateCol, rowCount, colCount, out solution))
                    return true;
            }
            solution = new bool[0,0];
            return false;
        }
    }
}

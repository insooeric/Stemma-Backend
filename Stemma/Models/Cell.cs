namespace Stemma.Models
{
    public class Cell
    {
        public int Id { get; set; }
        public double imageWidth;
        public double imageHeight;
        public bool isEmptyCell;
        public bool isDefaultCell;
        public double startPosX;
        public double startPosY;
        public string svgString;
        public double cellWidth;
        public double cellHeight;
        public int rowIndex;
        public int colIndex;

        public Cell(int cellId, string svgString, double imageWidth, double imageHeight, double cellWidth, double cellHeight, bool isEmptyCell, bool isDefaultCell, double startPosX, double startPosY, int rowIndex, int columnIndex)
        {
            this.Id = cellId;
            this.svgString = svgString;
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.isEmptyCell = isEmptyCell;
            this.isDefaultCell = isDefaultCell;
            this.startPosX = startPosX;
            this.startPosY = startPosY;
            this.rowIndex = rowIndex;
            this.colIndex = columnIndex;
        }
    }
}

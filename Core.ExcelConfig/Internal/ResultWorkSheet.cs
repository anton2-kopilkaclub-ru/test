using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Excel.Internal
{
    /// <summary>
    /// некоторые внутренние настройки
    /// часть из них - динамические, т.е. меняются от вызова к вызову,
    /// поэтому лучше хранить их в контейнере. 
    /// Вдруг кто-то когда-то сделает пару вызовов одновременно
    /// </summary>
    internal class ResultWorkSheet
    {

        /// <summary>
        /// сколько колонок выделено на инфу под одно оборудование
        /// </summary>
        public int columnsPerTask = 5;
        /// <summary>
        /// лист, с которым работаем
        /// </summary>
        public ExcelWorksheet worksheet;
        /// <summary>
        /// текущяя строка
        /// </summary>
        public int currRow = 1;

        /// <summary>
        /// с какой колонки начинать следующую строку
        /// </summary>
        public int startingColumn = 1;
        /// <summary>
        /// текущий столбец
        /// </summary>
        public int currColumn = 1;

        /// <summary>
        /// при записи в одну ячеку можно сразу объединять несколько ячеек
        /// Значение не может быть меньше 1
        /// </summary>
        public int singleColumnSize = 2;

        /// <summary>
        /// перейти на новую строку
        /// </summary>
        public void NewLine()
        {
            currRow++;
            currColumn = startingColumn;
        }
        /// <summary>
        /// перейти к логической колонке, с учетом отступа и размера ячеек
        /// </summary>
        public void AdvanceToColumn(int logicalColumn)
        {
            currColumn = startingColumn + singleColumnSize * logicalColumn;
        }

        /// <summary>
        /// перейти к логической колонке, с учетом отступа и размера ячеек
        /// </summary>
        public void AdvanceToRow(int row)
        {
            currRow = row;
            AdvanceToColumn(0);
        }

        /// <summary>
        /// возвращает набор ячеек, в которые помещено значение
        /// </summary>
        public ExcelRange Write(object toWrite, WriteOptions options, WriteStyle style = WriteStyle.Orinary)
        {
            ExcelRange cells;
            switch (options)
            {
                case WriteOptions.FullWidth:
                    int width = worksheet.Dimension.End.Column;
                    cells = worksheet.Cells[currRow, 1, currRow, width];
                    cells.Merge = true;
                    cells.Value = toWrite;
                    ApplyStyle(cells.Style, style);
                    NewLine();
                    return cells;
                case WriteOptions.SingleCell:
                    //какие-то глобальные проблемы с заданием типа ячейки
                    //теоретически, при получении числового значения,
                    //фреймворк должен понимать, что в ячейке - число,
                    //но он выставляет как строку - видимо, смотрит на формальный тип object
                    cells = worksheet.Cells[currRow, currColumn,
                                            currRow, currColumn + singleColumnSize - 1];
                    cells.Merge = true;
                    decimal tmp;
                    if (decimal.TryParse(toWrite.ToString(), out tmp))
                    {
                        cells.Value = tmp;
                    }
                    else
                    {
                        cells.Value = toWrite.ToString();
                    }
                    currColumn += singleColumnSize;
                    ApplyStyle(cells.Style, style);
                    return cells;
                case WriteOptions.Formula:
                    cells = worksheet.Cells[currRow, currColumn,
                                            currRow, currColumn + singleColumnSize - 1];
                    cells.Merge = true;
                    cells.Formula = toWrite.ToString();
                    currColumn += singleColumnSize;
                    ApplyStyle(cells.Style, style);
                    return cells;
                default: throw new ArgumentException("Unknown write mode");
            }
        }
        /// <summary>
        /// стиль может быть как у одной ячейке, так и у Range и еще бог знает у кого.
        /// Поэтому работаем прямо со стилем
        /// </summary>
        public void ApplyStyle(ExcelStyle excelStyle, WriteStyle style)
        {
            switch (style)
            {
                case WriteStyle.Orinary:
                    excelStyle.WrapText = true;
                    excelStyle.QuotePrefix = false;
                    break;
                case WriteStyle.Header: //заголовок - просто жирный обычный текст
                    ApplyStyle(excelStyle, WriteStyle.Orinary);
                    excelStyle.Font.Bold = true;
                    break;
                case WriteStyle.BigHeader: //заголовок - увеличинный отцентрованный заголовок
                    ApplyStyle(excelStyle, WriteStyle.Header);
                    excelStyle.Font.Size = 1.2f * excelStyle.Font.Size;
                    excelStyle.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    break;
                default: //ну не знаем мы, что тут делать. Ничего страшного
                    break;
            }
        }
    }

    /// <summary>
    /// варианты записи строки в ячейки excel внутренними функциями
    /// </summary>
    internal enum WriteOptions
    {
        /// <summary>
        /// объединить все ячейки в текущей строке по желаемой ширине
        /// </summary>
        FullWidth,
        /// <summary>
        /// записывать только в текущую ячейку, строка не переводится,
        /// текущая ячейка сдвигается
        /// </summary>
        SingleCell,
        /// <summary>
        /// формула; в одну ячейку
        /// </summary>
        Formula
    }

    /// <summary>
    /// варианты применяемого стиля
    /// </summary>
    internal enum WriteStyle
    {
        /// <summary>
        /// базовый стиль
        /// </summary>
        Orinary,
        /// <summary>
        /// простой заголовок
        /// </summary>
        Header,
        /// <summary>
        /// отцентрованный заголовок увеличенным шрифтом
        /// </summary>
        BigHeader
    }

}

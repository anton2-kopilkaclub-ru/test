using Core.SimulationResult;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using OfficeOpenXml.Style;
using Core.Excel.Internal;

namespace Core.Excel
{
    /// <summary>
    /// расширения для результатов моделирования
    /// </summary>
    public static class SimulateResultExtension
    {
        
        /// <summary>
        /// записать результаты моделирования в файл Excel
        /// </summary>
        public static void ToExcel(this ISimulationResult simulationResult, string filename) {
            if (File.Exists(filename)){ //всегда создаем новый файл результатов
                File.Delete(filename);
            }

            FileInfo excelFile = new FileInfo(filename);
            //в пути могут быть пока еще не созданные каталоги, создадим их
            Directory.CreateDirectory(excelFile.DirectoryName);

            using (ExcelPackage package = new ExcelPackage(excelFile)) {

                ResultWorkSheet currWorkSheet = new ResultWorkSheet();
                //"План загрузки оборудования" - слишком много для названия листа :=)
                currWorkSheet.worksheet = package.Workbook.Worksheets.Add("План загрузки");
                
                currWorkSheet.currRow = 1;

                if (simulationResult.HasErrors){ //были ошибки, просто вернем их список
                    WriteErrors(currWorkSheet, simulationResult.Errors);
                    package.Save();
                    return;
                }
                //запишем данные по оборудованию
                currWorkSheet.startingColumn = 2; //для красивости немного отступим
                currWorkSheet.singleColumnSize = 1;
                WriteEquipmentData(currWorkSheet, simulationResult);
                currWorkSheet.startingColumn = 1;
                currWorkSheet.NewLine();

                //запишем  метрики
                currWorkSheet.singleColumnSize = 4; //для метрик нужно побольше места
                WriteMetrics(currWorkSheet, simulationResult.Metrics);
                currWorkSheet.NewLine();
                
                //запишем все примечания
                foreach (string str in simulationResult.Footnotes) {
                    currWorkSheet.Write( str, WriteOptions.FullWidth);
                }
                package.Save();
            }
        }

        private static void WriteEquipmentData(ResultWorkSheet currWorkSheet, ISimulationResult simulationResult)
        {
            //запишем данные по загрузке оборудования
            int startRow = currWorkSheet.currRow;
            int stopRow = 1;
            int currEquipmentNum = 0;
            foreach (IEquipment eq in simulationResult.TaskByEquipment.Keys) {
                int startColumn = currEquipmentNum * currWorkSheet.columnsPerTask;
                //добавим заголовок 
                int headerHeight = AddTaskHeader(eq, startRow, startColumn, currWorkSheet);
                
                int taskNum = 0;                
                //заполним данные по всем задачам для этого оборудования
                foreach(var task in simulationResult.TaskByEquipment[eq]){
                    currWorkSheet.AdvanceToRow(startRow + taskNum + headerHeight);
                    currWorkSheet.AdvanceToColumn(startColumn);
                    if (currWorkSheet.currRow > stopRow){
                        stopRow = currWorkSheet.currRow;
                    }
                    currWorkSheet.Write(string.Format("{0}",task.id), WriteOptions.SingleCell);
                    currWorkSheet.Write(string.Format("{0}", task.description), WriteOptions.SingleCell);
                    currWorkSheet.Write(string.Format("{0}", task.startedAt), WriteOptions.SingleCell);
                    currWorkSheet.Write(string.Format("{0}", task.duration), WriteOptions.SingleCell);
                    taskNum++;
                }
                currWorkSheet.NewLine();
                currWorkSheet.AdvanceToColumn(startColumn + 3);
                currWorkSheet.Write(string.Format("sum(indirect(address({0},{2})&\":\"&address({1},{2})))", 
                        startRow + headerHeight, currWorkSheet.currRow - 1, 
                        currWorkSheet.currColumn), WriteOptions.Formula);
                currEquipmentNum++;
            }

            //перейдем на последнюю строку, которую заполнили по оборудованию
            stopRow++; //учитываем формулу "всего"
            currWorkSheet.AdvanceToRow(stopRow  );

            //для таблички оборудования сделаем красивую автоширину колонки
            //AutoFitColumns() не работает нигде и никак
            //currWorkSheet.worksheet.Cells.AutoFitColumns();

            //уменьшим шрифт таблицы
            currWorkSheet.worksheet.Cells[startRow, 1, 
                currWorkSheet.currRow,currWorkSheet.worksheet.Dimension.End.Column]
                .Style.Font.Size = 8.0f;
            //вернем красивость заголовку
            currWorkSheet.ApplyStyle(currWorkSheet.worksheet.Row(startRow).Style,WriteStyle.BigHeader);
            currWorkSheet.NewLine();
        }

        /// <summary>
        /// добавляет заголовок для оборудования
        /// Возвращает высоту заголовка в строках
        /// </summary>
        private static int AddTaskHeader(IEquipment eq, int startRow, int startColumn, ResultWorkSheet currWorkSheet)
        {
            IDictionary<int, double> preferredWith = new Dictionary<int, double>(){
                { 0, 6}, //"id задачи"
                { 1, 10}, //"задача"
                { 2, 8}, //"время начала"
                { 3, 8} //"продолжительность"
            };
            //перейдем в нужное место 
            currWorkSheet.AdvanceToRow(startRow);
            currWorkSheet.AdvanceToColumn(startColumn);
            int headerFirstColumn = currWorkSheet.currColumn;

            currWorkSheet.Write(string.Format("{0} id={1}",eq.name,eq.id), WriteOptions.SingleCell, WriteStyle.BigHeader);
            currWorkSheet.AdvanceToColumn(startColumn + preferredWith.Keys.Count - 1);
            int headerLastColumn = currWorkSheet.currColumn;
            currWorkSheet.worksheet.Cells[startRow, headerFirstColumn, startRow, headerLastColumn].Merge = true;

            //cоздаем подзаголовки для задачи
            currWorkSheet.NewLine();
            currWorkSheet.AdvanceToColumn(startColumn);
            
            //выпишем заголовок для задачи
            //сразу задаем нужную ширину; автоматическое проставление по содержимому не получилось
            for (int j = 0; j < preferredWith.Count; j++){
                currWorkSheet.worksheet.Column(currWorkSheet.currColumn + j).Width = preferredWith[j];
            }
            //для подзаголовка нужна высота побольше
            currWorkSheet.worksheet.Row(currWorkSheet.currRow).Height = 32;

            //cобственно подзаголовки
            currWorkSheet.Write("id задачи", WriteOptions.SingleCell, WriteStyle.Header);
            currWorkSheet.Write("задача", WriteOptions.SingleCell, WriteStyle.Header);
            currWorkSheet.Write("время начала", WriteOptions.SingleCell, WriteStyle.Header);
            currWorkSheet.Write("продолжительность", WriteOptions.SingleCell, WriteStyle.Header);

            return 2;
        }

        private static void WriteMetrics(ResultWorkSheet currWorkSheet, IDictionary<string, string> metrics)
        {
            //запишем все метрики
            foreach (var pair in metrics){
                //метрика - это строка названия + строка значения
                //запишем в соседних ячейках
                currWorkSheet.Write( pair.Key, WriteOptions.SingleCell);
                currWorkSheet.Write( pair.Value, WriteOptions.SingleCell);
                currWorkSheet.NewLine();
            }

        }

        private static void WriteErrors(ResultWorkSheet currWorkSheet, ICollection<ValidationResult> errors)
        {
            currWorkSheet.Write("Возникли ошибки при моделировании", WriteOptions.FullWidth);
            foreach (var err in errors){
                currWorkSheet.Write(err.ErrorMessage, WriteOptions.FullWidth);
            }

        }




    }
}

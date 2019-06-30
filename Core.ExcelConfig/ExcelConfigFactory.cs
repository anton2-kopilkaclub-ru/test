using Core.Config;
using Core.Config.DTO;
using Core.Context;
using Core.Context.SimpleBruteAlgorithm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace Core.Excel
{
    /// <summary>
    /// создает конфигурацию на основе файлов excel
    /// </summary>
    public class ExcelConfigFactory
    {
        private ILogger logger;
        /// <summary>
        /// получить пакет "сырых" входных данных из входных файлов 
        /// </summary>
        public  ISimulationContext GetSimulationContext(ConfigParams inputData,
            IConfigurationSection algorithmConfig = null)
        {
            ISimulationContext context = CreateContext(algorithmConfig);
            context.LoggerFactory = this.loggerFactory;
            context.InputParams = CreateBundle(inputData);
            //если в данном классе алгоритма есть настройки, загружаем их
            HandleOptions(context, algorithmConfig);
            
            return context;
        }

        private void HandleOptions(ISimulationContext context, IConfigurationSection algorithmConfig)
        {
            IContextWithOptions withOptions = context as IContextWithOptions;
            if (withOptions == null)
            { //нет настроек в алгоритме
                logger.LogDebug("Algorithm doesn't support options ");
                return;
            }
            logger.LogDebug("Algorithm may require options to run.");
            IConfigurationSection optionsSection = (algorithmConfig != null)
                ? algorithmConfig.GetSection("options")
                : null;
            if (optionsSection == null)
            {
                logger.LogWarning("no options specified in config");
                return;
            }
            //настройки все есть, передаем в алгоритм
            object options = optionsSection.Get(withOptions.OptionClass);
            withOptions.SetOptions(options);
            logger.LogDebug("options passed to algorithm");
        }


        private ISimulationContext CreateContext(IConfigurationSection algorithmConfig){
            //если никакие настройки не указаны - создаем первый попавшийся алгоритм без параметров
            if (algorithmConfig == null){
                return new SimpleBruteAlgorithm();
            }
            //получим название класса, который нужно создать
            string className = algorithmConfig.GetValue<string>("class");
            if (string.IsNullOrWhiteSpace(className)){
                throw new ArgumentException("algorithm class name not specified in 'class' config section");
            }
            Assembly whereToSearch = System.Reflection.Assembly.GetAssembly(typeof(SimpleBruteAlgorithm));
            //поищем его в текущей сборке
           object createdType = whereToSearch.CreateInstance(className);
            if (createdType == null) {
                throw new ArgumentException(string.Format("class '{0}' not found in assembly {1}. Loading from a different assembly is currently not supported",className,whereToSearch.FullName));
            }
            //и убедимся, что он реализовывает нужный интерфейс
            ISimulationContext context = createdType as ISimulationContext;
            if (context == null ){
                throw new ArgumentException(string.Format("class '{0}' does not implement necessary interface {1}", className,typeof(ISimulationContext).Name));
            }
            return context;
        }

        private ConfigBundle CreateBundle(ConfigParams configData ) {
            logger.LogInformation("creating config bundle");
            ConfigBundle bundle = new ConfigBundle(loggerFactory);
            bundle.InputParams = configData;
            //грузим тайминги
            bundle.Timings = LoadFromExcel<TimingDTO>(configData.Timing, "reading times config file",3,
                (elems =>  new TimingDTO(){
                        equipmentId = elems[0].Text,
                        materialId = elems[1].Text,
                        timing = elems[2].Text
                    }
                ));
            //грузим номенклатуру
            bundle.Nomenclature = LoadFromExcel<NomenclatureDTO>(configData.Nomenclature, "reading Nomenclature config file", 2,
                (elems =>  new NomenclatureDTO() {
                        id = elems[0].Text,
                        name = elems[1].Text
                    }
                ));

            //грузим партии
            bundle.Parties = LoadFromExcel<partiesDTO>(configData.Parties, "reading Parties config file", 2,
                (elems => new partiesDTO(){
                        id = elems[0].Text,
                        nomenclatureId = elems[1].Text
                    }
                ));

            //грузим оборудование
            bundle.Equipment = LoadFromExcel<MachineToolsDTO>(configData.Equipment, "reading Machine_tools config file", 2,
                (elems => new MachineToolsDTO() {
                    id = elems[0].Text,
                    name = elems[1].Text
                }
                ));

            logger.LogInformation("config bundle created");
            return bundle;
        }
        private ILoggerFactory loggerFactory;
        public ExcelConfigFactory(ILoggerFactory loggerFactory) {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger(this.GetType());
        }

        /// <summary>
        /// загрузить из таблички Excel в DTO с записью в лог
        /// </summary>
        protected List<T> LoadFromExcel<T>(FileParam excelFileParam, string logScopeName, int cellCount, Func<ExcelRangeBase[],T> createElem) {
            logger.LogTrace(logScopeName);
            using (logger.BeginScope(logScopeName))
            {
                if ((excelFileParam == null) || (string.IsNullOrWhiteSpace(excelFileParam.fileName))){
                    throw new ArgumentException("No filename given");
                }

                logger.LogTrace(string.Format("opening '{0}'", excelFileParam.fileName));
                FileInfo excelFile = new FileInfo(excelFileParam.fileName);
                if (!excelFile.Exists){
                    throw new ArgumentException("File not found");
                }

                using (ExcelPackage package = new ExcelPackage(excelFile))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                    int rowCount = worksheet.Dimension.Rows;
                    if (rowCount < 2) {
                        throw new ApplicationException("Too little rows. Need at least two: header + data");
                    }
                    List<T> result = new List<T>();
                    //skipping first line - that is header
                    //numeration base is 1, not 0
                    for (int i = 2; i <= rowCount; i++) {
                        ExcelRange row = worksheet.Cells[string.Format("{0}:{0}", i)];
                        // see if all cells of this row are empty
                        bool allEmpty = row.All(c => string.IsNullOrWhiteSpace(c.Text));
                        if (allEmpty) continue; // skip this row

                        //check that we have correct number of non-empty first cells
                        bool hasEnoughNotEmpty = (row.Take(cellCount).Count(cell => !string.IsNullOrWhiteSpace(cell.Text)) == cellCount);
                        if (!hasEnoughNotEmpty) {
                            throw new ArgumentException(
                                string.Format("file '{0}' row {1}, need at least {2} non-empty starting columns",
                                              excelFileParam.fileName,i , cellCount)); 
                        }
                        T newElem = createElem(row.Take(cellCount).ToArray());
                        result.Add(newElem);
                        logger.LogTrace(string.Format("row {0}. Created element: {1}",i,newElem.ToString()));                                 
                    }
                    logger.LogDebug(string.Format("{0} elements found",result.Count));
                    return result;
                }

            }
        }  
    }
}

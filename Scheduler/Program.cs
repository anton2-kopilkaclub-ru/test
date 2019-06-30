using Core;
using Core.SimulationResult;
using Core.Config;
using Core.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;

namespace ConsoleApp
{
    class Program
    {
        private static ILogger logger;
        private static ILoggerFactory logFactory;
        private static IConfigurationRoot configRoot;
        private static ExcelConfigFactory contextFactory;
        static void Main(string[] args)
        {
            try
            {
                string msg = "This console application is intended to run as a batch file " 
                  +" using input files specified in app.json configuration file "   
                  +" and writes output to specified file. Details can be found in log and "
                  +" only few messages pass through to the console.\r\n";
                Console.WriteLine(msg);

                Configure();
                logger.LogInformation("starting application");

                //получим контекст моделирования, передавая настройки для входных параметров
                //и для самого алгоритма
                ISimulationContext algorithm = contextFactory.GetSimulationContext(
                    configRoot.GetSection("input").Get<ConfigParams>(),
                    configRoot.GetSection("algorithm")
                    );
                ISimulationResult result = algorithm.Simulate();

                OutputResult(result);
            }
            catch (Exception ex) {
                string msg = string.Format("Exception {0} with message : {1}",ex.GetType(),ex.Message);
                if (logger != null) {
                    logger.LogCritical(ex,msg);
                    logger.LogCritical(ex.StackTrace);
                    NLog.LogManager.Flush();                
                }
                Console.WriteLine(msg);
                Console.WriteLine("See log for more details");
            }
        }
        /// <summary>
        /// вывести результат в консоль и в файл excel,
        /// если это указано в опциях
        /// </summary>
        /// <param name="result"></param>
        private static void OutputResult(ISimulationResult result)
        {
            Console.WriteLine();
            //выведем на консоль результаты
            foreach (string str in result.ToText())
            {
                Console.WriteLine(str);
                logger.LogInformation(str);
            }

            //проверим, указан ли файл для вывода в Excel
            logger.LogInformation("Creating Excel output file");
            FileParam excelFile = configRoot.GetSection("output").Get<FileParam>();
            if (FileParam.IsEmpty(excelFile)){
                string msg = "Excel output file not specified in config file and thus won't be generated";
                logger.LogWarning(msg);
                Console.WriteLine(msg);
                return;
            }
            result.ToExcel(excelFile.fileName);
            logger.LogInformation("Excel output file created");

        }

        /// <summary>
        /// инициализирует логирование и пр.
        /// </summary>
        private static void Configure()
        {
            var services = new ServiceCollection();
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Trace));
            var provider = services.BuildServiceProvider();

            logFactory = provider.GetService<ILoggerFactory>();
            logFactory.AddNLog(new NLogProviderOptions() {  IncludeScopes = true} );
            contextFactory = new ExcelConfigFactory(logFactory);
            NLog.LogManager.LoadConfiguration("nlog.config");

            logger = provider.GetService<ILogger<Program>>();
            logger.LogDebug("logging up. Reading  appsettings.json  ");


            configRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();


        }
    }
}

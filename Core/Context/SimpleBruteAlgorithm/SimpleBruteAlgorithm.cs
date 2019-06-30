using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Core.Config;
using Core.SimulationResult;
using Microsoft.Extensions.Logging;

namespace Core.Context.SimpleBruteAlgorithm
{
    public class SimpleBruteAlgorithm : ISimulationContext
    {
        public ILoggerFactory LoggerFactory { get; set; }
        public ConfigBundle InputParams { get; set; }

        public virtual string AlgorithmDescription => 
            "Решение 'в лоб' : распределяем задачи по оборудованию в порядке их поступления на менее занятое оборудование";

        protected ILogger logger;
        public virtual ISimulationResult Simulate()
        {
            SimpleSimulationResult simulationResult = new SimpleSimulationResult();
            CheckInitializationAndInputData(simulationResult);
            if (simulationResult.HasErrors){
                return simulationResult;
            }
            //создадим свою модель, специально заточенную под этот алгоритм
            Model model = CreateModel();
            RunSimulation(model);
            return model.ToSimulationResult();
        }

       

        private void RunSimulation(Model model) {
            logger.LogInformation("starting simulation");
            foreach (var task in model.AllTasks){
                Equipment eq = model.FindLessLoadedForTask(task);
                model.Assign(task, eq);
            }  
            logger.LogInformation("simulation successfully finished");
        }

        private void CheckInitializationAndInputData(ISimulationResult simulationResult)
        {
            if ((LoggerFactory == null) || (InputParams == null))
            {
                simulationResult.Errors.Add(new ValidationResult("Modelling context not initialized. Can't contnue"));
                return ;
            }
            logger = LoggerFactory.CreateLogger(this.GetType());
            InputParams.Validate();
            if (!InputParams.isValid)
            {
                foreach (var err in InputParams.ValidationResults)
                {
                    logger.LogError(string.Format("input validation : {0}", err.ErrorMessage));
                }
                //входные данные некорректны или не согласованы или еще какая беда с ними
                List<ValidationResult> result = new List<ValidationResult>(InputParams.ValidationResults);
                result.Insert(0, new ValidationResult("input data is not valid or incorrect"));
                simulationResult.Errors = result;
                return ;
            }

        }

        private Model CreateModel()
        {    //в интерфейсе прописаны только входные данные и общий вид результата
             //поэтому каждый алгоритм может создавать 
            //свою собственную модель данных на основе входных данных
            logger.LogInformation("Creating Model");

            Model model = new Model(this.AlgorithmDescription);

            //создадим список оборудования по ID
            Dictionary<string, Equipment> equipmentById = new Dictionary<string, Equipment>(
                InputParams.Equipment.ConvertAll<KeyValuePair<string, Equipment>>( 
                    p => new KeyValuePair<string, Equipment>(p.id,new Equipment() {
                         id = p.id,
                         name= p.name
                     }))
                );
            //сразу добавим в модель, потому что с оборудованием больше ничего не надо делать
            model.Equipment.AddRange(equipmentById.Values);

            //для каждого типа руды получим время обработки в зависимости от оборудования
            Dictionary<string, Dictionary<Equipment, double>> processingTime = new Dictionary<string, Dictionary<Equipment, double>>();
            foreach(var timing in InputParams.Timings) {
                if (!processingTime.ContainsKey(timing.materialId)){
                    processingTime[timing.materialId] = new Dictionary<Equipment, double>();
                }
                double time = Convert.ToDouble(timing.timing);
                Equipment eq = equipmentById[timing.equipmentId];
                processingTime[timing.materialId][eq] = time;
            }

            //создадим список задач
            Dictionary<string, string> nomenclatureNameById = new Dictionary<string, string>(
                InputParams.Nomenclature.ConvertAll<KeyValuePair<string, string>>( 
                    p=> new KeyValuePair<string, string>(p.id,p.name))
                );
            model.AllTasks.AddRange(InputParams.Parties.ConvertAll<Task>(p=> new Task() {
                 id = p.id,
                 description = nomenclatureNameById[p.nomenclatureId],
                  Timing = processingTime[p.nomenclatureId]
            }));
            //скажем модели, что будем начинать новое моделирование
            model.Reset();
            logger.LogInformation("Model created ");
            return model;
        }
    }
}

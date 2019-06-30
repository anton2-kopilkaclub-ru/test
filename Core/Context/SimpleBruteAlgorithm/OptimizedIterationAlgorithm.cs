using System;
using System.Collections.Generic;
using System.Text;
using Core.SimulationResult;
using Microsoft.Extensions.Logging;

namespace Core.Context.SimpleBruteAlgorithm
{
    /// <summary>
    /// пытается итерационно улучшить результаты алгоритма "в лоб"
    /// </summary>
    public class OptimizedIterationAlgorithm : SimpleBruteAlgorithm, IContextWithOptions
    {
        public override string AlgorithmDescription => "итерационная оптимизация прямого алгоритма ";
        public Type OptionClass => typeof(OptimizedIterationOptions);

        private OptimizedIterationOptions options = new OptimizedIterationOptions();
        public void SetOptions(object rawOptions){
            OptimizedIterationOptions options = rawOptions as OptimizedIterationOptions;
            if (options == null){
                throw new ArgumentException("SetOptions() called with wrong type or null");
            }
            this.options = options;
        }

        public override ISimulationResult Simulate()
        {
            SimpleSimulationResult notOptimized = (SimpleSimulationResult)base.Simulate();
            if (notOptimized.HasErrors){
                //нечего тут оптимизировать, одни ошибки
                return notOptimized;
            }
            //получаем модель по состоянию на завершение моделирования предыдущим методом
            Model model = notOptimized.Model;
            double initialCost = model.GetTotalTime();

            bool optimizationStageOk = OptimizeByGain(model);
            if (!optimizationStageOk){
                //состояние модели  поменялось, вторую стадию не запускаем
                //todo: выполнить model.Reset(), провести повторную симуляцию
                //базовым алгоритмом и тогда можно даже в этом случае запустить вторую стадию
                logger.LogWarning("Optimization stage1 failed, returning non-optimized result");
                return notOptimized;

            }
            //пытаемся перенести задачи с самого загруженного оборудования на простаивающее
            //так, чтобы общее время уменьшилось
            OptimizeByIdleTime(model);
            logger.LogInformation(string.Format("Total optimization benefit after 2 stages : {0}",
                initialCost - model.GetTotalTime()));

            return model.ToSimulationResult();
        }

        /// <summary>
        /// выполнять пошагово оптимизацию, пока не превышено ограничение на количество шагов
        /// и на шаге оптимизации хоть что-то было сделано
        /// </summary>
        private void OptimizeBySteps(Model model,string stageName, Func<Model,bool> tryOptimize)
        {
            logger.LogInformation(string.Format("Starting optimization {0}",stageName));
            //найдем самое загруженное оборудование
            // Equipment eq = findTheMostBusyEq(model);

            double startingCost = model.GetTotalTime();
            int step = 0;
            double currentCost = startingCost;
            double prevCost;
            bool optimizationMade;
            do
            {
                step++;
                prevCost = currentCost;
                optimizationMade = tryOptimize(model);
                currentCost = model.GetTotalTime();
                logger.LogDebug(string.Format("Optimization step {0}. Total cost {1}, total optimization benefit {2}, benefit for this step {3}",
                     step, currentCost, startingCost - currentCost, prevCost - currentCost
                    ));
            }
            while (step < options.maxIterations && optimizationMade);


            logger.LogInformation(string.Format("optimization finished {0}",stageName));

        }
        private void OptimizeByIdleTime(Model model)
        {
            OptimizeBySteps(model, "stage 2", (p) => MakeOptimizationStepStage2(p));
        }

        private bool MakeOptimizationStepStage2(Model model)
        {
            //найдем самое загруженное оборудование
            Equipment eq = findTheMostBusyEq(model);

            //всё остальное оборудование отсортируем по времени простоя
            List<Equipment> otherEq = new List<Equipment>();
            otherEq.AddRange( model.Equipment.FindAll((p) =>  p != eq));
            otherEq.Sort((x, y) => y.idleTime.CompareTo(x.idleTime) );

            //смотрим начиная с самого наименее загруженного оборудования,
            //чтобы при переносе задача полностью погрузилась во время простоя
            //сравнение должно быть строгим, чтобы не было бесполезных "туда-сюда-шатаний"
            for(int i = 0; i < otherEq.Count; i++){
                Equipment currEq = otherEq[i];
                double idleTime = currEq.idleTime;
                //найдем задачу, которую можно целиком перебросить на это оборудование
                Task task = eq.AssignedTasks.Find((p) =>{
                    //задача должна допускать работу на другом оборудовании
                    return p.Timing.ContainsKey(currEq) && (p.Timing[currEq] < idleTime);
                });
                //нашли подходящую задачу, перебрасываем
                if (task != null){
                    model.Unassign(task, eq);
                    model.Assign(task, currEq);
                    logger.LogTrace(string.Format("moving task '{1}' id={0} :  {2} id={3} ==> {4} id={5} trying to minimize idle time",
                                    task.id, task.description,
                                    eq.name, eq.id,
                                    currEq.name, currEq.id
                                    ));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// итерационная оптимизация по принципу перекидывания задачи на другое оборудование,
        /// если она там быстрее выполняется
        /// Возвращает false, если общее время только выросло
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool OptimizeByGain(Model model)
        {
            //запомним, что у нас было с затратами в самом начале
            double startingCost = model.GetTotalTime();
            OptimizeBySteps(model, "stage 1", (p) => MakeOptimizationStep(p));
            return model.GetTotalTime() < startingCost;
        }

        private bool MakeOptimizationStep(Model model)
        {
            //найдем самое загруженное оборудование
            Equipment eq = findTheMostBusyEq(model);

            Equipment whereToPut = eq;
            Task task = eq.AssignedTasks[0]; //обязательно будет, иначе вообще нет ни одной задачи бы не было
            double maxGain = 0;
            //найдем самую выгодную задачу для переноса на другое оборудование
            eq.AssignedTasks.ForEach(p=> {
                double timeForCurrent = p.Timing[eq]; //эта задача того самого обрудования
                foreach(var pair in p.Timing){
                    double gain = timeForCurrent - pair.Value;
                    //чтобы не было "лишних" движений, 
                    //нужно пропускать так же равноценные варианты
                    //todo: сделать это опцией и посмотреть....
                    if (gain <= maxGain) { 
                        continue;
                    }
                    //запомним, на какое оборудование можно перенести 
                    //и какая от этого польза будет
                    maxGain = gain;
                    whereToPut = pair.Key;
                    task = p;
                }
            });

            //проверим, вдруг уже все "заоптимизировали"
            if (maxGain == 0){
                return false; 
            }
            //перекинем задачу на найденное более выгодное оборудование
            model.Unassign(task,eq);
            model.Assign(task,whereToPut);
            logger.LogTrace(string.Format("switching task '{1}' id={0} :  {2} id={3} ==> {4} id={5} expected gain {6}",
                task.id,task.description,
                eq.name,eq.id,
                whereToPut.name, whereToPut.id,
                maxGain));
            return true;
        }

        private Equipment findTheMostBusyEq(Model model)
        {
            Equipment eq = model.Equipment[0];
            double maxTiming = 0;
            model.Equipment.ForEach(p => { //найдем самое загруженное оборудование
                if (p.currentTiming > maxTiming)
                {
                    maxTiming = p.currentTiming;
                    eq = p;
                }
            });
            return eq;
        }
    }

    
    /// <summary>
    /// настройки
    /// </summary>
    public class OptimizedIterationOptions
    {
        /// <summary>
        /// максимально допустимое количество итераций
        /// </summary>
        public int maxIterations { get; set; } = 1000;
    }
}

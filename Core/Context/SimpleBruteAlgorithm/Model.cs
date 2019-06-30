using Core.SimulationResult;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Context.SimpleBruteAlgorithm
{
    /// <summary>
    /// модель всей логики для этого алгоритма
    /// </summary>
    internal class Model
    {
        /// <summary>
        /// список оборудования
        /// </summary>
        public List<Equipment> Equipment { get; set; } = new List<Equipment>();

        /// <summary>
        /// вообще все задачи
        /// </summary>
        public List<Task> AllTasks { get; set; } = new List<Task>();

        /// <summary>
        /// сбрасывает состояние модели к состоянию "перед моделированием"
        /// </summary>
        public void Reset(){
            double initialTiming = 0;
            Equipment.ForEach((p)=> { //сбрасываем состояние оборудования
                p.currentTiming = initialTiming;
                p.idleTime = 0;
                p.AssignedTasks.Clear();  
                });
        }

        /// <summary>
        /// находит наименее загруженное оборудование из тех, что способны выполнить задачу
        /// </summary>
        /// <returns></returns>
        public Equipment FindLessLoadedForTask(Task task){
            double timing = double.MaxValue;
            Equipment bestEq = null;
            // выберем  оборудование с меньшим текущим таймером
            //из тех, что могут обработать заявку
            foreach(var eq in task.Timing.Keys) {
                if (eq.currentTiming < timing){
                    timing = eq.currentTiming;
                    bestEq = eq;
                }
            }
            return bestEq;
        }

        /// <summary>
        /// назначить задачу указанному оборудованию
        /// </summary>
        /// <param name="task"></param>
        /// <param name="eq"></param>
        public void Assign(Task task, Equipment eq){
            if (!task.Timing.ContainsKey(eq)){
                throw new ApplicationException(string.Format("attempt to assign task id = '{0}' to equipment id='{1}'",task.id,eq.id));
            }
            eq.AssignedTasks.Add(task);
            eq.currentTiming += task.Timing[eq];

            UpdateIdleTiming();
        }

        /// <summary>
        /// получить общее время работы оборудования
        /// на текущий момент
        /// </summary>
        /// <returns></returns>
        public double GetTotalTime() {
            double timing = 0;
            foreach(var eq in Equipment){
                if (eq.currentTiming > timing) {
                    timing = eq.currentTiming;
                }
            }
            return timing;
        }

        public Model() { }
        public string AlgorithmName { get; set; }
        public Model(string algorithmName) : base() {
            this.AlgorithmName = algorithmName;
        }

        public void Unassign(Task task, Equipment eq)
        {
            if (!eq.AssignedTasks.Contains(task))
            {
                throw new ApplicationException(string.Format("attempt to unAssign task id = '{0}' from equipment id='{1}' which is not assigned", task.id, eq.id));
            }
            eq.AssignedTasks.Remove(task);
            eq.currentTiming -= task.Timing[eq];

            UpdateIdleTiming();
        }

        /// <summary>
        /// пересчитать время простоя для всего оборудования
        /// </summary>
        private void UpdateIdleTiming()
        {
            double totalTiming = GetTotalTime();
            Equipment.ForEach((p) => { //апдейтим время простоя
                p.idleTime = totalTiming - p.currentTiming;
            });

        }
    }

    internal static class ModelExtension {
        internal static SimpleSimulationResult ToSimulationResult(this Model model)
        {
            SimpleSimulationResult simulationResult = new SimpleSimulationResult();
            simulationResult.Model = model;
            simulationResult.Footnotes.Add(string.Format("Использован алгоритм {0}",model.AlgorithmName));
            
            simulationResult.Metrics.Add("общее время", model.GetTotalTime().ToString());
            simulationResult.TaskByEquipment.Clear();
            Dictionary<string, string> idleTimes = new Dictionary<string, string>();

            //заполним табличку по загрузке оборудования,
            //попутно посчитаем время простоя
            double totalIdleTime = 0;
            foreach (var eq in model.Equipment)
            {
                idleTimes.Add(eq.name, eq.idleTime.ToString());
                totalIdleTime += eq.idleTime;
                List<IAssignedTask> tasks = new List<IAssignedTask>();
                simulationResult.TaskByEquipment.Add(eq, tasks);
                double time = 0;
                //добавим в результат все задачи, назначенные этому оборудованию
                foreach (var task in eq.AssignedTasks)
                {
                    tasks.Add(new AssignedTask()
                    {
                        id = task.id,
                        description = task.description,
                        startedAt = time,
                        duration = task.Timing[eq]
                    });
                    time += task.Timing[eq];
                }
            }

            //добавим метрики по времени простоя
            simulationResult.Metrics.Add("всего время простоя", totalIdleTime.ToString());
            foreach (var pair in idleTimes)
            {
                simulationResult.Metrics.Add(pair.Key, pair.Value);
            }
            return simulationResult;
        }
    }
}

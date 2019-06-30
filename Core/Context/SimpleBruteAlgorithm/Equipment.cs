using Core.SimulationResult;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Context.SimpleBruteAlgorithm
{
    /// <summary>
    /// модель оборудования
    /// </summary>
   internal class Equipment : IEquipment
    {
        public string id { get; set; }
        public string name { get; set; }

        /// <summary>
        /// текущая загруженность, 
        /// т.е. время, с которого начнет выполняться следующая добавленная задача
        /// </summary>
        public double currentTiming { get; set; }

        /// <summary>
        /// отставание от "лидера", то есть время простоя оборудования
        /// </summary>
        public double idleTime { get; set; }

        /// <summary>
        /// список задач, назначенных данному оборудованию
        /// </summary>
        public List<Task> AssignedTasks { get; set; } = new List<Task>();


    }
}

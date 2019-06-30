using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Context.SimpleBruteAlgorithm
{
    /// <summary>
    /// модель для задачи
    /// (объединяем руду и время ее выполнения)
    /// </summary>
    internal class Task
    {
        /// <summary>
        /// id партии
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// описание (название руды)
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// время выполнения на разном оборудовании
        /// если на каком-то оборудовании нельзя выполнить,
        /// этого ключа не будет
        /// </summary>
        public IDictionary<Equipment, double> Timing { get; set; } = new Dictionary<Equipment, double>();
    }
}

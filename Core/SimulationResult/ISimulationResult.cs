using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Core.SimulationResult
{
    /// <summary>
    /// результат моделирования
    /// </summary>
    public interface ISimulationResult
    {
        /// <summary>
        /// были ли ошибки,
        /// которые не позволили провести моделирование
        /// </summary>
        bool HasErrors { get; }

        /// <summary>
        /// список ошибок, возникших в процессе
        /// </summary>
        ICollection<ValidationResult> Errors { get; set; }

        /// <summary>
        /// различные примечания
        /// </summary>
        IList<string> Footnotes { get; }

        /// <summary>
        /// различные метрики
        /// </summary>
        IDictionary<string,string> Metrics { get; }

        /// <summary>
        /// распределение задач по оборудованию
        /// </summary>
        IDictionary<IEquipment,IList<IAssignedTask>> TaskByEquipment { get; }
        
       /// <summary>
        /// получить результат (даже если были ошибки)
        /// в виде текстовых строк
        /// </summary>
        /// <returns></returns>
        IList<string> ToText();

    }
}

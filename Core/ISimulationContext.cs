using Core.Config;
using Core.SimulationResult;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    /// <summary>
    /// собственно алгоритм интегрирования
    /// </summary>
    public interface ISimulationContext
    {
        /// <summary>
        /// заполняет создатель конкретного экземпляра алгоритма
        /// </summary>
        ILoggerFactory LoggerFactory { get; set; }

        /// <summary>
        /// полный набор входных параметров, уже инициализированных и заполненных
        /// </summary>
        ConfigBundle InputParams { get; set; }

        /// <summary>
        /// производит моделирование, 
        /// перед этим проверяя входные параметры
        /// </summary>
        ISimulationResult Simulate();

        /// <summary>
        /// описание, что это за алгоритм
        /// </summary>
        string AlgorithmDescription { get; }
    }
}

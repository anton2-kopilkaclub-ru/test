using Core.SimulationResult;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Context.SimpleBruteAlgorithm
{
    /// <summary>
    /// для представления результата
    /// </summary>
    internal class AssignedTask : Task, IAssignedTask
    {
        public double startedAt { get; set; }

        public double stoppedAt {
            //если тайминг всегда целый, то можно даже (startedAt + duration - 1),
            //будет видно, что сначала отработали от 0 до 19, затем пошли с 20 следующую обрабатывать
            //но для дробных таймингов (т.е. оборудование работает непрерывно)
            //так не получится, поэтому оставляем как есть
            get { return startedAt + duration; }
        }

        public double duration { get; set; }
    }
}

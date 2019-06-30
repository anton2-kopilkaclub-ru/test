using System;
using System.Collections.Generic;
using System.Text;

namespace Core.SimulationResult
{
    public interface IAssignedTask
    {
        /// <summary>
        /// id партии
        /// </summary>
        string id { get;  }
        /// <summary>
        /// описание (название руды)
        /// </summary>
         string description { get;  }

        /// <summary>
        /// время начала выполнения задачи
        /// </summary>
        double startedAt { get;  }

        /// <summary>
        /// время окончания выполнения задачи
        /// </summary>
        double stoppedAt { get;  }

        /// <summary>
        /// сколько выполнялась задача
        /// </summary>
        double duration { get; }
    }
}

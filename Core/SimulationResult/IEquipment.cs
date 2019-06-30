using System;
using System.Collections.Generic;
using System.Text;

namespace Core.SimulationResult
{
    /// <summary>
    /// интерфейс для выдачи результата по оборудованию
    /// </summary>
    public interface IEquipment
    {
        string id { get; set; }
        string name { get; set; }

    }
}

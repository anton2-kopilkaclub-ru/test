using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Core.Config.DTO
{
    /// <summary>
    /// DTO для оборудования
    /// </summary>
    public class MachineToolsDTO
    {
        [Required]
        public string id { get; set; }
        [Required]
        public string name { get; set; }

        public override string ToString()
        { //for logging purposes
            return string.Format("{0} id={1}, name={2}", this.GetType().Name,id, name);
        }
    }
}

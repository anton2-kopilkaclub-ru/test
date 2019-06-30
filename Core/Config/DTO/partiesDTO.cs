using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Core.Config.DTO
{
    /// <summary>
    /// DTO для партий
    /// </summary>
    public class partiesDTO
    {
        [Required]
        public string id { get; set; }
        [Required]
        public string nomenclatureId { get; set; }

        public override string ToString()
        { //for logging purposes
            return string.Format("{0} id={1}, nomenclatureId={2}", this.GetType().Name, id, nomenclatureId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Core.Config.DTO
{
    /// <summary>
    /// DTO  для таймингов
    /// </summary>
    public class TimingDTO
    {
        [Required]
        public string timing { get; set; }
        [Required]
        public string materialId { get; set; }
        [Required]
        public string equipmentId { get; set; }

        public override string ToString()
        { //for logging purposes
            return string.Format("{0} equipmentId={1}, nomenclatureId={2}, timing={3}", 
                this.GetType().Name, equipmentId, materialId,timing);
        }
    }
}

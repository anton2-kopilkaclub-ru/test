using Core.Context.SimpleBruteAlgorithm;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Core.SimulationResult
{
    public class SimpleSimulationResult : ISimulationResult
    {
        internal Model Model { get; set; }
        public bool HasErrors { get => Errors.Count > 0; }

        public ICollection<ValidationResult> Errors { get; set; } = new List<ValidationResult>();

        public IList<string> Footnotes { get; set; } = new List<string>();

        public IDictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();

        public IDictionary<IEquipment, IList<IAssignedTask>> TaskByEquipment { get; set; } =
            new Dictionary<IEquipment, IList<IAssignedTask>>();

        public IList<string> ToText()
        {
            List<string> result = new List<string>();
            if (HasErrors) { //были ошибки, просто вернем их список
                result.Add("Following errors occured during modelling ");
                foreach (var err in Errors) {
                    result.Add(err.ErrorMessage);
                }
                return result;
            }
            //добавим вывод задач, назначенных на оборудование
            foreach(IEquipment eq in TaskByEquipment.Keys){
                StringBuilder str = new StringBuilder();
                str.Append(string.Format("id='{1}' {0} : ", eq.name,eq.id));
                bool first = true;
                foreach(var task in TaskByEquipment[eq]) {
                    str.Append(string.Format("{4} id{0} '{1}' {2}-{3}", task.id,task.description,
                        task.startedAt,task.stoppedAt,first ? string.Empty : " -> "));
                    first = false;
                }
                result.Add(str.ToString());
            }

            //добавим метрики
            result.Add(string.Empty);
            foreach (var pair in Metrics){
                result.Add(string.Format("{0} : {1}",pair.Key,pair.Value)); 
            }

            //и примечания
            result.Add(string.Empty);
            result.AddRange(this.Footnotes);
            return result;
        }
    }
}

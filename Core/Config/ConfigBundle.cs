using Core.Config.DTO;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using System.Collections;

namespace Core.Config
{
    /// <summary>
    /// все входные параметры для алгоритма
    /// </summary>
   public class ConfigBundle
    {
        /// <summary>
        /// входная конфигурация; там есть настройки по проверке данных
        /// </summary>
        public ConfigParams InputParams { get; set; }
        /// <summary>
        /// входные данные для таймингов
        /// </summary>
        [Required(ErrorMessage = "Нет данных по времени обработки")]
        public List<TimingDTO> Timings { get; set; }

        /// <summary>
        /// входные данные для оборудования
        /// </summary>
        [Required(ErrorMessage = "Нет данных по оборудованию")]
        public List<MachineToolsDTO> Equipment { get; set; }

        /// <summary>
        /// входные данные для номенклатуры
        /// </summary>
        [Required(ErrorMessage = "Нет данных по номенклатуре")]
        public List<NomenclatureDTO> Nomenclature { get; set; }

        /// <summary>
        /// входные данные для партий
        /// </summary>
        [Required(ErrorMessage ="Нет данных по партиям")]
        public List<partiesDTO> Parties { get; set; }

        
        /// <summary>
        /// являются ли данные согласованными и корректными
        /// устанавливается методом Validate()
        /// </summary>
        public bool isValid { get; set; }

        /// <summary>
        /// результат проверки входных данных
        /// устанавливается методом Validate()
        /// </summary>
        public ICollection<ValidationResult> ValidationResults { get; set; }

        /// <summary>
        /// проверить согласованность входных данных
        /// </summary>
        /// <returns></returns>
        public void Validate()
        {
            logger.LogInformation("checking input data");
            this.ValidationResults = new List<ValidationResult>();
            var context = new ValidationContext(this, serviceProvider: null, items: null);
            //сначала проверяем  себя любимого, вдруг, например, коллекции не инициализировались
            this.isValid = Validator.TryValidateObject(this, context, ValidationResults);
            if (!this.isValid) {
                return ; 
            }
            //проверяем все объекты в коллекциях как отдельные объекты
            ValidateElements(this.Nomenclature,ValidationResults);
            ValidateElements(this.Parties,  ValidationResults);
            ValidateElements(this.Timings,  ValidationResults);
            ValidateElements(this.Equipment, ValidationResults);

            this.isValid = (ValidationResults.Count == 0);
            if (!this.isValid){ //нет смысла проверять согласованность данных, если в самих данных ошибки
                return ;
            }
            //порядок важен; кроме согласованности,
            //проверяются также сами данные, не только согласованность
            //ошибки кидаем в ValidationResults
            this.isValid = CheckNomenclature()
                && CheckEquipment()
                && CheckParties()
                && CheckTimings();
            if (this.isValid){
                logger.LogInformation("input data checked ok");
            }
        }

        private bool CheckTimings()
        {
            Timings.ForEach((p) => {
                p.equipmentId = p.equipmentId.Trim();
                p.materialId = p.materialId.Trim();
                p.timing = p.timing.Trim();
            });
            //проверим, что для каждой записи по таймингу существует 
            //указанная номенклатура и указанное оборудование
            //не должно быть номенклатуры, которую никто не умеет обрабатывать
            //а также что сам тайминг приводится к числу (хотя бы double)
            //и строго больше нуля
            Dictionary<string, bool> existingNomenclature = new Dictionary<string, bool>();
            foreach (var item in Nomenclature){
                existingNomenclature.Add(item.id, false);
            }
            List<string> existingEquipmentId = Equipment.ConvertAll<string>(p => p.id);
            bool validatedOk = true;
            foreach (var item in Timings) {
                double timing;
                if (!double.TryParse(item.timing,out timing)){
                    ValidationResults.Add(new ValidationResult(string.Format("Time is not a number: {0}", item.ToString())));
                    validatedOk = false;
                }
                if (timing <= 0) {
                    ValidationResults.Add(new ValidationResult(string.Format("Time should be positive: {0}", item.ToString())));
                    validatedOk = false;
                }
                if (!existingEquipmentId.Contains(item.equipmentId)) {
                    ValidationResults.Add(new ValidationResult(string.Format("Foreign key violation: no equipment with id='{0}' in {1}", item.equipmentId, item.ToString())));
                    validatedOk = false;
                }
                if (!existingNomenclature.ContainsKey(item.materialId)){
                    ValidationResults.Add(new ValidationResult(string.Format("Foreign key violation: no nomenclature with id='{0}' in {1}", item.materialId, item.ToString())));
                    validatedOk = false;
                }
                else {
                    existingNomenclature[item.materialId] = true;
                }
            }
            //вдруг никто не умеет обрабатывать какой-то тип руды    
            foreach(var pair in existingNomenclature){
                if (!pair.Value){
                    ValidationResults.Add(new ValidationResult(string.Format("Foreign key violation: no equipment can treat nomenclature id='{0}' ", pair.Key)));
                    validatedOk = false;
                }
            }
            //проверим, что тайминг уникалин для пары оборудование + тип руды
            if (!CheckUnique<TimingDTO>(Timings, (p) => string.Format("{0}:{1}",
                    p.equipmentId,p.materialId),"machine_tool_id + nomenclature_id")) {
                validatedOk = false;
            };
            return validatedOk;
            }
        private bool CheckParties()
        {
            Parties.ForEach((p) => {
                p.id = p.id.Trim();
                p.nomenclatureId = p.nomenclatureId.Trim();
            });
            //потребуем уникальность id
            if (!CheckUnique<partiesDTO>(Parties, (p) => p.id, "id")){
                return false;
            };
            //проверим, что для каждой записи по партии существует указанная номенклатура
            //и что для каждой номенклатуры есть хоть одна партия
            Dictionary<string, bool> usedNomenclature = new Dictionary<string, bool>();
            foreach (var item in Nomenclature) {
                usedNomenclature.Add(item.id, false);
            }
            //при первой же ошибке - выходим
            foreach(var item in Parties){
                if (!usedNomenclature.ContainsKey(item.nomenclatureId)){
                    ValidationResults.Add(new ValidationResult(string.Format("Foreign key violation: no nomenclature for party: {0}",item.ToString())));
                    return false;
                }
                usedNomenclature[item.nomenclatureId] = true;
            }
            //проверим, вдруг не вся номенклатура использована
            foreach(var pair in usedNomenclature){
                if (!pair.Value) {
                    // предупреждения в логе покажем всегда
                    string msg = string.Format("Nomenclature id='{0}' not used in parties",pair.Key);
                    logger.LogWarning(msg);
                    if (InputParams.Options.errorIfNomenclatureNotUsed) {
                        //ошибку сгенерируем только если в настройках так указано
                        ValidationResults.Add(new ValidationResult(msg));
                        return false;
                    }
                }
            }
            return true;
        }
        private bool CheckEquipment()
        {
            Equipment.ForEach((p) => {
                p.id = p.id.Trim();
                p.name = p.name.Trim();
            });
            //потребуем уникальность не только id, но и названия
            return CheckUnique<MachineToolsDTO>(Equipment, (p) => p.id, "id") &&
                   CheckUnique<MachineToolsDTO>(Equipment, (p) => p.name, "name");
        }

        private bool CheckNomenclature()
        {
            Nomenclature.ForEach((p) => {
                p.id = p.id.Trim();
                p.name = p.name.Trim();
            });
            //потребуем уникальность не только id, но и названия
            return CheckUnique<NomenclatureDTO>(Nomenclature, (p) => p.id, "id") &&
                   CheckUnique<NomenclatureDTO>(Nomenclature, (p) => p.name,"name");
        }

/// <summary>
/// проверяет уникальность указанного параметра в пределах коллекции
/// </summary>
/// <returns></returns>
        private bool CheckUnique<T>(List<T> list, Func<T, string> getValue, string fieldName) {
            List<string> unique = new List<string>();
            foreach(var item in list){
                string toCheck = getValue(item);
                if (unique.Contains(toCheck)){
                    ValidationResults.Add(new ValidationResult(
                        string.Format("unique constraint violated, field {0} object {1} {2}",
                        fieldName,typeof(T).Name,item.ToString())));
                    return false;
                }
                unique.Add(toCheck);
            };
            return true;
        }


        /// <summary>
        /// валидирует каждый элемент коллекции в отдельности
        /// </summary>
        private void ValidateElements(ICollection items,  ICollection<ValidationResult> result ) {
            foreach (object item in items){
                ValidationContext context = new ValidationContext(item, serviceProvider: null, items: null);
                if (!Validator.TryValidateObject(item, context, result)) {
                    logger.LogError(string.Format("validation failed for element {0}",item.ToString()));
                }
            }
        }

        private ILogger logger;
        public ConfigBundle(ILoggerFactory loggerFactory) {
            this.logger = loggerFactory.CreateLogger(this.GetType());
        }
    }
}

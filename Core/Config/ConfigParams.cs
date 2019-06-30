using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Config
{
    /// <summary>
    /// конфигурация для загрузки файлов с данными
    /// </summary>
    public class ConfigParams
    {
        /// <summary>
        /// данные о файле с таймингом
        /// (times.xlsx)
        /// </summary>
        public FileParam Timing { get; set; }

        /// <summary>
        /// данные о файле с партиями руды
        /// (parties.xlsx)
        /// </summary>
        public FileParam Parties { get; set; }

        /// <summary>
        /// данные о файле с номенклатурой
        /// (nomenclatures.xlsx)
        /// </summary>
        public FileParam Nomenclature { get; set; }

        /// <summary>
        /// данные о файле с оборудованием
        /// (machine_tools.xlsx)
        /// </summary>
        public FileParam Equipment { get; set; }
        /// <summary>
        /// настройки для входных данных
        /// </summary>
        public InputOptions Options { get; set; } = new InputOptions();
    }

    public class InputOptions{
        /// <summary>
        /// выдавать ли ошибку, если не вся номенклатура была использована
        /// </summary>
        public bool errorIfNomenclatureNotUsed { get; set; }
    }
    /// <summary>
    /// данные о файле в конфигурации
    /// </summary>
    public class FileParam {
        public string fileName { get; set; }

        /// <summary>
        /// задано ли какое-нибудь значение для имя файла
        /// </summary>
        /// <returns></returns>
        public static bool IsEmpty(FileParam filename) {
            return ((filename == null) 
                || string.IsNullOrWhiteSpace(filename.fileName));
        }
    }
}

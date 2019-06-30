using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Context
{
    /// <summary>
    /// если алгоритм предусматривает настройку, он должен реализовывать этот интерфейс
    /// </summary>
    public interface IContextWithOptions
    {
        /// <summary>
        /// тип класса, к которому привязаны свойства
        /// </summary>
        Type OptionClass { get; }

        /// <summary>
        /// устанавливает свойства, проверяя соответствие типу
        /// </summary>
        void SetOptions(object options);
    }
}

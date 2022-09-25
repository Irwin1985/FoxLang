using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxLang
{
    public class Environment
    {
        private Dictionary<string, object> record;
        private Environment parent;

        /**
         * Crea un nuevo entorno con el record dado.
         */
        public Environment(Dictionary<string, object> record, Environment parent = null)
        {
            this.record = record;
            this.parent = parent;
        }
        /**
         * Define una variable en el record local.
         */
        public object Define(string name, object value)
        {
            record[name] = value;
            return value;
        }

        /**
         * Retorna el valor de una variable definida o arroja excepción.
         */
        public object Lookup(string name)
        {
            return Resolve(name).record[name];
        }

        /**
         * Actualiza el valor de una variable.
         */
        public object Assign(string name, object value)
        {
            Environment env = Resolve(name, false);
            if (env == null)
            {
                // Define the variable
                return Define(name, value);
            }
            env.record[name] = value;
            return value;
        }

        /**
         * Retorna el objeto Environment donde fue definida la variable.
         */
        public Environment Resolve(string name, bool throwError = true)
        {
            if (record.ContainsKey(name))
            {
                return this;
            }
            if (parent != null)
            {
                return parent.Resolve(name);
            }
            if (throwError)
                throw new RuntimeException("Variable " + name + " is not defined.");
            return null;
        }
    }
}

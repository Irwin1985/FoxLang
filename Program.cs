using System;
using System.Collections.Generic;

namespace FoxLang
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string input = @"
                    FUNCTION SALUDAR()
                        LPARAMETERS tcNombre, tcProfesion
                        RETURN 'Hola ' + tcNombre + '!, Felicidades por ser un gran ' + tcProfesion
                    ENDFUNC
                    SALUDAR('FELIPE', 'ALBAÑIL')
                ";
                Parser parser = new Parser(input);
                object[] AstProgram = parser.Parse();
            
                // Definimos el diccionario global
                Dictionary<string, object> globalRecord = new Dictionary<string, object>();
                globalRecord["version"] = "1.0";
                globalRecord["author"] = "Irwin Rodríguez <rodriguez.irwin@gmail.com>";


                Environment global = new Environment(globalRecord, null);
                Evaluator.SetGlobalEnvironment(global);
                object evaluated = Evaluator.Eval(AstProgram, global);
                if (evaluated != null)
                {
                    Console.WriteLine(evaluated.ToString());
                } else
                {
                    Console.WriteLine("null");
                }
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}

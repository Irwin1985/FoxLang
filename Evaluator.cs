using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FoxLang
{
    public class RuntimeException : Exception
    {
        public RuntimeException(string message) : base(message) { }
    }
    public class ReturnException : RuntimeException
    {
        public readonly object ReturnValue;
        public ReturnException(object returnValue) : base("") {
            ReturnValue = returnValue;
        }
    }
    public static class Evaluator
    {
        private static Environment global;
        public static void SetGlobalEnvironment(Environment globalEnvironment)
        {
            global = globalEnvironment;
        }
        public static object Eval(object[] node, Environment env)
        {
            switch (node[0].ToString())
            {
                case "Program":
                    return EvalProgram(node[1] as List<object>, env);
                case "BlockStatement":
                    return EvalBlockStatement(node, env);
                case "ExpressionStatement":
                    return Eval(node[1] as object[], env);
                case "NumericLiteral":                    
                case "StringLiteral":
                case "BooleanLiteral":
                case "NullLiteral":
                    return node[1];
                case "UnaryExpression":
                    return EvalUnaryExpression(node[1] as string, node[2] as object[], env);
                case "LogicalExpression":
                    return EvalLogicalExpression(node[1] as string, node[2] as object[], node[3] as object[], env);
                case "BinaryExpression":
                    return EvalBinaryExpression(node[1] as string, node[2] as object[], node[3] as object[], env);
                case "VariableStatement":
                    return DefineVariable(node, env);
                case "AssignmentExpression":
                    return EvalAssignmentExpression(node, env);
                case "Identifier":
                    return env.Lookup((node[1] as string).ToLower());
                case "ReturnStatement":
                    object returnValue = null;
                    if (node[1] != null)
                    {
                        returnValue = Eval(node[1] as object[], env);
                    }
                    throw new ReturnException(returnValue);
                case "IfStatement":
                    return EvalIfStatement(node, env);
                case "FunctionStatement":
                    env.Define((node[1] as object[])[1].ToString().ToLower(), new object[] { node, env});
                    return null;
                case "CallExpression":
                    return EvalCallExpression(node, env);
                default:
                    return null;
            }
            throw new RuntimeException("No implementation found for this node: " + node.ToString());
        }

        private static object EvalProgram(List<object> statements, Environment env)
        {
            object value = null;
            try
            {
                foreach (object[] stmt in statements)
                {
                    value = Eval(stmt, env);
                }
            } catch (ReturnException r)
            {
                return r.ReturnValue;
            }
            return value;
        }

        private static object EvalBlockStatement(object[] blockStmt, Environment env)
        {
            object value = null;
            List<object> statements = blockStmt[1] as List<object>;
            try
            {
                foreach (object[] stmt in statements)
                {
                    value = Eval(stmt, env);
                }
            }
            catch (ReturnException r)
            {
                throw r;
            }
            return value;
        }

        private static object EvalUnaryExpression(string optor, object[] right, Environment env)
        {
            object rightVal = Eval(right, env);
            if (optor == "-" || optor == "+")
            {
                if (rightVal.GetType() == typeof(int))
                {
                    int val = (int)rightVal;
                    return (optor == "-") ? -val : val;                        
                }                
            } else if (optor == "!")
            {
                if (rightVal.GetType() == typeof(bool))
                {
                    return !(bool)rightVal;
                }
            }
            throw new RuntimeException(string.Format("Operand must be a number for the {0} operator, got: {1}", optor, rightVal.GetType()));
        }

        private static object EvalLogicalExpression(string optor, object[] left, object[] right, Environment env)
        {
            try
            {
                bool leftVal = (bool)Eval(left, env);
                if (optor.ToLower() == "or")
                {
                    if (leftVal)
                    {
                        return leftVal;
                    }
                } else
                {
                    if (!leftVal)
                    {
                        return leftVal;
                    }
                }
                return Eval(right, env);
            } catch(Exception e)
            {
                throw e;
            }
        }
        private static object EvalBinaryExpression(string optor, object[] left, object[] right, Environment env)
        {
            object leftVal = Eval(left, env);
            object rightVal = Eval(right, env);
            if (leftVal.GetType() == typeof(int) && rightVal.GetType() == typeof(int))
            {
                return EvalBinaryIntegerExpression(optor, (int)leftVal, (int)rightVal);
            } else if (leftVal.GetType() == typeof(bool) && rightVal.GetType() == typeof(bool))
            {
                return EvalBinaryBooleanExpression(optor, (bool)leftVal ? 1 : 0, (bool)rightVal ? 1 : 0);
            } else if (leftVal.GetType() == typeof(string) && rightVal.GetType() == typeof(string))
            {
                return EvalBinaryStringExpression(optor, (string)leftVal, (string)rightVal);
            } else if (leftVal.GetType() == typeof(string) && rightVal.GetType() == typeof(int))
            {
                if (optor == "*")
                {
                    return new StringBuilder().Insert(0, (string)leftVal, (int)rightVal).ToString();
                }
            }
            throw new RuntimeException(string.Format("Invalid operands for the '{0}' operator: {1}, {2}.", optor, leftVal.GetType(), rightVal.GetType()));
        }

        private static object EvalBinaryIntegerExpression(string optor, int leftVal, int rightVal)
        {
            switch (optor)
            {
                case "+":
                    return leftVal + rightVal;
                case "-":
                    return leftVal - rightVal;
                case "*":
                    return leftVal * rightVal;
                case "/":
                    return leftVal / rightVal;
                case "==":
                    return leftVal == rightVal;
                case "!=":
                    return leftVal != rightVal;
                case "<":
                    return leftVal < rightVal;
                case ">":
                    return leftVal > rightVal;
                case "<=":
                    return leftVal <= rightVal;
                case ">=":
                    return leftVal >= rightVal;
                default:
                    throw new RuntimeException("Invalid operator: " + optor);
            }
        }

        private static object EvalBinaryBooleanExpression(string optor, int leftVal, int rightVal)
        {

            switch (optor)
            {
                case "*":
                    return leftVal * rightVal == 1;
                case "==":
                    return leftVal == rightVal;
                case "!=":
                    return leftVal != rightVal;
                case "<":
                    return leftVal < rightVal;
                case ">":
                    return leftVal > rightVal;
                case "<=":
                    return leftVal <= rightVal;
                case ">=":
                    return leftVal >= rightVal;
                default:
                    throw new RuntimeException("Invalid operator: " + optor);
            }
        }

        private static object EvalBinaryStringExpression(string optor, string leftVal, string rightVal)
        {
            switch (optor)
            {
                case "+":
                case "-":
                    return leftVal + rightVal;
                case "==":
                    return leftVal == rightVal;
                case "!=":
                    return leftVal != rightVal;
                case "<":
                    return leftVal.GetHashCode() < rightVal.GetHashCode();
                case ">":
                    return leftVal.GetHashCode() > rightVal.GetHashCode();
                case "<=":
                    return leftVal.GetHashCode() <= rightVal.GetHashCode();
                case ">=":
                    return leftVal.GetHashCode() >= rightVal.GetHashCode();
                default:
                    throw new RuntimeException("Invalid operator: " + optor);
            }
        }

        private static object DefineVariable(object[] node, Environment env)
        {
            Environment environment = env;
            if (node[1].ToString() == "public")
            {
                environment = global;
            }

            List<object> declarations = node[2] as List<object>;

            for (int i = 0; i < declarations.Count; i++)
            {
                object[] declaration = declarations[i] as object[];
                VariableDeclaration(declaration, env, environment);
            }

            return null;
        }

        private static void VariableDeclaration(object[] node, Environment env, Environment defineEnv)
        {
            object value = null;
            if (node[3] != null)
            {
                // resolvemos primero en el Environment donde vamos a definir la variable.
                // value = Eval(node[3] as object[], env);
                try
                {
                    value = Eval(node[3] as object[], defineEnv);
                } catch (Exception e)
                {
                    Console.WriteLine("policia error: buscamos en otro...");
                    // Resolvemos en el ámbito actual.
                    value = Eval(node[3] as object[], env);
                }
            }
            else
            {
                if (node[2] != null) // tiene tipo definido
                {
                    object[] identifier = node[2] as object[];
                    switch (identifier[1].ToString().ToLower())
                    {
                        case "string": value = ""; break;
                        case "number": value = 0.0; break;
                        case "boolean": value = false; break;
                        default:
                            value = null; break;
                    }
                }
            }
            object[] name = node[1] as object[];
            defineEnv.Define(name[1].ToString().ToLower(), value);
        }

        private static object EvalIfStatement(object[] node, Environment env)
        {
            var condition = Eval(node[1] as object[], env);
            if (IsTruthy(condition))
            {
                return Eval(node[2] as object[], env);
            }
            if (node[3] != null)
            {
                return Eval(node[3] as object[], env);
            }
            return null;
        }

        private static object EvalAssignmentExpression(object[] node, Environment env)
        {
            var value = Eval(node[3] as object[], env);
            if ((node[2] as object[])[0].ToString() != "Identifier")
            {
                throw new RuntimeException("Unsupported target node");
            }
            var name = (node[2] as object[])[1].ToString().ToLower();
            return env.Assign(name, value);
        }

        private static object EvalCallExpression(object[] node, Environment env)
        {
            object result = Eval(node[1] as object[], env);
            
            // Validar que el objeto sea un array.
            if (result.GetType() != typeof(object[]))
                throw new RuntimeException("Not a function.");

            object[] value = result as object[];

            object[] funObj = value[0] as object[];
            Environment funEnv = value[1] as Environment;

            // Validar que sea de tipo FunctionStatement
            if (funObj[0].ToString() != "FunctionStatement")
                throw new RuntimeException("Not a function.");

            List<object> arguments = node[2] as List<object>;
            List<object> functionParameters = funObj[2] as List<object>;

            Environment newEnv = new Environment(new Dictionary<string, object>(), funEnv);
            for (int i = 0; i < arguments.Count; i++)
            {
                var evaluated = Eval(arguments[i] as object[], env);
                newEnv.Define((functionParameters[i] as object[])[1].ToString().ToLower(), evaluated);
            }

            return Eval(funObj[3] as object[], newEnv);
        }

        private static bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() == typeof(bool)) return (bool)obj;
            return true;
        }
    }
}

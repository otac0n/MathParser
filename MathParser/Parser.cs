namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using Pegasus.Common;

    public partial class Parser
    {
        private static Expression ConvertIfLower(Expression expression, Expression to)
        {
            return ConvertIfLower(expression, to: to.Type);
        }

        private static Expression ConvertIfLower(Expression expression, Type to)
        {
            var from = expression.Type;
            if (to == typeof(Complex) && from != typeof(Complex))
            {
                return Expression.Convert(expression, to);
            }

            return expression;
        }

        private static Expression LowerToReal(Expression expression)
        {
            var from = expression.Type;
            if (from == typeof(Complex))
            {
                return Expression.MakeMemberAccess(expression, typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
            }

            return expression;
        }

        private static Expression Abs(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Abs), new[] { expression.Type }) ?? typeof(Complex).GetMethod(nameof(Complex.Abs), new[] { expression.Type }), expression);
        }

        private static Expression Ceiling(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Ceiling), new[] { expression.Type }), expression);
        }

        private static Expression Floor(Expression expression)
        {
            return Expression.Call(typeof(Math).GetMethod(nameof(Math.Floor), new[] { expression.Type }), expression);
        }

        private static Expression Pow(Expression @base, Expression exponent)
        {
            if (@base.Type == typeof(double) && exponent.Type == typeof(double))
            {
                return Expression.Power(@base, exponent);
            }

            @base = ConvertIfLower(@base, to: typeof(Complex));
            return Expression.Call(typeof(Complex).GetMethod(nameof(Complex.Pow), new[] { @base.Type, exponent.Type }), @base, exponent);
        }

        private static Expression Sqrt(Expression @base)
        {
            @base = ConvertIfLower(@base, to: typeof(Complex));
            return Expression.Call(typeof(Complex).GetMethod(nameof(Complex.Sqrt), new[] { @base.Type }), @base);
        }

        private static Expression Function(string name, IList<Expression> arguments)
        {
            if (arguments.Count == 1)
            {
                if (name.Equals("Re", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Expression.Property(ConvertIfLower(arguments[0], to: typeof(Complex)), typeof(Complex).GetProperty(nameof(Complex.Real), BindingFlags.Public | BindingFlags.Instance));
                }

                if (name.Equals("Im", StringComparison.CurrentCultureIgnoreCase))
                {
                    return Expression.Property(ConvertIfLower(arguments[0], to: typeof(Complex)), typeof(Complex).GetProperty(nameof(Complex.Imaginary), BindingFlags.Public | BindingFlags.Instance));
                }
            }

            Expression[] mappedArguments;
            var found = FindFunction(typeof(Complex), name, arguments.Select(a => a.Type).ToArray());
            if (found == null)
            {
                found = FindFunction(typeof(Complex), name, arguments.Select(_ => typeof(Complex)).ToArray());
                if (found == null)
                {
                    throw new MissingMethodException(typeof(Complex).FullName, name + "(" + string.Join(", ", arguments.Select(a => a.Type.FullName)) + ")");
                }
                else
                {
                    mappedArguments = arguments.Select(a => ConvertIfLower(a, to: typeof(Complex))).ToArray();
                }
            }
            else
            {
                mappedArguments = arguments.ToArray();
            }

            return Expression.Call(found, mappedArguments);
        }

        private static MethodInfo FindFunction(Type type, string name, Type[] argTypes)
        {
            return (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    where m.DeclaringType == type
                    where m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                    let parameters = m.GetParameters()
                    where parameters.Length == argTypes.Length
                    where Enumerable.Range(0, argTypes.Length).All(i => parameters[i].ParameterType.IsAssignableFrom(argTypes[i]))
                    select m).FirstOrDefault();
        }

        private static void CreateVariable(Cursor state, string variable)
        {
            var existing = state[variable] as Expression;
            if (existing == null)
            {
                state[variable] = Expression.Parameter(typeof(Complex), variable);
            }
        }
    }
}

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using static Operations;
    using WKF = WellKnownFunctions;

    internal class Scope
    {
        public static Expression Bind(string name, params Expression[] arguments) => Bind(name, (IList<Expression>)arguments);

        public static Expression Bind(string name, IList<Expression> arguments)
        {
            if (!DefaultScope.NamedFunctions.TryGetValue(name, out var function))
            {
                throw new MissingMethodException($"Could not find a binding for '{name}'.");
            }

            return Bind(function, arguments);
        }

        public static Expression Bind(KnownFunction function, params Expression[] arguments) => Bind(function, (IList<Expression>)arguments);

        public static Expression Bind(KnownFunction function, IList<Expression> arguments)
        {
            if (function == WKF.Exponential.Sqrt && arguments.Count == 1)
            {
                var @base = arguments[0];
                if (!TryConvert(@base, false, (double value) => value >= 0))
                {
                    arguments = [ConvertIfLower(@base, to: typeof(Complex))];
                }
            }

            var match = (from known in DefaultScope.KnownMethods
                         where known.Value == function
                         let m = known.Key
                         let mp = m.Parameters
                         where mp.Count == arguments.Count
                         let parameterMatches = (from i in Enumerable.Range(0, arguments.Count)
                                                 select new
                                                 {
                                                     ParameterType = mp[i].Type,
                                                     Assignable = mp[i].Type.IsAssignableFrom(arguments[i].Type),
                                                     Convertible = mp[i].Type == typeof(Complex), // TODO: This should probably look for registered converters.
                                                 }).ToList()
                         where parameterMatches.All(p => p.Assignable || p.Convertible)
                         orderby parameterMatches.Count(p => !p.Assignable) ascending,
                                 m.Body is MethodCallExpression ascending
                         select (m, parameterMatches)).First();

            var (lambda, parameters) = match;

            return new ReplaceVisitor(
                Enumerable.Range(0, arguments.Count)
                    .ToDictionary(
                        i => (Expression)lambda.Parameters[i],
                        i => parameters[i].Assignable ? arguments[i] : ConvertIfLower(arguments[i], to: parameters[i].ParameterType)))
                .Visit(lambda.Body);
        }

        public static void Bind(MethodCallExpression methodCall, out KnownFunction knownMethod, out IList<Expression>? arguments)
        {
            if (!TryBind(methodCall, out knownMethod, out arguments))
            {
                throw new MissingMethodException($"Could not find a binding for '{methodCall.Method}'.");
            }
        }

        public static bool TryBind(MethodCallExpression methodCall, [NotNullWhen(true)] out KnownFunction? knownMethod, [NotNullWhen(true)] out IList<Expression>? arguments)
        {
            var method = methodCall.Method;
            if (TryBind(method, out knownMethod))
            {
                arguments = method.IsStatic ? methodCall.Arguments : [methodCall.Object!, .. methodCall.Arguments];
                return true;
            }

            arguments = null;
            return false;
        }

        public static void Bind(MethodInfo method, out KnownFunction knownMethod)
        {
            if (!TryBind(method, out knownMethod))
            {
                throw new MissingMethodException($"Could not find a binding for '{method}'.");
            }
        }

        public static bool TryBind(MethodInfo method, [NotNullWhen(true)] out KnownFunction? knownMethod)
        {
            knownMethod = (from known in DefaultScope.KnownMethods
                           where known.Key.Body is MethodCallExpression methodCall && methodCall.Method == method
                           select known.Value).Distinct().SingleOrDefault();

            return knownMethod != null;
        }

        public static string BindName(KnownFunction function)
        {
            if (!TryBindName(function, out var name))
            {
                throw new MissingMethodException($"Could not find a binding for '{function.Name}'.");
            }

            return name;
        }

        public static bool TryBindName(KnownFunction function, [NotNullWhen(true)] out string? name)
        {
            var names = from binding in DefaultScope.NamedFunctions
                        where binding.Value == function
                        orderby binding.Key.Length descending
                        select binding.Key;
            using var enumerator = names.GetEnumerator();
            if (enumerator.MoveNext())
            {
                name = enumerator.Current;
                return true;
            }

            name = null;
            return false;
        }
    }
}

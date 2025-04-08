// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;

    public partial class Scope
    {
        /// <summary>
        /// Binds some <see cref="KnownConstant"/> to the specified name.
        /// </summary>
        /// <param name="name">The name of the <see cref="KnownConstant"/>.</param>
        /// <returns>The bound <see cref="KnownConstant"/> as an <see cref="Expression"/>.</returns>
        /// <exception cref="MissingFieldException">Thrown when a constant could not be bound.</exception>
        public Expression BindConstant(string name) =>
            this.TryBindConstant(name, out var expression)
                ? expression
                : throw new MissingFieldException($"Could not find a binding for '{name}'.");

        /// <summary>
        /// Binds an <see cref="Expression"/> to the specified <see cref="KnownConstant"/>.
        /// </summary>
        /// <param name="constant">The <see cref="KnownConstant"/> to bind.</param>
        /// <returns>The bound <see cref="Expression"/>.</returns>
        /// <exception cref="MissingFieldException">Thrown when an expression could not be bound.</exception>
        public Expression BindConstant(KnownConstant constant) =>
            this.TryBindConstant(constant, out var expression)
                ? expression
                : throw new MissingFieldException($"Could not find a binding for {constant}.");

        /// <summary>
        /// Binds a <see cref="KnownConstant"/> to the specified <see cref="Expression"/>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> to bind.</param>
        /// <returns>The bound <see cref="KnownConstant"/>.</returns>
        /// <exception cref="MissingFieldException">Thrown when a constant could not be bound.</exception>
        public KnownConstant BindConstant(Expression expression) =>
            this.TryBindConstant(expression, out var knownConstant)
                ? knownConstant
                : throw new MissingFieldException($"Could not find a binding for {expression}.");

        /// <summary>
        /// Binds some <see cref="KnownFunction"/> to the specified name, given some arguments.
        /// </summary>
        /// <param name="name">The name of the <see cref="KnownFunction"/>.</param>
        /// <param name="arguments">The input arguments.</param>
        /// <returns>The bound <see cref="KnownFunction"/> as an <see cref="Expression"/>.</returns>
        /// <exception cref="MissingMethodException">Thown when a function could not be bound.</exception>
        public Expression BindFunction(string name, params Expression[] arguments) =>
            this.BindFunction(name, (IList<Expression>)arguments);

        /// <summary>
        /// Binds some <see cref="KnownFunction"/> to the specified name, given some arguments.
        /// </summary>
        /// <param name="name">The name of the <see cref="KnownFunction"/>.</param>
        /// <param name="arguments">The input arguments.</param>
        /// <returns>The bound <see cref="KnownFunction"/> as an <see cref="Expression"/>.</returns>
        /// <exception cref="MissingMethodException">Thown when a function could not be bound.</exception>
        public Expression BindFunction(string name, IList<Expression> arguments) =>
            this.NamedObjects.TryGetValue(name, out var knownObject) && knownObject is KnownFunction function
                ? this.BindFunction(function, arguments)
                : throw new MissingMethodException($"Could not find a binding for '{name}'.");

        /// <summary>
        /// Binds an <see cref="Expression"/> the the specified <see cref="KnownFunction"/> and arguments.
        /// </summary>
        /// <param name="function">The <see cref="KnownFunction"/> to bind.</param>
        /// <param name="arguments">The input arguments.</param>
        /// <returns>The bound <see cref="Expression"/>.</returns>
        /// <exception cref="MissingMethodException">Thown when an expression could not be bound.</exception>
        public Expression BindFunction(KnownFunction function, params Expression[] arguments) =>
            this.BindFunction(function, (IList<Expression>)arguments);

        /// <summary>
        /// Binds an <see cref="Expression"/> the the specified <see cref="KnownFunction"/> and arguments.
        /// </summary>
        /// <param name="function">The <see cref="KnownFunction"/> to bind.</param>
        /// <param name="arguments">The input arguments.</param>
        /// <returns>The bound <see cref="Expression"/>.</returns>
        /// <exception cref="MissingMethodException">Thown when an expression could not be bound.</exception>
        public Expression BindFunction(KnownFunction function, IList<Expression> arguments) =>
            this.TryBindFunction(function, arguments, out var expression)
                ? expression
                : throw new MissingMethodException($"Could not find a binding for {function}.");

        /// <summary>
        /// Binds a <see cref="KnownFunction"/> and its arguments to the specified <see cref="Expression"/>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> to bind.</param>
        /// <returns>The bound <see cref="KnownFunction"/> and its arguments.</returns>
        /// <exception cref="MissingMethodException">Thrown when a function could not be bound.</exception>
        public (KnownFunction Function, IList<Expression> Arguments) BindFunction(Expression expression) =>
            this.TryBindFunction(expression, out var knownMethod, out var arguments)
                ? (knownMethod, arguments)
                : throw new MissingMethodException($"Could not find a binding for {expression}.");

        /// <summary>
        /// Binds some name to the specified <see cref="IKnownObject"/>.
        /// </summary>
        /// <param name="knownObject">A <see cref="KnownConstant"/> or a <see cref="KnownFunction"/> to bind.</param>
        /// <returns>The bound name.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when a name could not be bound.</exception>
        public string BindName(IKnownObject knownObject) =>
            this.TryBindName(knownObject, out var name)
                ? name
                : throw new KeyNotFoundException($"Could not find a binding for {knownObject}.");

        public bool TryBindConstant(string name, [NotNullWhen(true)] out Expression? expression)
        {
            if (this.NamedObjects.TryGetValue(name, out var knownObject) && knownObject is KnownConstant constant && this.TryBindConstant(constant, out expression))
            {
                return true;
            }

            expression = null;
            return false;
        }

        public bool TryBindConstant(KnownConstant constant, [NotNullWhen(true)] out Expression? expression)
        {
            var constants = from known in this.KnownConstants
                            where known.Value == constant
                            let m = known.Key
                            orderby m is ConstantExpression ascending,
                                    m is MethodCallExpression ascending
                            select m;

            using var enumerator = constants.GetEnumerator();
            if (enumerator.MoveNext())
            {
                expression = enumerator.Current;
                return true;
            }

            expression = null;
            return false;
        }

        public bool TryBindFunction(KnownFunction function, Expression[] arguments, [NotNullWhen(true)] out Expression? expression) =>
            this.TryBindFunction(function, (IList<Expression>)arguments, out expression);

        public bool TryBindFunction(KnownFunction function, IList<Expression> arguments, [NotNullWhen(true)] out Expression? expression)
        {
            // TODO: This is a hack to avoid Math.Sqrt(-1) preferring Complext.Sqrt(-1)
            if (function == WellKnownFunctions.Exponential.Sqrt && arguments.Count == 1)
            {
                var @base = arguments[0];
                if (!this.TryConvert(@base, false, (double value) => value >= 0))
                {
                    arguments = [this.ConvertIfLower(@base, to: typeof(Complex))];
                }
            }

            var matches = from known in this.KnownMethods
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
                          select (m, parameterMatches);

            using var enumerator = matches.GetEnumerator();
            if (enumerator.MoveNext())
            {
                var (lambda, parameters) = enumerator.Current;

                expression = new ReplaceVisitor(
                    Enumerable.Range(0, arguments.Count)
                        .ToDictionary(
                            i => (Expression)lambda.Parameters[i],
                            i => parameters[i].Assignable ? arguments[i] : this.ConvertIfLower(arguments[i], to: parameters[i].ParameterType)))
                    .Visit(lambda.Body);
                return true;
            }

            expression = null;
            return false;
        }

        public bool TryBindConstant([NotNullWhen(true)] Expression? expression, [NotNullWhen(true)] out KnownConstant? knownConstant)
        {
            if (expression != null)
            {
                var visitor = new MatchVisitor(expression);

                var methods = (from known in this.KnownConstants
                               let match = visitor.PatternMatch(known.Key)
                               where match.Success
                               where match.Arguments.Count == 0
                               select known.Value).Distinct();

                using var enumerator = methods.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    knownConstant = enumerator.Current;
                    return true;
                }
            }

            knownConstant = null;
            return false;
        }

        public bool TryBindFunction([NotNullWhen(true)] Expression? expression, [NotNullWhen(true)] out KnownFunction? knownMethod, [NotNullWhen(true)] out IList<Expression>? arguments)
        {
            if (expression != null)
            {
                var visitor = new MatchVisitor(expression);

                var methods = (from known in this.KnownMethods
                               let match = visitor.PatternMatch(known.Key)
                               where match.Success
                               where match.Arguments.All(p => p != null)
                               select (known.Value, match.Arguments)).Distinct();

                using var enumerator = methods.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    (knownMethod, arguments) = enumerator.Current;
                    return true;
                }
            }

            (knownMethod, arguments) = (null, null);
            return false;
        }

        public bool TryBindName(IKnownObject knownObject, [NotNullWhen(true)] out string? name)
        {
            var names = from binding in this.NamedObjects
                        where binding.Value == knownObject
                        orderby binding.Key.Length ascending
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

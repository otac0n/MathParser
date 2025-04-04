﻿namespace MathParser
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using System.Threading;
    using WKF = WellKnownFunctions;

    /// <summary>
    /// Provides a collection of <see cref="KnownFunction">known functions</see> that can be bound to <see cref="Expression">expressions</see>.
    /// </summary>
    public sealed class Scope : IEnumerable<Scope>
    {
        private readonly Lock syncRoot = new();
        private IDictionary<string, KnownFunction> namedFunctions;
        private IDictionary<LambdaExpression, KnownFunction> knownMethods;
        private bool frozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        public Scope()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="knownMethods">An existiong collection of expression bindings.</param>
        /// <param name="namedFunctions">An existing collection of name bindings.</param>
        public Scope(IDictionary<LambdaExpression, KnownFunction>? knownMethods = null, IDictionary<string, KnownFunction>? namedFunctions = null)
        {
            this.KnownMethods = knownMethods;
            this.NamedFunctions = namedFunctions;
        }

        /// <summary>
        /// Gets or sets the collection of expression bindings.
        /// </summary>
        public IDictionary<LambdaExpression, KnownFunction> KnownMethods
        {
            get => this.knownMethods;
            set
            {
                lock (this.syncRoot)
                {
                    if (this.frozen)
                    {
                        throw new NotSupportedException();
                    }

                    this.knownMethods = value ?? new Dictionary<LambdaExpression, KnownFunction>();
                }
            }
        }

        /// <summary>
        /// Gets or sets the collection of name bindings.
        /// </summary>
        public IDictionary<string, KnownFunction> NamedFunctions
        {
            get => this.namedFunctions;
            set
            {
                lock (this.syncRoot)
                {
                    if (this.frozen)
                    {
                        throw new NotSupportedException();
                    }

                    this.namedFunctions = value ?? new Dictionary<string, KnownFunction>();
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerator<Scope> GetEnumerator()
        {
            yield return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Freezes the scope and returns it.
        /// </summary>
        /// <returns>The scope object.</returns>
        public Scope Freeze()
        {
            lock (this.syncRoot)
            {
                if (!this.frozen)
                {
                    this.knownMethods = this.knownMethods.AsReadOnly();
                    this.namedFunctions = this.namedFunctions.AsReadOnly();
                    this.frozen = true;
                }
            }

            return this;
        }

        /// <summary>
        /// Adds all known operators as well as generic math interfaces for the given type.
        /// </summary>
        /// <param name="numberType">The type to search for <see cref="WellKnownFunctions"/>.</param>
        public void Add(Type numberType) =>
            WellKnownFunctionMapping.Add(this.KnownMethods, numberType);

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        public void Add<TIn, TOut>(Expression<Func<TIn, TOut>> expression, KnownFunction value) =>
            this.KnownMethods.Add(expression, value);

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        public void Add<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> expression, KnownFunction value) =>
            this.KnownMethods.Add(expression, value);

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        public void Add(LambdaExpression expression, KnownFunction value) =>
            this.KnownMethods.Add(expression, value);

        /// <summary>
        /// Adds a <see cref="KnownFunction"/> to the named function list under its default name.
        /// </summary>
        /// <param name="knownFunction">The <see cref="KnownFunction"/> to add.</param>
        public void Add(KnownFunction knownFunction) =>
            this.NamedFunctions.Add(knownFunction.Name, knownFunction);

        /// <summary>
        /// Adds a <see cref="KnownFunction"/> to the named function list under an assoicated name.
        /// </summary>
        /// <param name="name">The associated name.</param>
        /// <param name="knownFunction">The <see cref="KnownFunction"/> to add.</param>
        public void Add(string name, KnownFunction knownFunction) =>
            this.NamedFunctions.Add(name, knownFunction);

        public Expression Bind(string name, params Expression[] arguments) => this.Bind(name, (IList<Expression>)arguments);

        public Expression Bind(string name, IList<Expression> arguments)
        {
            if (!this.NamedFunctions.TryGetValue(name, out var function))
            {
                throw new MissingMethodException($"Could not find a binding for '{name}'.");
            }

            return this.Bind(function, arguments);
        }

        public Expression Bind(KnownFunction function, params Expression[] arguments) => this.Bind(function, (IList<Expression>)arguments);

        public Expression Bind(KnownFunction function, IList<Expression> arguments)
        {
            // TODO: This is a hack to avoid Math.Sqrt(-1) preferring Complext.Sqrt(-1)
            if (function == WKF.Exponential.Sqrt && arguments.Count == 1)
            {
                var @base = arguments[0];
                if (!this.TryConvert(@base, false, (double value) => value >= 0))
                {
                    arguments = [this.ConvertIfLower(@base, to: typeof(Complex))];
                }
            }

            var match = (from known in this.KnownMethods
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
                        i => parameters[i].Assignable ? arguments[i] : this.ConvertIfLower(arguments[i], to: parameters[i].ParameterType)))
                .Visit(lambda.Body);
        }

        public void Bind(Expression expression, out KnownFunction knownMethod, out IList<Expression>? arguments)
        {
            if (!this.TryBind(expression, out knownMethod, out arguments))
            {
                throw new MissingMethodException($"Could not find a binding for '{expression}'.");
            }
        }

        public bool TryBind([NotNullWhen(true)] Expression? expression, [NotNullWhen(true)] out KnownFunction? knownMethod, [NotNullWhen(true)] out IList<Expression>? arguments)
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

        public string BindName(KnownFunction function)
        {
            if (!this.TryBindName(function, out var name))
            {
                throw new MissingMethodException($"Could not find a binding for '{function.Name}'.");
            }

            return name;
        }

        public bool TryBindName(KnownFunction function, [NotNullWhen(true)] out string? name)
        {
            var names = from binding in this.NamedFunctions
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

        internal static LambdaExpression MakeLambda(Type[] parameters, Func<ParameterExpression[], Expression> builder)
        {
            var parameterExpressions = Array.ConvertAll(parameters, Expression.Parameter);
            return Expression.Lambda(builder(parameterExpressions), parameterExpressions);
        }

        private static class WellKnownFunctionMapping
        {
            /// <summary>
            /// <see href="https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/classes.md#153106-method-names-reserved-for-operators"/> §15.3.10.6 Method names reserved for operators.
            /// <seealso href="https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/WellKnownMemberNames.cs">
            /// Roslyn definition of reserved operator method names, including op_Exponent.
            /// </seealso>
            /// </summary>
            public static readonly Dictionary<string, ExpressionType> OperatorMethods = new()
            {
                ["op_UnaryNegation"] = ExpressionType.Negate,
                ["op_UnaryPlus"] = ExpressionType.UnaryPlus,
                ["op_Addition"] = ExpressionType.Add,
                ["op_Subtraction"] = ExpressionType.Subtract,
                ["op_Multiply"] = ExpressionType.Multiply,
                ["op_Division"] = ExpressionType.Divide,
                ["op_Modulus"] = ExpressionType.Modulo,
                ["op_Exponent"] = ExpressionType.Power, // VB
                ["op_Exponentiation"] = ExpressionType.Power, // F#
                ["op_CheckedAddition"] = ExpressionType.AddChecked,
                ["op_CheckedSubtraction"] = ExpressionType.SubtractChecked,
                ["op_CheckedMultiply"] = ExpressionType.MultiplyChecked,
                ["op_CheckedDivision"] = ExpressionType.Divide, // DivideChecked not available.
                ["op_OnesComplement"] = ExpressionType.OnesComplement,
                ["op_LogicalNot"] = ExpressionType.Not,
                ["op_BitwiseAnd"] = ExpressionType.And,
                ["op_BitwiseOr"] = ExpressionType.Or,
                ["op_ExclusiveOr"] = ExpressionType.ExclusiveOr,
                ["op_Equality"] = ExpressionType.Equal,
                ["op_Inequality"] = ExpressionType.NotEqual,
                ["op_GreaterThan"] = ExpressionType.GreaterThan,
                ["op_GreaterThanOrEqual"] = ExpressionType.GreaterThanOrEqual,
                ["op_LessThan"] = ExpressionType.LessThan,
                ["op_LessThanOrEqual"] = ExpressionType.LessThanOrEqual,
                ["op_LeftShift"] = ExpressionType.LeftShift,
                ["op_RightShift"] = ExpressionType.RightShift,
            };

            private static readonly Dictionary<ExpressionType, KnownFunction> Operators = new()
            {
                [ExpressionType.Negate] = WKF.Arithmetic.Negate,
                [ExpressionType.Add] = WKF.Arithmetic.Add,
                [ExpressionType.AddChecked] = WKF.Arithmetic.Add,
                [ExpressionType.Subtract] = WKF.Arithmetic.Subtract,
                [ExpressionType.SubtractChecked] = WKF.Arithmetic.Subtract,
                [ExpressionType.Multiply] = WKF.Arithmetic.Multiply,
                [ExpressionType.MultiplyChecked] = WKF.Arithmetic.Multiply,
                [ExpressionType.Divide] = WKF.Arithmetic.Divide, // DivideChecked not available.
                [ExpressionType.Power] = WKF.Exponential.Pow,
                [ExpressionType.Equal] = WKF.Comparison.Equal,
                [ExpressionType.NotEqual] = WKF.Comparison.NotEqual,
                [ExpressionType.GreaterThan] = WKF.Comparison.GreaterThan,
                [ExpressionType.GreaterThanOrEqual] = WKF.Comparison.GreaterThanOrEqual,
                [ExpressionType.LessThan] = WKF.Comparison.LessThan,
                [ExpressionType.LessThanOrEqual] = WKF.Comparison.LessThanOrEqual,
            };

            public static void Add(IDictionary<LambdaExpression, KnownFunction> t, Type numberType)
            {
                var methods = numberType.GetMethods(BindingFlags.Public | BindingFlags.Static).ToLookup(m => m.Name);
                var matches = from methodGroup in methods
                              where OperatorMethods.ContainsKey(methodGroup.Key)
                              let expressionType = OperatorMethods[methodGroup.Key]
                              where Operators.ContainsKey(expressionType)
                              let function = Operators[expressionType]
                              from method in methodGroup
                              let parameters = method.GetParameters()
                              let parameterTypes = Array.ConvertAll(parameters, p => p.ParameterType)
                              select (expression: MakeLambda(parameterTypes, p => p.Length == 1 ? Expression.MakeUnary(expressionType, p[0], null!) : Expression.MakeBinary(expressionType, p[0], p[1])), function);

                foreach (var (expression, function) in matches)
                {
                    t.Add(expression, function);
                }

                var interfaces = numberType.GetInterfaces();
                foreach (var type in interfaces)
                {
                    var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    var typeArgs = type.IsGenericType ? type.GetGenericArguments() : [];

                    if (definition == typeof(IEqualityOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.Equal(p[0], p[1])), WKF.Comparison.Equal);
                        t.Add(MakeLambda(argTypes, p => Expression.NotEqual(p[0], p[1])), WKF.Comparison.NotEqual);
                    }
                    else if (definition == typeof(IComparisonOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.GreaterThan(p[0], p[1])), WKF.Comparison.GreaterThan);
                        t.Add(MakeLambda(argTypes, p => Expression.GreaterThanOrEqual(p[0], p[1])), WKF.Comparison.GreaterThanOrEqual);
                        t.Add(MakeLambda(argTypes, p => Expression.LessThan(p[0], p[1])), WKF.Comparison.LessThan);
                        t.Add(MakeLambda(argTypes, p => Expression.LessThanOrEqual(p[0], p[1])), WKF.Comparison.LessThanOrEqual);
                    }
                    ////else if (definition == typeof(IUnaryPlusOperators<,>))
                    ////{
                    ////    var argTypes = new[] { typeArgs[0] };
                    ////    t.Add(MakeLambda(argTypes, p => Expression.UnaryPlus(p[0])), WellKnownFunctions.Identity);
                    ////}
                    else if (definition == typeof(IUnaryNegationOperators<,>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        t.Add(MakeLambda(argTypes, p => Expression.Negate(p[0])), WKF.Arithmetic.Negate);
                        t.Add(MakeLambda(argTypes, p => Expression.NegateChecked(p[0])), WKF.Arithmetic.Negate);
                    }
                    else if (definition == typeof(IAdditionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.Add(p[0], p[1])), WKF.Arithmetic.Add);
                        t.Add(MakeLambda(argTypes, p => Expression.AddChecked(p[0], p[1])), WKF.Arithmetic.Add);
                    }
                    else if (definition == typeof(ISubtractionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.Subtract(p[0], p[1])), WKF.Arithmetic.Subtract);
                        t.Add(MakeLambda(argTypes, p => Expression.SubtractChecked(p[0], p[1])), WKF.Arithmetic.Subtract);
                    }
                    else if (definition == typeof(IMultiplyOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.Multiply(p[0], p[1])), WKF.Arithmetic.Multiply);
                        t.Add(MakeLambda(argTypes, p => Expression.MultiplyChecked(p[0], p[1])), WKF.Arithmetic.Multiply);
                    }
                    else if (definition == typeof(IDivisionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        t.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1])), WKF.Arithmetic.Divide);
                        var opDivideChecked = type.GetMethod("op_CheckedDivision", argTypes);
                        t.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1], opDivideChecked)), WKF.Arithmetic.Divide);
                    }
                    else if (definition == typeof(IPowerFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0], typeArgs[0] };
                        var pow = type.GetMethod(nameof(IPowerFunctions<>.Pow), argTypes);
                        t.Add(MakeLambda(argTypes, p => Expression.Call(pow, p[0], p[1])), WKF.Exponential.Pow);
                    }
                    else if (definition == typeof(IRootFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var sqrt = type.GetMethod(nameof(IRootFunctions<>.Sqrt), argTypes);
                        t.Add(MakeLambda(argTypes, p => Expression.Call(sqrt, p[0])), WKF.Exponential.Sqrt);
                    }
                    else if (definition == typeof(ILogarithmicFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var log = type.GetMethod(nameof(ILogarithmicFunctions<>.Log), argTypes);
                        t.Add(MakeLambda(argTypes, p => Expression.Call(log, p[0])), WKF.Exponential.Ln);
                    }
                    else if (definition == typeof(IExponentialFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var exp = type.GetMethod(nameof(IExponentialFunctions<>.Exp), argTypes);
                        t.Add(MakeLambda(argTypes, p => Expression.Call(exp, p[0])), WKF.Exponential.Exp);
                    }
                }
            }
        }
    }
}

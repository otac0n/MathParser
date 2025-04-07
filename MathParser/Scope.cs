namespace MathParser
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Reflection;
    using System.Threading;
    using WKC = WellKnownConstants;
    using WKF = WellKnownFunctions;

    /// <summary>
    /// Provides a collection of <see cref="KnownFunction">known functions</see> that can be bound to <see cref="Expression">expressions</see>.
    /// </summary>
    public sealed partial class Scope : IEnumerable<Scope>
    {
        private readonly Lock syncRoot = new();
        private IDictionary<string, IKnownObject> namedObjects;
        private IDictionary<Expression, KnownConstant> knownConstants;
        private IDictionary<LambdaExpression, KnownFunction> knownMethods;
        private IDictionary<(Type from, Type to), bool> typeConversions;
        private bool frozen;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        public Scope(StringComparer nameComparer)
        {
            this.knownConstants = new Dictionary<Expression, KnownConstant>();
            this.knownMethods = new Dictionary<LambdaExpression, KnownFunction>();
            this.namedObjects = new Dictionary<string, IKnownObject>(nameComparer);
            this.typeConversions = new Dictionary<(Type, Type), bool>();
            this.KnownConstants = this.knownConstants.AsReadOnly();
            this.KnownMethods = this.knownMethods.AsReadOnly();
            this.NamedObjects = this.namedObjects.AsReadOnly();
            this.TypeConversions = this.typeConversions.AsReadOnly();
        }

        /// <summary>
        /// Gets or sets the collection of constant expression bindings.
        /// </summary>
        public IDictionary<Expression, KnownConstant> KnownConstants { get; }

        /// <summary>
        /// Gets or sets the collection of function expression bindings.
        /// </summary>
        public IDictionary<LambdaExpression, KnownFunction> KnownMethods { get; }

        /// <summary>
        /// Gets or sets the collection of name bindings.
        /// </summary>
        public IDictionary<string, IKnownObject> NamedObjects { get; }

        /// <summary>
        /// Gets or sets the collection of supported conversions.
        /// </summary>
        public IDictionary<(Type from, Type to), bool> TypeConversions { get; }

        /// <inheritdoc/>
        public IEnumerator<Scope> GetEnumerator()
        {
            yield return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public Type? FindLargest(Type a, Type b)
        {
            if (a == b)
            {
                return a;
            }

            var aToB = this.IsImplicitlyConvetibleTo(a, to: b);
            var bToA = this.IsImplicitlyConvetibleTo(b, to: a);
            return aToB ? bToA ? a : b : bToA ? a : null;
        }

        public bool IsImplicitlyConvetibleTo(Type from, Type to)
        {
            if (from.IsAssignableTo(to))
            {
                return true;
            }

            return this.TypeConversions.TryGetValue((from, to), out var @implicit) && @implicit;
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

            public static readonly Dictionary<string, bool> Conversions = new()
            {
                ["op_Implicit"] = true,
                ["op_Explicit"] = false,
            };

            /// <summary>
            /// <see href="https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/conversions.md#1023-implicit-numeric-conversions"/> §10.2.3 Implicit numeric conversions.
            /// </summary>
            public static readonly (Type from, Type to)[] ImplicitPrimitiveConversions =
            [
                (typeof(sbyte), typeof(short)),
                (typeof(sbyte), typeof(int)),
                (typeof(sbyte), typeof(long)),
                (typeof(sbyte), typeof(float)),
                (typeof(sbyte), typeof(double)),
                (typeof(sbyte), typeof(decimal)),
                (typeof(byte), typeof(short)),
                (typeof(byte), typeof(ushort)),
                (typeof(byte), typeof(int)),
                (typeof(byte), typeof(uint)),
                (typeof(byte), typeof(long)),
                (typeof(byte), typeof(ulong)),
                (typeof(byte), typeof(float)),
                (typeof(byte), typeof(double)),
                (typeof(byte), typeof(decimal)),
                (typeof(short), typeof(int)),
                (typeof(short), typeof(long)),
                (typeof(short), typeof(float)),
                (typeof(short), typeof(double)),
                (typeof(short), typeof(decimal)),
                (typeof(ushort), typeof(int)),
                (typeof(ushort), typeof(uint)),
                (typeof(ushort), typeof(long)),
                (typeof(ushort), typeof(ulong)),
                (typeof(ushort), typeof(float)),
                (typeof(ushort), typeof(double)),
                (typeof(ushort), typeof(decimal)),
                (typeof(int), typeof(long)),
                (typeof(int), typeof(float)),
                (typeof(int), typeof(double)),
                (typeof(int), typeof(decimal)),
                (typeof(uint), typeof(long)),
                (typeof(uint), typeof(ulong)),
                (typeof(uint), typeof(float)),
                (typeof(uint), typeof(double)),
                (typeof(uint), typeof(decimal)),
                (typeof(long), typeof(float)),
                (typeof(long), typeof(double)),
                (typeof(long), typeof(decimal)),
                (typeof(ulong), typeof(float)),
                (typeof(ulong), typeof(double)),
                (typeof(ulong), typeof(decimal)),
                (typeof(char), typeof(ushort)),
                (typeof(char), typeof(int)),
                (typeof(char), typeof(uint)),
                (typeof(char), typeof(long)),
                (typeof(char), typeof(ulong)),
                (typeof(char), typeof(float)),
                (typeof(char), typeof(double)),
                (typeof(char), typeof(decimal)),
                (typeof(float), typeof(double))
            ];

            private static readonly ILookup<Type, (Type from, Type to)> ImplicitPrimitiveLookup =
                Enumerable.Union(
                    ImplicitPrimitiveConversions.Select(c => (key: c.from, conversion: c)),
                    ImplicitPrimitiveConversions.Select(c => (key: c.to, conversion: c)))
                .ToLookup(p => p.key, p => p.conversion);

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

            public static void Add(Type numberType, IDictionary<Expression, KnownConstant> c, IDictionary<LambdaExpression, KnownFunction> f, IDictionary<(Type from, Type to), bool> t)
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
                    f.Add(expression, function);
                }

                var converts = from k in Conversions
                               from m in methods[k.Key]
                               let parameters = m.GetParameters()
                               where parameters.Length == 1
                               select ((parameters[0].ParameterType, m.ReturnType), k.Value);
                var primitive = ImplicitPrimitiveLookup[numberType].Select(n => (n, true));
                var @explicit = from groups in ImplicitPrimitiveLookup
                                let other = groups.Key
                                where other != numberType
                                from p in new[] { (numberType, other), (other, numberType) }
                                select (p, false);

                foreach (var (conversion, @implicit) in converts.Concat(@explicit).Concat(primitive))
                {
                    t[conversion] = (t.TryGetValue(conversion, out var existing) && existing) || @implicit;
                }

                void AddConstant(PropertyInfo? property, KnownConstant constant)
                {
                    if (property != null)
                    {
                        c.Add(Expression.MakeMemberAccess(null, property), constant);

                        var map = numberType.GetInterfaceMap(property.DeclaringType);
                        var value = map.TargetMethods[Array.IndexOf(map.InterfaceMethods, property.GetMethod)].Invoke(null, []);
                        c.Add(Expression.Constant(value), constant);
                    }
                }

                var interfaces = numberType.GetInterfaces();
                foreach (var type in interfaces)
                {
                    var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    var typeArgs = type.IsGenericType ? type.GetGenericArguments() : [];

                    if (definition == typeof(INumberBase<>))
                    {
                        AddConstant(type.GetProperty(nameof(INumberBase<>.Zero)), WKC.Zero);
                        AddConstant(type.GetProperty(nameof(INumberBase<>.One)), WKC.One);
                    }
                    ////else if (definition == typeof(INumber<>))
                    ////{
                    ////    /* Clamp, Min, Max, Sign */
                    ////}
                    else if (definition == typeof(IFloatingPointConstants<>))
                    {
                        AddConstant(type.GetProperty(nameof(IFloatingPointConstants<>.E)), WKC.EulersNumber);
                        AddConstant(type.GetProperty(nameof(IFloatingPointConstants<>.Pi)), WKC.Pi);
                        AddConstant(type.GetProperty(nameof(IFloatingPointConstants<>.Tau)), WKC.Tau);
                    }
                    else if (definition == typeof(IFloatingPointIeee754<>))
                    {
                        AddConstant(type.GetProperty(nameof(IFloatingPointIeee754<>.NaN)), WKC.Indeterminate);
                        AddConstant(type.GetProperty(nameof(IFloatingPointIeee754<>.NegativeInfinity)), WKC.NegativeInfinity);
                        AddConstant(type.GetProperty(nameof(IFloatingPointIeee754<>.PositiveInfinity)), WKC.PositiveInfinity);
                    }
                    else if (definition == typeof(IEqualityOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.Equal(p[0], p[1])), WKF.Comparison.Equal);
                        f.Add(MakeLambda(argTypes, p => Expression.NotEqual(p[0], p[1])), WKF.Comparison.NotEqual);
                    }
                    else if (definition == typeof(IComparisonOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.GreaterThan(p[0], p[1])), WKF.Comparison.GreaterThan);
                        f.Add(MakeLambda(argTypes, p => Expression.GreaterThanOrEqual(p[0], p[1])), WKF.Comparison.GreaterThanOrEqual);
                        f.Add(MakeLambda(argTypes, p => Expression.LessThan(p[0], p[1])), WKF.Comparison.LessThan);
                        f.Add(MakeLambda(argTypes, p => Expression.LessThanOrEqual(p[0], p[1])), WKF.Comparison.LessThanOrEqual);
                    }
                    ////else if (definition == typeof(IUnaryPlusOperators<,>))
                    ////{
                    ////    var argTypes = new[] { typeArgs[0] };
                    ////    t.Add(MakeLambda(argTypes, p => Expression.UnaryPlus(p[0])), WellKnownFunctions.Identity);
                    ////}
                    else if (definition == typeof(IUnaryNegationOperators<,>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        f.Add(MakeLambda(argTypes, p => Expression.Negate(p[0])), WKF.Arithmetic.Negate);
                        f.Add(MakeLambda(argTypes, p => Expression.NegateChecked(p[0])), WKF.Arithmetic.Negate);
                    }
                    else if (definition == typeof(IAdditionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.Add(p[0], p[1])), WKF.Arithmetic.Add);
                        f.Add(MakeLambda(argTypes, p => Expression.AddChecked(p[0], p[1])), WKF.Arithmetic.Add);
                    }
                    else if (definition == typeof(ISubtractionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.Subtract(p[0], p[1])), WKF.Arithmetic.Subtract);
                        f.Add(MakeLambda(argTypes, p => Expression.SubtractChecked(p[0], p[1])), WKF.Arithmetic.Subtract);
                    }
                    else if (definition == typeof(IMultiplyOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.Multiply(p[0], p[1])), WKF.Arithmetic.Multiply);
                        f.Add(MakeLambda(argTypes, p => Expression.MultiplyChecked(p[0], p[1])), WKF.Arithmetic.Multiply);
                    }
                    else if (definition == typeof(IDivisionOperators<,,>))
                    {
                        var argTypes = typeArgs[..^1];
                        f.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1])), WKF.Arithmetic.Divide);
                        var opDivideChecked = type.GetMethod("op_CheckedDivision", argTypes);
                        f.Add(MakeLambda(argTypes, p => Expression.Divide(p[0], p[1], opDivideChecked)), WKF.Arithmetic.Divide);
                    }
                    else if (definition == typeof(IFloatingPointIeee754<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var inv = type.GetMethod(nameof(IFloatingPointIeee754<>.ReciprocalEstimate), argTypes)!;
                        f.Add(MakeLambda(argTypes, p => Expression.Call(inv, p[0])), WKF.Arithmetic.Reciprocal);
                    }
                    else if (definition == typeof(IPowerFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0], typeArgs[0] };
                        var pow = type.GetMethod(nameof(IPowerFunctions<>.Pow), argTypes)!;
                        f.Add(MakeLambda(argTypes, p => Expression.Call(pow, p[0], p[1])), WKF.Exponential.Pow);
                    }
                    else if (definition == typeof(IRootFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var sqrt = type.GetMethod(nameof(IRootFunctions<>.Sqrt), argTypes)!;
                        f.Add(MakeLambda(argTypes, p => Expression.Call(sqrt, p[0])), WKF.Exponential.Sqrt);
                    }
                    else if (definition == typeof(ILogarithmicFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var log = type.GetMethod(nameof(ILogarithmicFunctions<>.Log), argTypes)!;
                        f.Add(MakeLambda(argTypes, p => Expression.Call(log, p[0])), WKF.Exponential.Ln);
                    }
                    else if (definition == typeof(IExponentialFunctions<>))
                    {
                        var argTypes = new[] { typeArgs[0] };
                        var exp = type.GetMethod(nameof(IExponentialFunctions<>.Exp), argTypes)!;
                        f.Add(MakeLambda(argTypes, p => Expression.Call(exp, p[0])), WKF.Exponential.Exp);
                    }
                }
            }
        }
    }
}

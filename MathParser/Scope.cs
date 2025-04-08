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

            public static Dictionary<Type, Action<Type, Type[], Action<string, KnownConstant, bool>, Action<Type[], Func<ParameterExpression[], Expression>, KnownFunction>>> GenericMathInterfaces = new()
            {
                [typeof(INumberBase<>)] = (i, typeArgs, add, _) =>
                {
                    add(nameof(INumberBase<>.Zero), WKC.Zero, true);
                    add(nameof(INumberBase<>.One), WKC.One, true);
                },
                ////[typeof(INumber<>)] = (i, typeArgs, _, add) =>
                ////{
                ////    /* Clamp, Min, Max, Sign */
                ////}
                [typeof(IFloatingPointConstants<>)] = (i, typeArgs, add, _) =>
                {
                    add(nameof(IFloatingPointConstants<>.E), WKC.EulersNumber, false);
                    add(nameof(IFloatingPointConstants<>.Pi), WKC.Pi, false);
                    add(nameof(IFloatingPointConstants<>.Tau), WKC.Tau, false);
                },
                [typeof(IFloatingPointIeee754<>)] = (i, typeArgs, addC, addF) =>
                {
                    addC(nameof(IFloatingPointIeee754<>.NaN), WKC.Indeterminate, true);
                    addC(nameof(IFloatingPointIeee754<>.NegativeInfinity), WKC.NegativeInfinity, true);
                    addC(nameof(IFloatingPointIeee754<>.PositiveInfinity), WKC.PositiveInfinity, true);
                    var argTypes = new[] { typeArgs[0] };
                    var inv = i.GetMethod(nameof(IFloatingPointIeee754<>.ReciprocalEstimate), argTypes)!;
                    addF(argTypes, p => Expression.Call(inv, p[0]), WKF.Arithmetic.Reciprocal);
                },
                [typeof(IEqualityOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.Equal(p[0], p[1]), WKF.Comparison.Equal);
                    add(argTypes, p => Expression.NotEqual(p[0], p[1]), WKF.Comparison.NotEqual);
                },
                [typeof(IComparisonOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.GreaterThan(p[0], p[1]), WKF.Comparison.GreaterThan);
                    add(argTypes, p => Expression.GreaterThanOrEqual(p[0], p[1]), WKF.Comparison.GreaterThanOrEqual);
                    add(argTypes, p => Expression.LessThan(p[0], p[1]), WKF.Comparison.LessThan);
                    add(argTypes, p => Expression.LessThanOrEqual(p[0], p[1]), WKF.Comparison.LessThanOrEqual);
                },
                ////[typeof(IUnaryPlusOperators<,>)] = (i, typeArgs, _, add) =>
                ////{
                ////    var argTypes = new[] { typeArgs[0] };
                ////    add(argTypes, p => Expression.UnaryPlus(p[0]), WKF.Identity);
                ////}
                [typeof(IUnaryNegationOperators<,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = new[] { typeArgs[0] };
                    add(argTypes, p => Expression.Negate(p[0]), WKF.Arithmetic.Negate);
                    add(argTypes, p => Expression.NegateChecked(p[0]), WKF.Arithmetic.Negate);
                },
                [typeof(IAdditionOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.Add(p[0], p[1]), WKF.Arithmetic.Add);
                    add(argTypes, p => Expression.AddChecked(p[0], p[1]), WKF.Arithmetic.Add);
                },
                [typeof(ISubtractionOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.Subtract(p[0], p[1]), WKF.Arithmetic.Subtract);
                    add(argTypes, p => Expression.SubtractChecked(p[0], p[1]), WKF.Arithmetic.Subtract);
                },
                [typeof(IMultiplyOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.Multiply(p[0], p[1]), WKF.Arithmetic.Multiply);
                    add(argTypes, p => Expression.MultiplyChecked(p[0], p[1]), WKF.Arithmetic.Multiply);
                },
                [typeof(IDivisionOperators<,,>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = typeArgs[..^1];
                    add(argTypes, p => Expression.Divide(p[0], p[1]), WKF.Arithmetic.Divide);
                    var opDivideChecked = i.GetMethod("op_CheckedDivision", argTypes)!;
                    add(argTypes, p => Expression.Divide(p[0], p[1], opDivideChecked), WKF.Arithmetic.Divide);
                },
                [typeof(IPowerFunctions<>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = new[] { typeArgs[0], typeArgs[0] };
                    var pow = i.GetMethod(nameof(IPowerFunctions<>.Pow), argTypes)!;
                    add(argTypes, p => Expression.Call(pow, p[0], p[1]), WKF.Exponential.Pow);
                },
                [typeof(IRootFunctions<>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = new[] { typeArgs[0] };
                    var sqrt = i.GetMethod(nameof(IRootFunctions<>.Sqrt), argTypes)!;
                    add(argTypes, p => Expression.Call(sqrt, p[0]), WKF.Exponential.Sqrt);
                },
                [typeof(ILogarithmicFunctions<>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = new[] { typeArgs[0] };
                    var log = i.GetMethod(nameof(ILogarithmicFunctions<>.Log), argTypes)!;
                    add(argTypes, p => Expression.Call(log, p[0]), WKF.Exponential.Ln);
                },
                [typeof(IExponentialFunctions<>)] = (i, typeArgs, _, add) =>
                {
                    var argTypes = new[] { typeArgs[0] };
                    var exp = i.GetMethod(nameof(IExponentialFunctions<>.Exp), argTypes)!;
                    add(argTypes, p => Expression.Call(exp, p[0]), WKF.Exponential.Exp);
                },
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

                var interfaces = numberType.GetInterfaces();
                foreach (var type in interfaces)
                {
                    var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                    var typeArgs = type.IsGenericType ? type.GetGenericArguments() : [];

                    if (GenericMathInterfaces.TryGetValue(definition, out var performImport))
                    {
                        performImport(
                            type,
                            typeArgs,
                            (name, constant, valueFirst) =>
                            {
                                var interfaceProperty = type.GetProperty(name);
                                var map = numberType.GetInterfaceMap(type);
                                var getMethod = map.TargetMethods[Array.IndexOf(map.InterfaceMethods, interfaceProperty.GetMethod)];
                                var property = numberType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetMethod == getMethod).Single();
                                var value = property.GetValue(null);

                                if (!valueFirst)
                                {
                                    c.Add(Expression.MakeMemberAccess(null, property), constant);
                                }

                                c.Add(Expression.Constant(value), constant);

                                if (valueFirst)
                                {
                                    c.Add(Expression.MakeMemberAccess(null, property), constant);
                                }
                            },
                            (argTypes, makeLambda, function) => f.Add(MakeLambda(argTypes, makeLambda), function));
                    }
                }
            }
        }
    }
}

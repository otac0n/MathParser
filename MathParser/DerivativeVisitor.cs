namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;
    using WKC = WellKnownConstants;
    using WKF = WellKnownFunctions;

    public class DerivativeVisitor(Scope scope, ParameterExpression variable) : ExpressionVisitor
    {
        /// <summary>
        /// Gets the scope in which the transformations are performed.
        /// </summary>
        public Scope Scope => scope;

        /// <summary>
        /// Gets the variable for which the transformations are performed.
        /// </summary>
        public ParameterExpression Variable => variable;

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(node))]
        public override Expression? Visit(Expression? node)
        {
            if (scope.TryBind(node, out var knownConstant))
            {
                return this.VisitKnownConstant(knownConstant, node);
            }
            else if (scope.TryBind(node, out var knownFunction, out var functionArguments))
            {
                return this.VisitKnownFunction(knownFunction, node, functionArguments);
            }

            switch (node.NodeType)
            {
                case ExpressionType.Conditional:
                case ExpressionType.Constant:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Parameter:
                case ExpressionType.MemberAccess:
                    return base.Visit(node);
            }

            throw new NotImplementedException($"The {node.NodeType} '{node}' is not implemented.");
        }

        protected Expression VisitKnownConstant(KnownConstant knownConstant, Expression node)
        {
            if (knownConstant == WKC.Indeterminate)
            {
                return node;
            }
            else if (knownConstant == WKC.PositiveInfinity
                || knownConstant == WKC.NegativeInfinity)
            {
                return scope.NaN();
            }

            return scope.Zero();
        }

        protected Expression VisitKnownFunction(KnownFunction function, Expression node, IList<Expression> arguments)
        {
            var converted = new Expression[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                converted[i] = this.Visit(arguments[i]);
            }

            if (WKF.ExpressionTypeLookup.TryGetValue(function, out var effectiveType))
            {
                if (arguments.Count == 1)
                {
                    var operand = converted[0];
                    switch (effectiveType)
                    {
                        case ExpressionType.Negate:
                            return scope.Negate(operand);
                    }
                }
                else if (arguments.Count == 2)
                {
                    var left = converted[0];
                    var right = converted[1];
                    switch (effectiveType)
                    {
                        case ExpressionType.Add:
                            return scope.Add(left, right);

                        case ExpressionType.Subtract:
                            return scope.Subtract(left, right);

                        case ExpressionType.Multiply:
                            return scope.Add(scope.Multiply(left, arguments[1]), scope.Multiply(arguments[0], right));

                        case ExpressionType.Divide:
                            return scope.Divide(scope.Subtract(scope.Multiply(left, arguments[1]), scope.Multiply(arguments[0], right)), scope.Pow(arguments[1], Expression.Constant(2.0)));

                        case ExpressionType.Power:
                            return scope.IsConstantValue(arguments[1], out var constant)
                                ? scope.Multiply(left, scope.Multiply(constant, scope.Pow(arguments[0], scope.Subtract(constant, scope.One()))))
                                : scope.Multiply(node, this.Visit(scope.Multiply(scope.Log(arguments[0]), arguments[1])));
                    }
                }
            }

            if (function == WKF.Piecewise.Abs && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Divide(node, arguments[0]));
            }
            else if (function == WKF.Trigonometric.Sine && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Cos(arguments[0]));
            }
            else if (function == WKF.Trigonometric.Cosine && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Negate(scope.Sin(arguments[0])));
            }
            else if (function == WKF.Trigonometric.Tangent && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Pow(scope.Cos(arguments[0]), Expression.Constant(2.0)));
            }
            else if (function == WKF.Trigonometric.Arcsine && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Sqrt(scope.Subtract(Expression.Constant(1.0), scope.Pow(arguments[0], Expression.Constant(2.0)))));
            }
            else if (function == WKF.Trigonometric.Arcosine && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Negate(scope.Sqrt(scope.Subtract(Expression.Constant(1.0), scope.Pow(arguments[0], Expression.Constant(2.0))))));
            }
            else if (function == WKF.Trigonometric.Arctangent && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Add(scope.Pow(arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0)));
            }
            else if (function == WKF.Hyperbolic.Sine && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Cosh(arguments[0]));
            }
            else if (function == WKF.Hyperbolic.Cosine && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Sinh(arguments[0]));
            }
            else if (function == WKF.Hyperbolic.Tangent && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Pow(scope.Cosh(arguments[0]), Expression.Constant(2.0)));
            }
            else if (function == WKF.Hyperbolic.Arcsine && arguments.Count == 1)
            {
                return scope.Divide(converted[0], scope.Sqrt(scope.Add(scope.Pow(arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0))));
            }
            else if (function == WKF.Hyperbolic.Arcosine && arguments.Count == 1)
            {
                // TODO: Domain of the function is Reals > 1
                return scope.Divide(converted[0], scope.Sqrt(scope.Subtract(scope.Pow(arguments[0], Expression.Constant(2.0)), Expression.Constant(1.0))));
            }
            else if (function == WKF.Hyperbolic.Arctangent && arguments.Count == 1)
            {
                // TODO: Domain of the function is |Reals| < 1
                return scope.Divide(converted[0], scope.Subtract(Expression.Constant(1.0), scope.Pow(arguments[0], Expression.Constant(2.0))));
            }
            else if (function == WKF.Exponential.Sqrt && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], scope.Multiply(Expression.Constant(0.5), scope.Divide(node, arguments[0])));
            }
            else if (function == WKF.Exponential.Exp && arguments.Count == 1)
            {
                return scope.Multiply(converted[0], node);
            }
            else if (function == WKF.Exponential.Ln && arguments.Count == 1)
            {
                // TODO: Domain of the function is Reals > 0.
                return scope.Divide(converted[0], arguments[0]);
            }

            throw new NotImplementedException($"The {node.NodeType} '{node}' is not implemented.");
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return scope.Zero();
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == variable ? scope.One() : scope.Zero();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return scope.Zero();
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            {
                return this.Visit(node.Operand);
            }

            throw new NotImplementedException($"The {node.NodeType} '{node}' is not implemented.");
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return scope.Conditional(node.Test, this.Visit(node.IfTrue), this.Visit(node.IfFalse));
        }
    }
}

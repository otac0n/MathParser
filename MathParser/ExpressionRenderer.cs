// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq.Expressions;
    using System.Numerics;
    using MathParser.VisualNodes;

    /// <summary>
    /// Renders <see cref="Expression">Expressions</see> a an images.
    /// </summary>
    public class ExpressionRenderer
    {
        /// <summary>
        /// Gets or sets the brush that will be used when rendering expressions.
        /// </summary>
        public Brush Brush { get; set; } = SystemBrushes.WindowText;

        /// <summary>
        /// Gets or sets the font that will be used when measuring and rendering expressions.
        /// </summary>
        public Font Font { get; set; } = SystemFonts.DefaultFont;

        /// <summary>
        /// Draws the specified expression at the specified location.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to draw.</param>
        /// <param name="point">The <see cref="PointF"/> specifies the upper-left corner of the drawn expression.</param>
        public void DrawExpression(Graphics graphics, Expression expression, PointF point)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            visualTree.Draw(graphics, this.Font, this.Brush, point);
        }

        /// <summary>
        /// Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        public SizeF Measure(Graphics graphics, Expression expression)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            float baseline;
            return visualTree.Measure(graphics, this.Font, out baseline);
        }

        /// <summary>
        ///  Measures the size of the specified expression.
        /// </summary>
        /// <param name="graphics">The target <see cref="Graphics"/> object.</param>
        /// <param name="expression">The expression to measure.</param>
        /// <param name="baseline">Will be set to the distance, in pixels, from the top of the bounding region to the baseline.</param>
        /// <returns>The size of the bounding region of the measured expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#", Justification = "This is an optional overload. It is left as an out parameter for performance.")]
        public SizeF Measure(Graphics graphics, Expression expression, out float baseline)
        {
            var visualTree = ExpressionTransformer.TransformToVisualTree(expression);
            return visualTree.Measure(graphics, this.Font, out baseline);
        }

        private class ExpressionTransformer : ExpressionVisitor
        {
            private VisualNode root;

            private ExpressionTransformer()
            {
            }

            private enum Associativity
            {
                None = 0,
                Left,
                Right,
            }

            public static VisualNode TransformToVisualTree(Expression expression)
            {
                var transformer = new ExpressionTransformer();
                transformer.Visit(expression);
                return transformer.root;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                this.Visit(node.Left);
                var left = this.root;
                this.Visit(node.Right);
                var right = this.root;

                var simpleType = GetEffectiveNodeType(node);

                if (NeedsLeftBrackets(simpleType, node.Left))
                {
                    left = new BracketedVisualNode("(", left, ")");
                }

                if (NeedsRightBrackets(simpleType, node.Right))
                {
                    right = new BracketedVisualNode("(", right, ")");
                }

                if (simpleType == ExpressionType.Power)
                {
                    this.root = new PowerVisualNode(left, right);
                }
                else
                {
                    string op;
                    switch (simpleType)
                    {
                        case ExpressionType.Add:
                            op = "+";
                            break;

                        case ExpressionType.Subtract:
                            op = "-";
                            break;

                        case ExpressionType.Multiply:
                            op = "·";
                            break;

                        case ExpressionType.Divide:
                            op = "÷";
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    this.root = new BaselineAlignedVisualNode(left, new StringVisualNode(op), right);
                }

                return node;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value is double)
                {
                    this.root = FormatDouble((double)node.Value);
                }
                else if (node.Value is Complex)
                {
                    var value = (Complex)node.Value;
                    this.root = FormatComplex(value.Real, value.Imaginary);
                }
                else
                {
                    this.root = new StringVisualNode(node.Value?.ToString() ?? "null");
                }

                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var simpleType = GetEffectiveNodeType(node);
                if (simpleType == ExpressionType.Power)
                {
                    var leftArg = node.Arguments[0];
                    var rightArg = node.Arguments[1];

                    this.Visit(leftArg);
                    var left = this.root;
                    this.Visit(rightArg);
                    var right = this.root;

                    if (NeedsLeftBrackets(simpleType, leftArg))
                    {
                        left = new BracketedVisualNode("(", left, ")");
                    }

                    if (NeedsRightBrackets(simpleType, rightArg))
                    {
                        right = new BracketedVisualNode("(", right, ")");
                    }

                    this.root = new PowerVisualNode(left, right);
                    return node;
                }
                else
                {
                    return base.VisitMethodCall(node);
                }
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (node.Type == typeof(Complex) && node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant && node.Arguments[1].NodeType == ExpressionType.Constant)
                {
                    var real = (double)((ConstantExpression)node.Arguments[0]).Value;
                    var imaginary = (double)((ConstantExpression)node.Arguments[1]).Value;
                    this.root = FormatComplex(real, imaginary);
                    return node;
                }
                else
                {
                    return base.VisitNew(node);
                }
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                base.VisitParameter(node);
                this.root = new StringVisualNode(node.Name);
                return node;
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType == ExpressionType.Negate ||
                    node.NodeType == ExpressionType.NegateChecked)
                {
                    base.VisitUnary(node);
                    var inner = this.root;

                    if (NeedsRightBrackets(ExpressionType.Negate, node.Operand))
                    {
                        inner = new BracketedVisualNode("(", inner, ")");
                    }

                    this.root = new BaselineAlignedVisualNode(new StringVisualNode("-"), inner);
                    return node;
                }
                else
                {
                    return base.VisitUnary(node);
                }
            }

            private static VisualNode FormatComplex(double real, double imaginary)
            {
                var realNode = real == 0 || double.IsNaN(real) ? null : FormatDouble(real);
                var imaginaryNode = imaginary == 0 || double.IsNaN(imaginary) ? null :
                           imaginary == 1 ? new StringVisualNode("i") :
                           (VisualNode)new BaselineAlignedVisualNode(FormatDouble(imaginary), new StringVisualNode("i"));

                return
                    realNode != null && imaginaryNode != null ? new BaselineAlignedVisualNode(realNode, new StringVisualNode("+"), imaginaryNode) :
                    realNode != null ? realNode :
                    imaginaryNode != null ? imaginaryNode :
                    double.IsNaN(real) && double.IsNaN(imaginary) ? FormatDouble(double.NaN) :
                    FormatDouble(0);
            }

            private static VisualNode FormatDouble(double value)
            {
                return new StringVisualNode(
                    (value == Math.PI * 2) ? "τ" :
                    (value == Math.PI) ? "π" :
                    (value == Math.E) ? "e" :
                    (value == (1 + Math.Sqrt(5)) / 2) ? "φ" :
                    value.ToString("R"));
            }

            private static Associativity GetAssociativity(int precedence)
            {
                switch (precedence)
                {
                    case 0:
                    case 1:
                    case 2:
                        return Associativity.Left;

                    case 3:
                        return Associativity.Right;

                    default:
                        return Associativity.None;
                }
            }

            private static ExpressionType GetEffectiveNodeType(Expression expression)
            {
                var actualType = expression.NodeType;

                var getDoubleType = new Func<double, ExpressionType>(value => value < 0 ? ExpressionType.Negate : ExpressionType.Constant);
                var getComplexType = new Func<double, double, ExpressionType>((real, imaginary) =>
                {
                    if (real != 0 && imaginary != 0)
                    {
                        return ExpressionType.Add;
                    }
                    else
                    {
                        return real != 0
                            ? getDoubleType(real)
                            : imaginary == 1
                                ? ExpressionType.Parameter
                                : ExpressionType.Multiply;
                    }
                });

                if (actualType == ExpressionType.AddChecked)
                {
                    return ExpressionType.Add;
                }
                else if (actualType == ExpressionType.SubtractChecked)
                {
                    return ExpressionType.Subtract;
                }
                else if (actualType == ExpressionType.MultiplyChecked)
                {
                    return ExpressionType.Multiply;
                }
                else if (actualType == ExpressionType.NegateChecked)
                {
                    return ExpressionType.Negate;
                }
                else if (actualType == ExpressionType.New && expression.Type == typeof(Complex))
                {
                    var node = (NewExpression)expression;
                    if (node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant && node.Arguments[1].NodeType == ExpressionType.Constant)
                    {
                        var real = (double)((ConstantExpression)node.Arguments[0]).Value;
                        var imaginary = (double)((ConstantExpression)node.Arguments[1]).Value;
                        return getComplexType(real, imaginary);
                    }
                }
                else if (actualType == ExpressionType.Constant)
                {
                    if (expression.Type == typeof(Complex))
                    {
                        var value = (Complex)((ConstantExpression)expression).Value;
                        return getComplexType(value.Real, value.Imaginary);
                    }
                    else if (expression.Type == typeof(double))
                    {
                        return getDoubleType((double)((ConstantExpression)expression).Value);
                    }
                }
                else if (actualType == ExpressionType.Call)
                {
                    var node = (MethodCallExpression)expression;
                    if (node.Method.IsStatic && node.Method.Name == "Pow" && node.Arguments.Count == 2 &&
                        (node.Method.DeclaringType == typeof(Complex) || node.Method.DeclaringType == typeof(Math)))
                    {
                        return ExpressionType.Power;
                    }
                }
                else if (
                    actualType == ExpressionType.Convert ||
                    actualType == ExpressionType.ConvertChecked)
                {
                    return GetEffectiveNodeType(((UnaryExpression)expression).Operand);
                }

                return actualType;
            }

            private static int GetPrecedence(ExpressionType simpleType)
            {
                switch (simpleType)
                {
                    case ExpressionType.Add:
                    case ExpressionType.Subtract:
                        return 0;

                    case ExpressionType.Multiply:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                        return 1;

                    case ExpressionType.Negate:
                        return 2;

                    case ExpressionType.Power:
                        return 3;

                    default:
                        return int.MaxValue;
                }
            }

            private static bool IsFullyAssociative(ExpressionType left, ExpressionType right)
            {
                if (left == ExpressionType.Add && right == ExpressionType.Add)
                {
                    return true;
                }

                if (left == ExpressionType.Multiply && right == ExpressionType.Multiply)
                {
                    return true;
                }

                if (left == ExpressionType.Negate && right == ExpressionType.Negate)
                {
                    return true;
                }

                return false;
            }

            private static bool NeedsLeftBrackets(ExpressionType outerSimpleType, Expression inner)
            {
                var innerSimpleType = GetEffectiveNodeType(inner);
                var outerPrecedence = GetPrecedence(outerSimpleType);
                var innerPrecedence = GetPrecedence(innerSimpleType);
                var innerAssociativity = GetAssociativity(innerPrecedence);
                var fullyAssociative = IsFullyAssociative(innerSimpleType, outerSimpleType);

                if (outerPrecedence < innerPrecedence || fullyAssociative)
                {
                    return false;
                }

                if (outerPrecedence == innerPrecedence)
                {
                    if (innerAssociativity == Associativity.Left)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool NeedsRightBrackets(ExpressionType outerSimpleType, Expression inner)
            {
                var innerSimpleType = GetEffectiveNodeType(inner);
                var outerPrecedence = GetPrecedence(outerSimpleType);
                var innerPrecedence = GetPrecedence(innerSimpleType);
                var innerAssociativity = GetAssociativity(innerPrecedence);
                var fullyAssociative = IsFullyAssociative(outerSimpleType, innerSimpleType);

                if (outerPrecedence < innerPrecedence || fullyAssociative || outerSimpleType == ExpressionType.Power)
                {
                    return false;
                }

                if (outerPrecedence == innerPrecedence)
                {
                    if (innerAssociativity == Associativity.Right)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

namespace MathParser
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    /// Converts expressions to their string representation. Can be overridden.
    /// </summary>
    /// <param name="scope">The scope in which the transformations are performed.</param>
    public class StringTransformer(Scope scope) : ExpressionTransformer<string>(scope)
    {
        /// <inheritdoc />
        protected override string AddBrackets(string left, string expression, string right) => left + expression + right;

        /// <inheritdoc />
        protected override string CreateNot(string expression) => "!" + expression;

        /// <inheritdoc />
        protected override string CreateAnd(string left, string right) => left + " and " + right;

        /// <inheritdoc />
        protected override string CreateOr(string left, string right) => left + " or " + right;

        /// <inheritdoc />
        protected override string CreateAdd(string augend, string addend) => augend + "+" + addend;

        /// <inheritdoc />
        protected override string CreateDivide(string dividend, string divisor) => dividend + "/" + divisor;

        /// <inheritdoc />
        protected override string CreateMultiply(string multiplicand, string multiplier) => multiplicand + "·" + multiplier;

        /// <inheritdoc />
        protected override string CreateNegate(string expression) => "-" + expression;

        /// <inheritdoc />
        protected override string CreatePower(string @base, string exponent) => @base + "^" + exponent;

        /// <inheritdoc />
        protected override string CreateSubtract(string minuend, string subtrahend) => minuend + "-" + subtrahend;

        /// <inheritdoc />
        protected override string CreateConditional((string condition, string consequent)[] conditions, string alternative) =>
            conditions.Length == 1 && alternative == null
            ? conditions[0].consequent + "; " + conditions[0].condition
            : conditions.AsEnumerable().Reverse().Aggregate(alternative, (e, c) => "iif(" + c.condition + ", " + c.consequent + ", " + e + ")");

        /// <inheritdoc />
        protected override string CreateFunction(string name, params string[] arguments) => name + "(" + string.Join(", ", arguments) + ")";

        /// <inheritdoc />
        protected override string CreateRadical(string expression) => "√" + expression;

        /// <inheritdoc />
        protected override string CreateEquality(string left, ExpressionType op, string right) => left + FormatEqualityOperator(op) + right;

        /// <inheritdoc />
        protected override string CreateLambda(string name, string[] parameters, string body) => name + "(" + string.Join(", ", parameters) + ")" + FormatEqualityOperator(ExpressionType.Equal) + body;

        /// <inheritdoc />
        protected override string FormatBoolean(bool boolean) => boolean ? "true" : "false";

        /// <inheritdoc />
        protected override string FormatReal(double real) => NumberFormatter.FormatReal(real);

        /// <inheritdoc />
        protected override string FormatComplex(double real, double imaginary) => NumberFormatter.FormatComplexNumber(real, imaginary);

        /// <inheritdoc />
        protected override string FormatVariable(string name) => name;

        /// <inheritdoc />
        protected override ExpressionType GetEffectiveTypeReal(double real) => NumberFormatter.GetEffectiveTypeReal(real);

        /// <inheritdoc />
        protected override ExpressionType GetEffectiveTypeComplex(double real, double imaginary) => NumberFormatter.GetEffectiveTypeComplex(real, imaginary);

        /// <inheritdoc />
        protected override bool NeedsLeftBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
            if (outerEffectiveType == ExpressionType.Power &&
                ((outer is MethodCallExpression outerMethod && outerMethod.Method.Name == nameof(Math.Sqrt)) || (inner is MethodCallExpression innerMethod && innerMethod.Method.Name == nameof(Math.Sqrt))))
            {
                return GetPrecedence(this.GetEffectiveNodeType(inner)) <= GetPrecedence(outerEffectiveType);
            }

            if (innerEffectiveType == ExpressionType.Conditional &&
                !this.Scope.MatchConstraint(inner, out _, out _))
            {
                // A conditional with an alternative is rendered as a function.
                return false;
            }

            return base.NeedsLeftBrackets(outerEffectiveType, outer, innerEffectiveType, inner);
        }

        protected override bool NeedsRightBrackets(ExpressionType outerEffectiveType, Expression outer, ExpressionType innerEffectiveType, Expression inner)
        {
            if (innerEffectiveType == ExpressionType.Conditional &&
                !this.Scope.MatchConstraint(inner, out _, out _))
            {
                // A conditional with an alternative is rendered as a function.
                return false;
            }

            return base.NeedsRightBrackets(outerEffectiveType, outer, innerEffectiveType, inner);
        }

        /// <summary>
        /// Formats an equality operator as a string.
        /// </summary>
        /// <param name="op">The equality operator.</param>
        /// <returns>The string representation of the operator.</returns>
        private static string FormatEqualityOperator(ExpressionType op) =>
            op switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
            };
    }
}

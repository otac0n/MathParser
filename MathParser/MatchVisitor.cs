namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;

    /// <summary>
    /// A visitor that replaces nodes.
    /// </summary>
    /// <param name="root">The root node to match.</param>
    [DebuggerDisplay("{Root}")]
    public class MatchVisitor(Expression? root) : ExpressionVisitor
    {
        public Expression? Root { get; } = root;

        private bool success;
        private Expression? compare;
        private IList<ParameterExpression> parameters;
        private bool[] bound;
        private Expression?[] arguments;

        public Match PatternMatch(Expression expression)
        {
            this.compare = this.Root;
            this.success = true;
            if (expression is LambdaExpression lambda)
            {
                this.parameters = lambda.Parameters;
                this.bound = new bool[this.parameters.Count];
                this.arguments = new Expression[this.parameters.Count];
                this.Visit(lambda.Body);
            }
            else
            {
                (this.parameters, this.bound, this.arguments) = ([], [], []);
                this.Visit(expression);
            }

            return new Match(this.success, this.bound, this.arguments);
        }

        /// <inheritdoc/>
        public override Expression? Visit(Expression? node)
        {
            if (node == null)
            {
                this.success &= this.compare == null;
                return node;
            }

            if (node.NodeType == ExpressionType.Parameter && node is ParameterExpression parameter && this.parameters.Contains(parameter))
            {
                return base.Visit(node);
            }

            if (this.compare == null || this.compare.NodeType != node.NodeType)
            {
                this.success = false;
                return node;
            }

            return base.Visit(node);
        }

        /// <inheritdoc/>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            var index = this.parameters.IndexOf(node);
            if (index >= 0)
            {
                if (!this.bound[index])
                {
                    this.arguments[index] = this.compare;
                    this.bound[index] = true;
                }
                else
                {
                    this.success &= this.arguments[index] == this.compare;
                }
            }
            else
            {
                this.success &= node == this.compare;
            }

            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var compareBinary = (BinaryExpression)this.compare!;
            this.success &= compareBinary.Method == node.Method;

            if (this.success)
            {
                this.compare = compareBinary.Conversion;
                this.Visit(node.Conversion);
            }

            if (this.success)
            {
                this.compare = compareBinary.Left;
                this.Visit(node.Left);
            }

            if (this.success)
            {
                this.compare = compareBinary.Right;
                this.Visit(node.Right);
            }

            this.compare = compareBinary;
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitBlock(BlockExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var compareConstant = (ConstantExpression)this.compare!;
            this.success &= compareConstant.Type == node.Type && object.Equals(node.Value, compareConstant.Value);
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitDefault(DefaultExpression node)
        {
            this.success &= node.Type == ((DefaultExpression)this.compare!).Type;
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitDynamic(DynamicExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitExtension(Expression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitGoto(GotoExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitIndex(IndexExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitLabel(LabelExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(node))]
        protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitListInit(ListInitExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitLoop(LoopExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitMember(MemberExpression node)
        {
            var compareMember = (MemberExpression)this.compare!;
            this.success &= compareMember.Member == node.Member;

            if (this.success)
            {
                this.compare = compareMember.Expression;
                this.Visit(node.Expression);
            }

            this.compare = compareMember;
            return node;
        }

        /// <inheritdoc/>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var compareMethodCall = (MethodCallExpression)this.compare!;
            this.success &= compareMethodCall.Method == node.Method;

            if (this.success)
            {
                this.compare = compareMethodCall.Object;
                this.Visit(node.Object);
            }

            for (var i = 0; i < compareMethodCall.Arguments.Count && this.success; i++)
            {
                this.compare = compareMethodCall.Arguments[i];
                this.Visit(node.Arguments[i]);
            }

            this.compare = compareMethodCall;
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitNew(NewExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitTry(TryExpression node)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            var compareTypeBinary = (TypeBinaryExpression)this.compare!;
            this.success &= compareTypeBinary.TypeOperand == node.TypeOperand;

            if (this.success)
            {
                this.compare = compareTypeBinary.Expression;
                this.Visit(node.Expression);
            }

            this.compare = compareTypeBinary;
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            var compareUnary = (UnaryExpression)this.compare!;
            this.success &= compareUnary.Method == node.Method;

            if (this.success)
            {
                this.compare = compareUnary.Operand;
                this.Visit(node.Operand);
            }

            this.compare = compareUnary;
            return node;
        }

        public class Match
        {
            public Match(bool success, IList<bool> bound, IList<Expression?> arguments)
            {
                this.Success = success;
                this.Bound = bound;
                this.Arguments = arguments;
            }

            public bool Success { get; }

            public IList<bool> Bound { get; }

            public IList<Expression?> Arguments { get; }
        }
    }
}

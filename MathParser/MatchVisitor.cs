namespace MathParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq.Expressions;

    /// <summary>
    /// A visitor that replaces nodes.
    /// </summary>
    /// <param name="root">The root node to match.</param>
    public class MatchVisitor(Expression? root) : ExpressionVisitor
    {
        public Expression? Root { get; } = root;

        private bool success;
        private Expression? compare;
        private IList<ParameterExpression> parameters;
        private bool[] bound;
        private Expression?[] arguments;

        public Match PatternMatch(LambdaExpression lambda)
        {
            this.compare = this.Root;
            this.parameters = lambda.Parameters;
            this.bound = new bool[this.parameters.Count];
            this.arguments = new Expression[this.parameters.Count];
            this.success = true;
            this.Visit(lambda.Body);
            return new Match(this.success, this.arguments);
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

        protected override Expression VisitBinary(BinaryExpression node)
        {
            throw new NotImplementedException();
            return base.VisitBinary(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            throw new NotImplementedException();
            return base.VisitBlock(node);
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            throw new NotImplementedException();
            return base.VisitCatchBlock(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            throw new NotImplementedException();
            return base.VisitConditional(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            throw new NotImplementedException();
            return base.VisitConstant(node);
        }

        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            throw new NotImplementedException();
            return base.VisitDebugInfo(node);
        }

        protected override Expression VisitDefault(DefaultExpression node)
        {
            throw new NotImplementedException();
            return base.VisitDefault(node);
        }

        protected override Expression VisitDynamic(DynamicExpression node)
        {
            throw new NotImplementedException();
            return base.VisitDynamic(node);
        }

        protected override ElementInit VisitElementInit(ElementInit node)
        {
            throw new NotImplementedException();
            return base.VisitElementInit(node);
        }

        protected override Expression VisitExtension(Expression node)
        {
            throw new NotImplementedException();
            return base.VisitExtension(node);
        }

        protected override Expression VisitGoto(GotoExpression node)
        {
            throw new NotImplementedException();
            return base.VisitGoto(node);
        }

        protected override Expression VisitIndex(IndexExpression node)
        {
            throw new NotImplementedException();
            return base.VisitIndex(node);
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            throw new NotImplementedException();
            return base.VisitInvocation(node);
        }

        protected override Expression VisitLabel(LabelExpression node)
        {
            throw new NotImplementedException();
            return base.VisitLabel(node);
        }

        [return: NotNullIfNotNull("node")]
        protected override LabelTarget? VisitLabelTarget(LabelTarget? node)
        {
            throw new NotImplementedException();
            return base.VisitLabelTarget(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            throw new NotImplementedException();
            return base.VisitLambda(node);
        }

        protected override Expression VisitListInit(ListInitExpression node)
        {
            throw new NotImplementedException();
            return base.VisitListInit(node);
        }

        protected override Expression VisitLoop(LoopExpression node)
        {
            throw new NotImplementedException();
            return base.VisitLoop(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var compareMember = (MemberExpression)this.compare!;
            if (compareMember.Member != node.Member)
            {
                this.success = false;
                return node;
            }

            this.compare = compareMember.Expression;
            this.Visit(node.Expression);

            this.compare = compareMember;
            return node;
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            throw new NotImplementedException();
            return base.VisitMemberAssignment(node);
        }

        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            throw new NotImplementedException();
            return base.VisitMemberBinding(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            throw new NotImplementedException();
            return base.VisitMemberInit(node);
        }

        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            throw new NotImplementedException();
            return base.VisitMemberListBinding(node);
        }

        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            throw new NotImplementedException();
            return base.VisitMemberMemberBinding(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var compareMethodCall = (MethodCallExpression)this.compare!;
            if (compareMethodCall.Method != node.Method)
            {
                this.success = false;
                return node;
            }

            this.compare = compareMethodCall.Object;
            this.Visit(node.Object);

            for (var i = 0; i < compareMethodCall.Arguments.Count && this.success; i++)
            {
                this.compare = compareMethodCall.Arguments[i];
                this.Visit(node.Arguments[i]);
            }

            this.compare = compareMethodCall;
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            throw new NotImplementedException();
            return base.VisitNew(node);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            throw new NotImplementedException();
            return base.VisitNewArray(node);
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            throw new NotImplementedException();
            return base.VisitRuntimeVariables(node);
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            throw new NotImplementedException();
            return base.VisitSwitch(node);
        }

        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            throw new NotImplementedException();
            return base.VisitSwitchCase(node);
        }

        protected override Expression VisitTry(TryExpression node)
        {
            throw new NotImplementedException();
            return base.VisitTry(node);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            throw new NotImplementedException();
            return base.VisitTypeBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            throw new NotImplementedException();
            return base.VisitUnary(node);
        }

        public class Match
        {
            public Match(bool success, IList<Expression?> arguments)
            {
                this.Success = success;
                this.Arguments = arguments;
            }

            public bool Success { get; }

            public IList<Expression?> Arguments { get; }
        }
    }
}

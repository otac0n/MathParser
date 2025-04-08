// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser
{
    using System;
    using System.Linq.Expressions;

    public partial class Scope
    {
        /// <summary>
        /// Adds all known operators, constants, and generic math interfaces for the given type.
        /// </summary>
        /// <param name="numberType">The type to search for <see cref="WellKnownFunctions"/>.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(Type numberType)
        {
            WellKnownFunctionMapping.Add(numberType, this.knownConstants, this.knownMethods, this.typeConversions);
            return this;
        }

        /// <summary>
        /// Adds an expression as a representation of the specified <see cref="KnownConstant"/>.
        /// </summary>
        /// <param name="expression">The expression that represents the constant.</param>
        /// <param name="value">The <see cref="KnownConstant"/> that is represented.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(Expression expression, KnownConstant value)
        {
            this.knownConstants.Add(expression, value);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="KnownConstant"/> to the named object list under its default name.
        /// </summary>
        /// <param name="knownConstant">The <see cref="KnownConstant"/> to add.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(KnownConstant knownConstant)
        {
            this.namedObjects.Add(knownConstant.Name, knownConstant);
            return this;
        }

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add<TIn, TOut>(Expression<Func<TIn, TOut>> expression, KnownFunction value)
        {
            this.knownMethods.Add(expression, value);
            return this;
        }

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add<T1, T2, TOut>(Expression<Func<T1, T2, TOut>> expression, KnownFunction value)
        {
            this.knownMethods.Add(expression, value);
            return this;
        }

        /// <summary>
        /// Adds an expression as an implementation of the specified <see cref="KnownFunction"/>.
        /// </summary>
        /// <param name="expression">The expression that implements the function.</param>
        /// <param name="value">The <see cref="KnownFunction"/> that is implemented.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(LambdaExpression expression, KnownFunction value)
        {
            this.knownMethods.Add(expression, value);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="KnownFunction"/> to the named function list under its default name.
        /// </summary>
        /// <param name="knownFunction">The <see cref="KnownFunction"/> to add.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(KnownFunction knownFunction)
        {
            this.namedObjects.Add(knownFunction.Name, knownFunction);
            return this;
        }

        /// <summary>
        /// Adds a <see cref="KnownFunction"/> to the named function list under an assoicated name.
        /// </summary>
        /// <param name="name">The associated name.</param>
        /// <param name="knownFunction">The <see cref="KnownFunction"/> to add.</param>
        /// <returns>This <see cref="Scope"/> for fluent addition of more values.</returns>
        public Scope Add(string name, KnownFunction knownFunction)
        {
            this.namedObjects.Add(name, knownFunction);
            return this;
        }

        /// <summary>
        /// Freezes the scope and returns it.
        /// </summary>
        /// <returns>The frozen <see cref="Scope"/> object.</returns>
        public Scope Freeze()
        {
            lock (this.syncRoot)
            {
                if (!this.frozen)
                {
                    this.knownConstants = this.KnownConstants;
                    this.knownMethods = this.KnownMethods;
                    this.namedObjects = this.NamedObjects;
                    this.typeConversions = this.TypeConversions;
                    this.frozen = true;
                }
            }

            return this;
        }
    }
}

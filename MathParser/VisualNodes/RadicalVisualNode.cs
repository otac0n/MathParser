// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MathParser.VisualNodes
{
    using System.Drawing;

    internal class RadicalVisualNode : BracketedVisualNode
    {
        private const string Radical = "√";

        public RadicalVisualNode(VisualNode node)
            : base(Radical + "(", node, ")")
        {
        }
    }
}

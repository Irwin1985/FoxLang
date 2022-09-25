using System.Collections.Generic;

namespace FoxLang
{
    public interface Node{
        void NodeType();
    }
    public interface Statement : Node
    {
        void StatementNode();
    }
    public interface Expression : Node
    {
        void ExpressionNode();
    }
    public class AstProgram : Statement
    {
        public List<Statement> statements;

        public AstProgram(List<Statement> statements)
        {
            this.statements = statements;
        }

        public void NodeType(){}

        public void StatementNode(){}
    }


}

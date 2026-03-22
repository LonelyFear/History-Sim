using System.Collections.Generic;
using System.Security;

namespace  BehaviorTrees
{
    public interface IStrategy
    {
        Node.Status Process();
        void Reset();
    }
    public class BehaviorTree : Node
    {
        public BehaviorTree(string name) : base(name) {}

        public override Status Process()
        {
            while (currentChild < children.Count)
            {
                Status status = children[currentChild].Process();
                if (status != Status.SUCCESS)
                {
                    return status;
                }
                currentChild++;
            }
            return Status.SUCCESS;
        }
    }
    public class Leaf : Node
    {
        readonly IStrategy strategy;
        public Leaf(string name, IStrategy strategy) : base(name)
        {
            this.strategy = strategy;
        }
        public override Status Process()
        {
            return strategy.Process();
        }
        public override void Reset()
        {
            strategy.Reset();
        }
    }
    public abstract class Node
    {
        public enum Status 
        {
            SUCCESS,
            FAILURE,
            RUNNING,
        }
        public readonly string name;
        protected readonly List<Node> children = new List<Node>();
        protected int currentChild = 0;

        public Node(string name = "Node")
        {
            this.name = name;
        }

        public void AddChild(Node child)
        {
            children.Add(child);
        }
        public virtual Status Process()
        {
            return children[currentChild].Process();
        }
        public virtual void Reset()
        {
            currentChild = 0;
            foreach (Node child in children)
            {
                child.Reset();
            }
        }
    }

}

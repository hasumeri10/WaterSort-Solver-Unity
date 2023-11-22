using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public int[] State;
    public Node Parent;
    public int Depth;

    public Node(int[] state, Node parent, int depth)
    {
        State = state;
        Parent = parent;
        Depth = depth;
    }
    
}

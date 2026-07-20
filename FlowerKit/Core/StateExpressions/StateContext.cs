namespace FlowerKit.Core.StateExpressions;

/// <summary>
/// Represents a possible action for state expressions.
/// </summary>
public class StateContext
{
    public StateExpression Events => new StateExpression();

    public StateExpression States => new StateExpression();

    public StateExpression Create(params object[] values)
    {
        return new StateExpression();
    }

    public StateExpression Delete(params object[] values)
    {
        return new StateExpression();
    }

    public StateExpression Update(params object[] values)
    {
        return new StateExpression();
    }
}
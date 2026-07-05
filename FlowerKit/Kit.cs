namespace FlowerKit;

using Core;

public static class Kit
{
    public static Flow Retry(this Flow flow)
    {
        // retry flow
        var retry = Flow.New<Event>(ctx =>
        {
            
        });

        return flow;
    }
}
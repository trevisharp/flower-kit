using System.Linq.Expressions;
using FlowerKit;

Log.Config
    .AddAppName("FlowerExample")
    .AddLevel()
    .AddTimeStamp();

Runtime.Run(args);

#region Events

record CreateEmployeeRequested(
    string Name,
    decimal Wage
) : Event;

record EmployeeCreated(
    int ID,
    string Name,
    decimal Wage
) : Event;

record EmployeePayRaised(
    int ID,
    decimal WageBonus
) : Event;

record InternalOrder(
    Guid ProcessID,
    int EmployeeID,
    int ProductID
) : Event;

record InternalOrderReject(
    Guid ProcessID,
    string Reason
) : Event;

record InternalOrderApprove(
    Guid ProcessID
) : Event;

#endregion

#region States

record Employee(
    int ID,
    string Name,
    decimal Wage
) : State(ctx => [
    from e in ctx.Events
    where e is EmployeeCreated
    select ctx.Create()
]);
    /*
    from e in events
    where e is EmployeeCreated
    select create(e.ID, e.Name, e.Wage),

    from e in events
    where e is EmployeePayRaised
    join s in states
    on s.ID equals e.ID
    select update(e.ID, s.Name, e.Wage + s.Wage)

    from e in events
    where e is EmployeeDismissal
    join s in states
    on e.ID equals s.ID
    select remove

    */

#endregion

#region Workflows

record EmployeeWorkflow() : Workflow(
    Flow.On<CreateEmployeeRequested>(ctx =>
    {

    })
);

record InternalOrderWorkflow() : Workflow(
    Flow.On<InternalOrder>(ctx =>
    {
        
    })
);

#endregion

record InternalOrderTest() : Test(
    () =>
    {
        Assert<InternalOrderApprove>();

        Publish<InternalOrder>.Emit(
            ProcessID: Guid.NewGuid(),
            EmployeeID: 1,
            ProductID: 1
        );
    },

    () =>
    {
        Assert<InternalOrderReject>(s => s.Last.Reason == "Price too high");

        Publish<InternalOrder>.Emit(
            ProcessID: Guid.NewGuid(),
            EmployeeID: 1,
            ProductID: 2
        );
    }
);
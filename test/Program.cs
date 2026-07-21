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
    decimal Wage,
    string Position
) : Event;

record EmployeeCreated(
    int ID,
    string Name,
    decimal Wage,
    string Position,
    DateTime Moment
) : Event;

record EmployeePayRaised(
    int ID,
    decimal BonusWage,
    DateTime Moment
) : Event;

record EmployeePositionChanged(
    int ID,
    string NewPosition,
    DateTime Moment
) : Event;

record FireEmployeeRequest(
    int ID
) : Event;

record EmployeeFired(
    int ID
) : Event;

#endregion

#region States

record Employee(
    int ID,
    string Name,
    decimal Wage,
    string Position,
    DateTime ContractDate,
    DateTime LastUpdate
) : State(ctx => [
    from e in ctx.Events<EmployeeCreated>()
    select new Employee(e.ID, e.Name, e.Wage, e.Position, e.Moment, e.Moment),

    from e in ctx.Events<EmployeeFired>()
    from s in ctx.States<Employee>()
    where e.ID == s.ID
    select ctx.Delete(s),

    from e in ctx.Events<EmployeePayRaised>()
    from s in ctx.States<Employee>()
    where e.ID == s.ID
    select s with { 
        Wage = s.Wage + e.BonusWage, 
        LastUpdate = e.Moment
    },

    from e in ctx.Events<EmployeePositionChanged>()
    from s in ctx.States<Employee>()
    where e.ID == s.ID
    select s with {
        Position = e.NewPosition,
        LastUpdate = e.Moment
    }
]);

#endregion

#region Workflows

record EmployeeWorkflow() : Workflow(
    Flow.On<CreateEmployeeRequested>(ctx =>
    {

    })
);

#endregion
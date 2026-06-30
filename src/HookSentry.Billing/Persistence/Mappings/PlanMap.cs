using FluentNHibernate.Mapping;
using HookSentry.Billing.Plans;

namespace HookSentry.Billing.Persistence.Mappings;

public class PlanMap : ClassMap<Plan>
{
    public PlanMap()
    {
        Table("plans");
        Not.LazyLoad();

        Id(x => x.Id, "id").GeneratedBy.Assigned();

        Map(x => x.Name, "name").Length(100).Not.Nullable();
        Map(x => x.MaxUsers, "max_users").Not.Nullable();
        Map(x => x.MaxDestinations, "max_destinations").Not.Nullable();
        Map(x => x.MaxEventsPerMonth, "max_events_per_month").Not.Nullable();
        Map(x => x.RetentionDays, "retention_days").Not.Nullable();
        Map(x => x.CreatedAt, "created_at").Not.Nullable();
        Map(x => x.UpdatedAt, "updated_at").Not.Nullable();
    }
}

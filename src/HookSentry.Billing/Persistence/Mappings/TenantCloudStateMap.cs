using FluentNHibernate.Mapping;
using HookSentry.Billing.TenantAccess;

namespace HookSentry.Billing.Persistence.Mappings;

public class TenantCloudStateMap : ClassMap<TenantCloudState>
{
    public TenantCloudStateMap()
    {
        Table("tenant_cloud_states");
        Not.LazyLoad();

        Id(x => x.Id, "id").GeneratedBy.Assigned();

        Map(x => x.TenantId, "tenant_id").Not.Nullable();
        Map(x => x.IsBlocked, "is_blocked").Not.Nullable();
        Map(x => x.BlockedAt, "blocked_at").Nullable();
        Map(x => x.BlockReason, "block_reason").Length(100).Nullable();
    }
}

using HookSentry.Billing.TenantAccess;

namespace HookSentry.Billing.Tests.TenantAccess;

public class TenantCloudStateTests
{
    private static TenantCloudState CriarEstado() => new(Guid.NewGuid());

    public class Construtor
    {
        [Fact]
        public void Deve_Gerar_Id_Nao_Vazio()
        {
            var state = CriarEstado();
            Assert.NotEqual(Guid.Empty, state.Id);
        }

        [Fact]
        public void Dois_Estados_Devem_Ter_Ids_Diferentes()
        {
            var a = CriarEstado();
            var b = CriarEstado();
            Assert.NotEqual(a.Id, b.Id);
        }

        [Fact]
        public void Deve_Atribuir_TenantId_Informado()
        {
            var tenantId = Guid.NewGuid();
            var state = new TenantCloudState(tenantId);
            Assert.Equal(tenantId, state.TenantId);
        }

        [Fact]
        public void IsBlocked_Deve_Ser_False_Na_Criacao()
        {
            var state = CriarEstado();
            Assert.False(state.IsBlocked);
        }

        [Fact]
        public void BlockedAt_Deve_Ser_Nulo_Na_Criacao()
        {
            var state = CriarEstado();
            Assert.Null(state.BlockedAt);
        }

        [Fact]
        public void BlockReason_Deve_Ser_Nulo_Na_Criacao()
        {
            var state = CriarEstado();
            Assert.Null(state.BlockReason);
        }

        [Fact]
        public void Deve_Lancar_Excecao_Quando_TenantId_For_Vazio()
        {
            Assert.Throws<ArgumentException>(() => new TenantCloudState(Guid.Empty));
        }
    }

    public class MetodoBlock
    {
        [Fact]
        public void Deve_Marcar_IsBlocked_Como_True()
        {
            var state = CriarEstado();
            state.Block("manual");
            Assert.True(state.IsBlocked);
        }

        [Fact]
        public void Deve_Atribuir_BlockReason_Informado()
        {
            var state = CriarEstado();
            state.Block("abuse");
            Assert.Equal("abuse", state.BlockReason);
        }

        [Fact]
        public void Deve_Definir_BlockedAt_Como_UtcNow()
        {
            var antes = DateTimeOffset.UtcNow;
            var state = CriarEstado();
            state.Block("manual");
            Assert.NotNull(state.BlockedAt);
            Assert.True(state.BlockedAt >= antes);
        }

        [Fact]
        public void Pode_Bloquear_Com_Reason_Com_Espacos_Em_Branco_Nas_Extremidades()
        {
            var state = CriarEstado();
            state.Block("  manual  ");
            Assert.Equal("manual", state.BlockReason);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Deve_Lancar_Excecao_Quando_Reason_For_Invalido(string? reason)
        {
            var state = CriarEstado();
            Assert.Throws<ArgumentException>(() => state.Block(reason!));
        }
    }

    public class MetodoUnblock
    {
        [Fact]
        public void Deve_Marcar_IsBlocked_Como_False()
        {
            var state = CriarEstado();
            state.Block("manual");
            state.Unblock();
            Assert.False(state.IsBlocked);
        }

        [Fact]
        public void Deve_Limpar_BlockedAt()
        {
            var state = CriarEstado();
            state.Block("manual");
            state.Unblock();
            Assert.Null(state.BlockedAt);
        }

        [Fact]
        public void Deve_Limpar_BlockReason()
        {
            var state = CriarEstado();
            state.Block("manual");
            state.Unblock();
            Assert.Null(state.BlockReason);
        }

        [Fact]
        public void Deve_Permitir_Desbloquear_Estado_Nao_Bloqueado()
        {
            var state = CriarEstado();
            var exception = Record.Exception(() => state.Unblock());
            Assert.Null(exception);
            Assert.False(state.IsBlocked);
        }
    }
}
